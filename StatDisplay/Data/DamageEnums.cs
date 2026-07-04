namespace StatDisplay.Data
{
    public enum DamSlotType : byte
    {
        Primary, Main = Primary,
        Secondary, Special = Secondary,
        Tool, Class = Tool,
        Melee,
        Count,
        All
    }

    public enum DamStatType : byte
    {
        Any,
        Crit,
        Count
    }
}
