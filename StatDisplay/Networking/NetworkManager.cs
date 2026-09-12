using OptionalNetworking;

namespace StatDisplay.Networking
{
    internal static class NetworkManager
    {
        public static ModInfo ModInfo { get; }

        public static bool MasterHasMod => ModInfo.MasterHasMod;

        static NetworkManager()
        {
            ModInfo = OptionalNetworkAPI.RegisterMod(EntryPoint.MODNAME);
        }
    }
}
