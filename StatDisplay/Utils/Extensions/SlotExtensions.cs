using StatDisplay.Data;
using Player;

namespace StatDisplay.Utils.Extensions
{
    internal static class SlotExtensions
    {
        public static AccSlotType ToAccSlotType(this AmmoType ammo)
        {
            return ammo switch
            {
                AmmoType.Standard => AccSlotType.Primary,
                AmmoType.Special => AccSlotType.Secondary,
                _ => AccSlotType.All
            };
        }

        public static DamSlotType ToDamSlotType(this AmmoType slot)
        {
            return slot switch
            {
                AmmoType.Standard => DamSlotType.Primary,
                AmmoType.Special => DamSlotType.Secondary,
                AmmoType.Class => DamSlotType.Tool,
                AmmoType.None => DamSlotType.Melee,
                _ => DamSlotType.All
            };
        }

        public static AccSlotType ToAccSlotType(this InventorySlot slot)
        {
            return slot switch
            {
                InventorySlot.GearStandard => AccSlotType.Primary,
                InventorySlot.GearSpecial => AccSlotType.Secondary,
                _ => AccSlotType.All
            };
        }

        public static DamSlotType ToDamSlotType(this InventorySlot slot)
        {
            return slot switch
            {
                InventorySlot.GearStandard => DamSlotType.Primary,
                InventorySlot.GearSpecial => DamSlotType.Secondary,
                InventorySlot.GearClass => DamSlotType.Tool,
                InventorySlot.GearMelee => DamSlotType.Melee,
                _ => DamSlotType.All
            };
        }

        public static InventorySlot ToInventorySlot(this AmmoType ammo)
        {
            return ammo switch
            {
                AmmoType.Standard => InventorySlot.GearStandard,
                AmmoType.Special => InventorySlot.GearSpecial,
                AmmoType.Class => InventorySlot.GearClass,
                AmmoType.ResourcePackRel => InventorySlot.ResourcePack,
                _ => InventorySlot.None
            };
        }

        public static AmmoType ToAmmoType(this InventorySlot slot)
        {
            return slot switch
            {
                InventorySlot.GearStandard=> AmmoType.Standard,
                InventorySlot.GearSpecial => AmmoType.Special,
                InventorySlot.GearClass => AmmoType.Class,
                InventorySlot.ResourcePack => AmmoType.ResourcePackRel,
                _ => AmmoType.None
            };
        }
    }
}
