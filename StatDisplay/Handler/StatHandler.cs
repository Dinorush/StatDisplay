using StatDisplay.Attributes;
using StatDisplay.Data;
using StatDisplay.Networking;
using Il2CppInterop.Runtime.Injection;
using Player;
using SNetwork;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using TMPro;
using UnityEngine;
using Il2CppInterop.Runtime.Attributes;
using StatDisplay.Config;

namespace StatDisplay.Handler
{
    public sealed class StatHandler : MonoBehaviour
    {
        [HideFromIl2Cpp]
        public static StatHandler Instance { get; private set; } = null!;
        private static readonly (float x, float y) BaseOffset = (-70f, -62f);
        private static readonly float TextMargin = 35;
        private const float SyncInterval = 2f;
        private static RectTransform? s_baseRectTransform;
        private static TextMeshPro? s_baseTextMesh;

        private readonly List<TextMeshPro> _textMeshList = new();
        private readonly List<GameObject> _meshGOList = new();
        private readonly Dictionary<ulong, StatData> _data = new();

        private float _nextSyncTime = 0f;
        private bool _visible = true;

        public StatHandler(IntPtr ptr) : base(ptr) { }

        [InvokeOnAssetLoad]
        private static void Init()
        {
            ClassInjector.RegisterTypeInIl2Cpp<StatHandler>();
            GameObject go = new(nameof(StatHandler));
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<StatHandler>();
        }

        [InvokeOnBuildStart]
        private static void ResetValues()
        {
            foreach (var data in Instance._data.Values)
                data.Reset();
        }

        private void Awake()
        {
            Instance = this;
            EnsureMeshCapacity(4);
            Configuration.Config.OnChanged += ResetMeshOffsets;
        }

        [HideFromIl2Cpp]
        public static bool TryGetData(SNet_Player player, [MaybeNullWhen(false)] out StatData data) => Instance._data.TryGetValue(player.Lookup, out data);

        [HideFromIl2Cpp]
        public static void AddAccuracyDelta(ulong lookup, AccSlotType slot, short[,] delta)
        {
            if (!Instance._data.TryGetValue(lookup, out var data)) return;

            data.AddAccuracy(slot, delta);
        }

        [HideFromIl2Cpp]
        public static void AddDamage(ulong lookup, DamSlotType slot, DamStatType stat, float value)
        {
            if (!Instance._data.TryGetValue(lookup, out var data)) return;

            data.AddDamage(slot, stat, value);
        }

        public static void AddRemoteAccDelta(ulong lookup, short[,,] deltaAccuracy)
        {
            if (!Instance._data.TryGetValue(lookup, out var data)) return;

            data.AddRemoteAccDelta(deltaAccuracy);
        }

        public static void AddRemoteDamDelta(ulong lookup, float[,] deltaDamage)
        {
            if (!Instance._data.TryGetValue(lookup, out var data)) return;

            data.AddRemoteDamDelta(deltaDamage);
        }

        internal static void AddPlayer(SNet_Player player, bool hasMod = true) => Instance.AddPlayer_Internal(player, hasMod);
        private void AddPlayer_Internal(SNet_Player player, bool hasMod = true)
        {
            if (_data.TryGetValue(player.Lookup, out var data))
            {
                if (data.HasMod == hasMod) return;
                data.HasMod = hasMod;
            }
            else
                _data.Add(player.Lookup, new(player, hasMod));

            RefreshMeshList();
        }

        internal static void RemovePlayer(SNet_Player player) => Instance.RemovePlayer_Internal(player);
        private void RemovePlayer_Internal(SNet_Player player)
        {
            if (!_data.Remove(player.Lookup, out var data)) return;

            if (data.HasMod || StatManager.MasterHasMod)
                RefreshMeshList();
        }

        internal static void Clear() => Instance.Clear_Internal();
        private void Clear_Internal()
        {
            _data.Clear();
            for (int i = 0; i < 4; i++)
                _meshGOList[i].SetActive(false);

            for (int i = 4; i < _meshGOList.Count; i++)
                GameObject.Destroy(_meshGOList[i]);

            if (_meshGOList.Count > 4)
            {
                _textMeshList.RemoveRange(4, _meshGOList.Count - 4);
                _meshGOList.RemoveRange(4, _meshGOList.Count - 4);
            }
        }

        internal static void SetVisible(bool visible) => Instance.SetVisible_Internal(visible);
        private void SetVisible_Internal(bool visible)
        {
            if (visible == _visible) return;
            _visible = visible;

            if (visible)
                RefreshMeshList();
            else
                foreach (var go in _meshGOList)
                    go.SetActive(false);
        }

        internal static void RefreshMeshList() => Instance.RefreshMeshList_Internal();
        private void RefreshMeshList_Internal()
        {
            if (!_visible) return;

            foreach (var go in _meshGOList)
                go.SetActive(false);

            int count = 0;
            var playerSlots = SNet.Slots.PlayerSlots;
            for (int i = 0; i < playerSlots.Count; i++)
            {
                var player = playerSlots[i].player;
                if (player == null) continue;

                if (_data.TryGetValue(player.Lookup, out var data))
                {
                    if (data.HasMod || StatManager.MasterHasMod)
                    {
                        EnsureMeshCapacity(count + 1);
                        data.StatText.SetToSlot(_textMeshList[count], player.CharacterIndex);
                        _meshGOList[count].transform.localPosition = GetMeshPos(count);
                        _meshGOList[count].SetActive(true);
                        count++;
                    }
                    else
                    {
                        data.StatText.SetToEmpty();
                    }
                }
            }
        }

        internal static void ForceSync() => Instance.DoSync();
        private void DoSync()
        {
            _nextSyncTime = Clock.Time + SyncInterval;

            foreach ((var lookup, var data) in _data)
            {
                StatSyncManager.BroadcastAccData(lookup, data);
                StatSyncManager.BroadcastDamData(lookup, data);
            }
        }

        private void Update()
        {
            if (_data.Count == 0) return;

            foreach (var data in _data.Values)
                data.StatText.UpdateMesh();

            if (Clock.Time < _nextSyncTime) return;

            DoSync();
        }

        private void EnsureMeshCapacity(int num)
        {
            num -= _textMeshList.Count;
            if (num <= 0) return;

            if (s_baseRectTransform == null)
            {
                PUI_Inventory inventory = GuiManager.Current.m_playerLayer.Inventory;
                s_baseTextMesh = inventory.m_inventorySlots[InventorySlot.GearMelee].m_slim_archetypeName;
                foreach (RectTransform rectTransform in inventory.m_iconDisplay.GetComponentsInChildren<RectTransform>(true))
                {
                    if (rectTransform.name == "Background Fade")
                    {
                        s_baseRectTransform = rectTransform;
                        break;
                    }
                }

                if (s_baseRectTransform == null) return;
            }

            for (int i = 0; i < num; i++)
            {
                GameObject gameObject = GameObject.Instantiate(s_baseRectTransform.gameObject, s_baseRectTransform.parent);
                RectTransform component = gameObject.GetComponent<RectTransform>();
                gameObject.gameObject.SetActive(true);
                foreach (Transform transform in gameObject.GetComponentsInChildren<Transform>(true))
                {
                    if (transform.name == "TimerShowObject")
                    {
                        transform.gameObject.active = false;
                    }
                }

                GameObject gameObject2 = new(EntryPoint.MODNAME + _textMeshList.Count)
                {
                    layer = 5,
                    hideFlags = HideFlags.HideAndDontSave
                };
                gameObject2.transform.SetParent(component.transform, false);

                var textMesh = GameObject.Instantiate(s_baseTextMesh!, gameObject2.transform);
                textMesh.m_width *= 2;
                textMesh.GetComponent<RectTransform>().anchoredPosition = new(-5f, 9f);
                textMesh.SetText($"-");
                textMesh.ForceMeshUpdate();

                _textMeshList.Add(textMesh);
                _meshGOList.Add(textMesh.transform.parent.parent.gameObject);
                _meshGOList[^1].SetActive(false);
            }
        }

        private static Vector3 GetMeshPos(int i)
        {
            return new Vector3(BaseOffset.x + Configuration.Config.Offset.X, BaseOffset.y + Configuration.Config.Offset.Y - i * TextMargin, 0f);
        }

        private void ResetMeshOffsets()
        {
            for (int i = 0; i < _textMeshList.Count; i++)
                _meshGOList[i].transform.localPosition = new Vector3(BaseOffset.x + Configuration.Config.Offset.X, BaseOffset.y + Configuration.Config.Offset.Y - i * TextMargin, 0f);
        }
    }
}
