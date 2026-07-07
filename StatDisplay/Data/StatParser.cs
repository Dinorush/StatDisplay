using StatDisplay.Utils.Extensions;
using Player;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using SNetwork;
using StatDisplay.Config;

namespace StatDisplay.Data
{
    public sealed class StatParser
    {
        const char StartToken = '{';
        const char EndToken = '}';
        const string NoModFormat = "N/A (<#a0a0a0>{Damage}</color>)";
        public static readonly string[] SlotNames = new string[] { "RED", "GRE", "BLU", "PUR", "EX" };

        private List<string> _strings;
        private List<Token> _tokens;
        private StringBuilder _builder;
        private readonly StatData _parent;
        private readonly int _characterSlot;
        private readonly string _characterName;
        private string FormattedSlotName
        {
            get
            {
                string name;
                if (Configuration.Config.FormatFullName)
                    name = _characterName;
                else
                    name = _characterSlot < 4 ? SlotNames[_characterSlot] : SlotNames[^1] + (_characterSlot - 3);
                return $"<#{ColorUtility.ToHtmlStringRGB(PlayerManager.Current.m_playerColors[_characterSlot])}>{name}</color>: ";
            }
        }

        private string _text = "";
        public string Text
        {
            get
            {
                if (_needsUpdate)
                    UpdateInner();
                return _text;
            }
        }
        private TextMeshPro? _textMesh = null;
        public TextMeshPro? TextMesh
        {
            get => _textMesh;
            set
            {
                _textMesh = value;
                Update(true);
            }
        }

        private bool _hasMod = false;
        public bool HasMod
        {
            get => _hasMod;
            set
            {
                _hasMod = value;
                SetFormat();
            }
        }

        private bool _needsUpdate = false;
        private bool _meshNeedsUpdate = false;

        public StatParser(StatData data, SNet_Player player, bool hasMod)
        {
            _characterSlot = player.CharacterIndex;
            _characterName = player.NickName;
            _parent = data;
            _hasMod = hasMod;
            SetFormat();
            Configuration.Config.OnChanged += SetFormat;
        }

        public void UpdateMesh()
        {
            if (_meshNeedsUpdate)
            {
                _meshNeedsUpdate = false;
                TextMesh?.SetText(Text);
            }
        }

        public void Update(bool updateMesh = false)
        {
            _needsUpdate = true;
            _meshNeedsUpdate = !updateMesh;
            if (updateMesh)
                TextMesh?.SetText(Text);
        }

        public string GetEndscreenText(InventorySlot slot)
        {
            ExtractFormatInfo(Configuration.Config.EndScreenFormat, out var builder, out var strings, out var tokens, slot);
            return CreateText(builder, strings, tokens);
        }

        [MemberNotNull(nameof(_builder), nameof(_strings), nameof(_tokens))]
        private void SetFormat()
        {
            ExtractFormatInfo(_hasMod ? Configuration.Config.Format : NoModFormat, out _builder, out _strings, out _tokens);
            Update();
        }

        private void ExtractFormatInfo(string format, out StringBuilder builder, out List<string> strings, out List<Token> tokens, InventorySlot restrictSlot = InventorySlot.None)
        {
            strings = new();
            tokens = new();
            int lastIndex = 0;
            int endIndex;
            for (int index = format.IndexOf(StartToken); index != -1; index = format.IndexOf(StartToken, endIndex + 1))
            {
                endIndex = format.IndexOf(EndToken, index + 1);
                if (endIndex == -1) break;

                strings.Add(format[lastIndex..index]);
                tokens.Add(new Token(_parent, format[(index + 1)..endIndex], restrictSlot));
                lastIndex = endIndex + 1;
            }
            strings.Add(format[lastIndex..]);
            builder = new(6 + _strings.Sum(segment => segment.Length) + _tokens.Count * 11);
        }

        private void UpdateInner()
        {
            _builder.Append(FormattedSlotName);
            _text = CreateText(_builder, _strings, _tokens);
            _needsUpdate = false;
        }

        private static string CreateText(StringBuilder builder, List<string> strings, List<Token> tokens)
        {
            for (int i = 0; i < strings.Count; i++)
            {
                builder.Append(strings[i]);
                if (i < tokens.Count)
                    builder.Append(tokens[i].GetString());
            }
            var text = builder.ToString();
            builder.Clear();
            return text;
        }

        class Token
        {
            // Exclude Count/All
            private readonly static string[] s_accSlotTypes = Array.ConvertAll(Enum.GetValues<AccSlotType>()[..(int)AccSlotType.Count], slot => slot.ToString());
            private readonly static string[] s_accShotTypes = Array.ConvertAll(Enum.GetValues<AccShotType>()[..(int)AccShotType.Count], shot => shot.ToString());
            private readonly static string[] s_accStatTypes = Array.ConvertAll(Enum.GetValues<AccStatType>()[..(int)AccStatType.Count], stat => stat.ToString());
            private readonly static string[] s_damSlotTypes = Array.ConvertAll(Enum.GetValues<DamSlotType>()[..(int)DamSlotType.Count], slot => slot.ToString());
            private readonly static string[] s_damStatTypes = Array.ConvertAll(Enum.GetValues<DamStatType>()[..(int)DamStatType.Count], stat => stat.ToString());

            private readonly StatData.Retriever _base;
            private readonly StatData.Retriever? _divider;

            public Token(StatData data, string text, InventorySlot slot)
            {
                int divIndex = text.IndexOf('/');
                if (divIndex != -1)
                {
                    _divider = ParseRetriever(data, text[(divIndex + 1)..], slot);
                    text = text[..divIndex];
                }
                else
                    _divider = null;

                _base = ParseRetriever(data, text, slot);
            }

            public string GetString()
            {
                var baseValue = _base.Value;
                if (_divider != null)
                {
                    var divValue = _divider.Value;
                    if (divValue == 0) return baseValue > 0 ? "100%" : "-";
                    return ((double)baseValue / divValue).ToString("P0");
                }
                return baseValue.ToString();
            }
            
            private static StatData.Retriever ParseRetriever(StatData data, string text, InventorySlot slot)
            {
                return text.Contains("Damage", StringComparison.CurrentCultureIgnoreCase) ? ParseDamageRetriever(data, text, slot) : ParseAccuracyRetriever(data, text, slot);
            }

            private static StatData.Retriever ParseDamageRetriever(StatData data, string text, InventorySlot slot)
            {
                DamSlotType slotType;
                if (slot != InventorySlot.None)
                    slotType = slot.ToDamSlotType();
                else if (!TryGetEnumInText(text, s_damSlotTypes, out slotType))
                    slotType = DamSlotType.All;

                if (!TryGetEnumInText(text, s_damStatTypes, out DamStatType statType))
                    statType = DamStatType.Any;

                return StatData.CreateDamageRetriever(data, slotType, statType);
            }

            private static StatData.Retriever ParseAccuracyRetriever(StatData data, string text, InventorySlot slot)
            {
                AccSlotType slotType;
                if (slot != InventorySlot.None)
                    slotType = slot.ToAccSlotType();
                else if (!TryGetEnumInText(text, s_accSlotTypes, out slotType))
                    slotType = AccSlotType.All;

                if (!TryGetEnumInText(text, s_accShotTypes, out AccShotType shotType))
                    shotType = AccShotType.Shot;
                if (!TryGetEnumInText(text, s_accStatTypes, out AccStatType statType))
                    statType = AccStatType.Hit;

                return StatData.CreateAccuracyRetriever(data, slotType, shotType, statType);
            }

            private static bool TryGetEnumInText<T>(string text, string[] enumStrings, out T value) where T : struct
            {
                int index = -1;
                int length = 0;
                foreach (var str in enumStrings)
                {
                    if ((index = text.IndexOf(str)) > -1)
                    {
                        length = str.Length;
                        break;
                    }
                }

                if (index != -1)
                {
                    if (text.Substring(index, length).TryToEnum(out value))
                        return true;
                    else
                    {
                        DinoLogger.Error($"Unable to parse {typeof(T).Name} in token \"{text}\"");
                        return false;
                    }
                }

                value = default;
                return false;
            }
        }
    }
}
