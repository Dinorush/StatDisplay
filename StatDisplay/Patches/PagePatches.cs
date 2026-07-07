using CellMenu;
using HarmonyLib;
using Localization;
using Player;
using SNetwork;
using StatDisplay.Attributes;
using StatDisplay.Data;
using StatDisplay.Handler;
using System;
using System.Text;
using UnityEngine;

namespace StatDisplay.Patches
{
    [HarmonyPatch]
    internal static class PagePatches
    {
        [InvokeOnAssetLoad]
        private static void OnAssetLoad()
        {
            _isReady = true;
        }

        private static bool _isReady = false;

        [HarmonyPatch(typeof(CM_PageExpeditionSuccess), nameof(CM_PageExpeditionSuccess.Setup))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_PageSetup(CM_PageExpeditionSuccess __instance)
        {
            foreach (var report in __instance.m_playerReports)
            {
                var size = report.m_gear.rectTransform.sizeDelta;
                size.y += 20;
                report.m_gear.rectTransform.sizeDelta = size;
                size = report.m_eval.rectTransform.sizeDelta;
                size.y += 20;
                report.m_eval.rectTransform.sizeDelta = size;
            }
        }

        [HarmonyPatch(typeof(CM_PageExpeditionSuccess), nameof(CM_PageExpeditionSuccess.OnEnable))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_PageEnable(CM_PageExpeditionSuccess __instance)
        {
            if (!_isReady || SNet.Slots.PlayerSlots == null) return;

            for (int i = 0; i < SNet.Slots.PlayerSlots.Length; i++)
            {
                var slot = SNet.Slots.PlayerSlots[i];
                if (slot == null || slot.player == null || !slot.player.IsInSlot) continue;
                var player = slot.player;
                PopulateReport(__instance, __instance.m_playerReports[i], player, false, i);
            }
        }

        private readonly static CM_PageSuccess_PrisonerEvaluation[] _failReports = new CM_PageSuccess_PrisonerEvaluation[4];
        private static IntPtr _lastFailPage = IntPtr.Zero;
        [HarmonyPatch(typeof(CM_PageExpeditionFail), nameof(CM_PageExpeditionFail.OnEnable))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_PageEnable(CM_PageExpeditionFail __instance)
        {
            if (!_isReady || SNet.Slots.PlayerSlots == null) return;

            CreateFailReports(__instance);
            CM_PageExpeditionSuccess successPage = MainMenuGuiLayer.Current.PageExpeditionSuccess;

            UnityEngine.Random.InitState(LevelGeneration.Builder.SessionSeedRandom.Seed);
            for (int i = 0; i < SNet.Slots.PlayerSlots.Length; i++)
            {
                var slot = SNet.Slots.PlayerSlots[i];
                var report = _failReports[i];
                report.SetVisible(false);
                if (slot == null || slot.player == null || !slot.player.IsInSlot) continue;
                var player = slot.player;
                PopulateReport(successPage, report, player, true, i);
            }
        }

        private static void CreateFailReports(CM_PageExpeditionFail __instance)
        {
            // In case a mod changes the fail page
            if (_lastFailPage == __instance.Pointer) return;
            _lastFailPage = __instance.Pointer;

            Transform contentHolder = __instance.m_staticContentHolder.transform;
            if (contentHolder.FindChild(EntryPoint.MODNAME + "_Align0")) return;

            // HACK - Want to set inactive, but it blinks in...
            __instance.m_artifactInfo_text.transform.localPosition = new Vector3(0, 2000, 0);

            __instance.m_staticContentHolder.localPosition = new Vector3(0, 100, 0);
            __instance.m_ArtifactInventoryDisplay.transform.localPosition = new Vector3(0, 100, 0);
            Transform[] aligns = new Transform[4];

            for (int i = 0; i < aligns.Length; i++)
            {
                var go = new GameObject(EntryPoint.MODNAME + "_Align" + i);
                GameObject.DontDestroyOnLoad(go);
                aligns[i] = go.transform;
                aligns[i].SetParent(contentHolder, false);
                aligns[i].localPosition = new Vector3(-900 + 600 * i, -250, 0);
            }

            GameObject basePrefab = MainMenuGuiLayer.Current.PageExpeditionSuccess.m_playerReportPrefab;
            for (int i = 0; i < _failReports.Length; i++)
            {
                Transform parentAlign = __instance.transform;
                if (i < aligns.Length)
                    parentAlign = aligns[i];
                _failReports[i] = GOUtil.SpawnChildAndGetComp<CM_PageSuccess_PrisonerEvaluation>(basePrefab, parentAlign);
                _failReports[i].transform.localPosition = Vector3.zero;
                _failReports[i].transform.localRotation = Quaternion.identity;
                _failReports[i].transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
                var size = _failReports[i].m_gear.rectTransform.sizeDelta;
                size.y += 20;
                _failReports[i].m_gear.rectTransform.sizeDelta = size;
                _failReports[i].SetVisible(visible: false);
            }
        }

        private static void PopulateReport(CM_PageExpeditionSuccess successPage, CM_PageSuccess_PrisonerEvaluation report, SNet_Player player, bool isFail, int index)
        {
            bool hasData = StatHandler.TryGetData(player, out var data);
            if (!hasData && !isFail) return;

            if (!PlayerBackpackManager.TryGetBackpack(player, out var backpack)) return;

            if (isFail)
            {
                report.SetColor(new(1f, 1f, 1f, 0.65f));
                report.m_name.text = "<color=red>" + (player.IsBot ? $"[{Text.Get(1372u)}]" : "") + player.GetName() + "</color>";
                report.m_gearHeader.text = "<size=150%><nobr>" + Text.Get(888u) + "</nobr></size>";
            }

            bool hasMainName = false;
            bool hasSpecialName = false;
            StringBuilder sb = new("<color=white>");
            if (successPage.TryGetArchetypeName(backpack, InventorySlot.GearStandard, out string mainName))
            {
                hasMainName = true;
                sb.AppendLine(mainName + GetDamageText(data, DamSlotType.Main));
            }
            if (successPage.TryGetArchetypeName(backpack, InventorySlot.GearSpecial, out string specialName))
            {
                hasSpecialName = true;
                sb.AppendLine(specialName + GetDamageText(data, DamSlotType.Special));
            }
            if (successPage.TryGetArchetypeName(backpack, InventorySlot.GearClass, out string archName))
            {
                sb.AppendLine(archName + GetDamageText(data, DamSlotType.Class));
            }
            if (successPage.TryGetArchetypeName(backpack, InventorySlot.GearMelee, out archName))
            {
                sb.AppendLine(archName + GetDamageText(data, DamSlotType.Melee));
            }

            if (hasData)
                sb.Append($"Total: {GetDamageText(data, DamSlotType.All, includePipe: false)}");
            sb.Append("</color>");
            report.m_gear.text = sb.ToString();

            sb.Clear();

            if (isFail)
                report.m_evalHeader.text = "<size=150%><nobr>" + Text.Get(898u) + "</nobr></size>";
            else
            {
                var agent = player.PlayerAgent.Cast<PlayerAgent>();
                string healthText;
                float healthRel = agent.Damage.GetHealthRel();
                if (healthRel > 0.7f)
                    healthText = Text.Get(889u);
                else if (healthRel > 0.5f)
                    healthText = Text.Get(890u);
                else if (healthRel > 0.3f)
                    healthText = Text.Get(891u);
                else if (healthRel > 0f)
                    healthText = "<color=red>" + Text.Get(892u) + "</color>";
                else
                    healthText = "<color=red>" + Text.Get(893u) + "</color>";

                float infection = agent.Damage.Infection;
                string infectionText = ((infection > 0.85f) ? Text.Get(894u) : ((infection > 0.5f) ? Text.Get(895u) : ((!(infection > 0f)) ? Text.Get(897u) : Text.Get(896u))));

                sb.AppendLine(Text.Format(899u, "<color=white>" + healthText + "</color>"));
                sb.AppendLine(Text.Format(900u, "<color=white>" + infectionText + "</color>"));
            }

            if (hasData && data!.HasMod)
            {
                if (hasMainName)
                    sb.AppendLine(mainName + $"<color=white>: {data.StatText.GetEndscreenText(InventorySlot.GearStandard)}</color>");
                if (hasSpecialName)
                    sb.AppendLine(specialName + $"<color=white>: {data.StatText.GetEndscreenText(InventorySlot.GearSpecial)}</color>");
                // HACK - Any invalid InventorySlot uses SlotType.All, and the internal restriction check is only != None.
                sb.Append($"Total: {data.StatText.GetEndscreenText(InventorySlot.ResourcePack)}</color>");
            }
            else
            {
                sb.Append($"Missing StatDisplay");
            }

            report.m_eval.text = sb.ToString();
            if (isFail)
                CoroutineManager.BlinkIn(report.gameObject, 3f + index * 0.2f);
        }

        private static string GetDamageText(StatData? data, DamSlotType slot, bool includePipe = true)
        {
            if (data == null) return "";
            var pipeString = includePipe ? " | " : "";
            if (!data.HasMod && !StatManager.MasterHasMod) return pipeString + "N/A";
            return pipeString + $"<#a0a0a0>{(ulong)data.GetDamage(slot, DamStatType.Any)}</color> (<#ffff00>{(ulong)data.GetDamage(slot, DamStatType.Crit)}</color>)";
        }
    }
}
