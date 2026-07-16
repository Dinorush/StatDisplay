using StatDisplay.Networking;
using HarmonyLib;
using SNetwork;
using Player;
using StatDisplay.Handler;

namespace StatDisplay.Patches
{
    [HarmonyPatch]
    internal static class NetworkPatches
    {
        [HarmonyPatch(typeof(SNet_SessionHub), nameof(SNet_SessionHub.AddPlayerToSession))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_AddPlayer(SNet_Player player)
        {
            ModSyncManager.OnAddPlayer(player);
        }

        [HarmonyPatch(typeof(SNet_SessionHub), nameof(SNet_SessionHub.OnLeftLobby))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_RemovePlayer(SNet_Player player)
        {
            ModSyncManager.OnRemovePlayer(player);
        }

        [HarmonyPatch(typeof(SNet_SyncManager), nameof(SNet_SyncManager.OnFoundMaster))]
        [HarmonyPatch(typeof(SNet_SyncManager), nameof(SNet_SyncManager.OnFoundNewMasterDuringMigration))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_FoundMaster()
        {
            StatManager.OnMasterSet();
        }

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

        [HarmonyPatch(typeof(PlayerSync), nameof(PlayerSync.OnSpawn))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_PlayerSpawn(PlayerSync __instance)
        {
            if (__instance.Replicator.OwningPlayer.CharacterIndex != -1)
                StatHandler.RefreshMeshList();
        }

        [HarmonyPatch(typeof(PlayerAgent), nameof(PlayerAgent.OnDespawn))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void Pre_PlayerDespawn()
        {
            StatHandler.RefreshMeshList();
        }

        [HarmonyPatch(typeof(SNet_SessionHub), nameof(SNet_SessionHub.LeaveHub))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_LeaveHub()
        {
            StatManager.Clear();
        }
    }
}
