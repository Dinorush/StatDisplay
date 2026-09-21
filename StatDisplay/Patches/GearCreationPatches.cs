using HarmonyLib;
using Player;
using StatDisplay.Dependencies;
using StatDisplay.Handler;
using StatDisplay.Utils.Extensions;

namespace StatDisplay.Patches
{
    internal class GearCreationPatches
    {
        public static void Init(Harmony harmony)
        {
            // The patch runs before EWC sets up custom weapon stuff, but running after EWC would be a pain, so rely on EWC callback if it exists instead.
            if (!EWCWrapper.HasEWC)
                harmony.PatchAll(typeof(GearCreationPatches));
        }

        [HarmonyPatch(typeof(PlayerBackpack), nameof(PlayerBackpack.CreateAndStoreBackpackItem))]
        [HarmonyPostfix]
        private static void Post_AddItem(PlayerBackpack __instance, Item item, InventorySlot slot)
        {
            if (slot != InventorySlot.GearStandard && slot != InventorySlot.GearSpecial) return;
            if (!StatHandler.TryGetData(__instance.Owner, out var data)) return;

            data.SetAccuracyWeights(item, slot.ToAccSlotType());
        }
    }
}
