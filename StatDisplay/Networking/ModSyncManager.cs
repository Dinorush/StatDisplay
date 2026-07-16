using StatDisplay.Attributes;
using SNetwork;
using StatDisplay.Networking.Events;

namespace StatDisplay.Networking
{
    internal static class ModSyncManager
    {
        private readonly static NotifySync _notifySync = new();

        [InvokeOnLoad]
        private static void Init()
        {
            _notifySync.Setup();
        }

        internal static void OnAddPlayer(SNet_Player player)
        {
            if (player.IsLocal)
                StatManager.SetModdedPlayer(player);
            else if (!player.IsBot)
                _notifySync.Send(true, player, SNet_ChannelType.SessionOrderCritical);
            else
                StatManager.SetModdedPlayer(player, StatManager.MasterHasMod);
            StatManager.AddPlayer(player);
        }

        internal static void ReceiveNotify(SNet_Player player)
        {
            StatManager.SetModdedPlayer(player);
        }

        internal static void OnRemovePlayer(SNet_Player player)
        {
            StatManager.SetModdedPlayer(player, false);
        }
    }
}
