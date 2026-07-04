using HarmonyLib;
using StatDisplay.Handler;

namespace StatDisplay.Patches
{
    [HarmonyPatch]
    internal static class WOPatches
    {
        [HarmonyPatch(typeof(WardenObjectiveManager), nameof(WardenObjectiveManager.OnWinConditionSolved))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_WinSolved()
        {
            StatHandler.ForceSync();
        }
    }
}
