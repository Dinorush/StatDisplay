using HarmonyLib;
using SNetwork;
using StatDisplay.Handler;

namespace StatDisplay.Patches
{
    [HarmonyPatch]
    internal static class CharacterSlotPatches
    {
        [HarmonyPatch(typeof(SNet_SyncManager), nameof(SNet_SyncManager.OnSyncPlayerData_Session))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void Pre_SyncData(pPlayerData_Session data, ref (SNet_Player player, int index) __state)
        {
            __state.index = data.characterSlotIndex;
            if (__state.index == -1) return;

            if (!data.player.GetPlayer(out __state.player) || __state.player.Session.characterSlotIndex == __state.index)
                __state.index = -1;
        }

        [HarmonyPatch(typeof(SNet_SyncManager), nameof(SNet_SyncManager.OnSyncPlayerData_Session))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_SyncData((SNet_Player player, int index) __state)
        {
            if (__state.index == -1 || __state.player.CharacterIndex != __state.index) return;
            StatHandler.RefreshMeshList();
        }
    }
}
