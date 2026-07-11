using StatDisplay.Networking;
using HarmonyLib;
using SNetwork;
using Player;

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

        [HarmonyPatch(typeof(PlayerSync), nameof(PlayerSync.OnSpawn))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_PlayerSpawn(PlayerSync __instance)
        {
            StatManager.AddPlayer(__instance.Replicator.OwningPlayer);
        }

        [HarmonyPatch(typeof(PlayerAgent), nameof(PlayerAgent.OnDespawn))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void Pre_PlayerDespawn(PlayerAgent __instance)
        {
            StatManager.RemovePlayer(__instance.Owner);
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
