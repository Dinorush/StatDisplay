using System;

namespace StatDisplay.Data
{
    public enum AccSlotType : byte
    {
        Primary,
        Secondary,
        Count,
        All
    }

    [Flags]
    public enum AccWeightType : byte
    {
        None = 0,
        Pellet = 1,
        Ammo = 1 << 1,
        All = Pellet | Ammo
    }

    public enum AccShotType : byte
    {
        Shot,
        Group,
        Full,
        Count
    }

    public enum AccStatType : byte
    {
        Hit,
        Crit,
        Fired,
        Count
    }
}
