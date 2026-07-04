using StatDisplay.Networking;
using HarmonyLib;
using SNetwork;
using Player;

namespace StatDisplay.Patches
{
    [HarmonyPatch]
    internal static class NetworkPatches
    {
        [HarmonyPatch(typeof(PlayerSync), nameof(PlayerSync.OnSpawn))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_PlayerSpawn(PlayerSync __instance)
        {
            ModSyncManager.OnAddPlayer(__instance.Replicator.OwningPlayer);
        }

        [HarmonyPatch(typeof(PlayerAgent), nameof(PlayerAgent.OnDespawn))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void Pre_PlayerDespawn(PlayerAgent __instance)
        {
            ModSyncManager.OnRemovePlayer(__instance.Owner);
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
