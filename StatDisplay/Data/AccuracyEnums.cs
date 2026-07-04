namespace StatDisplay.Data
{
    public enum AccSlotType : byte
    {
        Primary, Main = Primary,
        Secondary, Special = Secondary,
        Count,
        All
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
