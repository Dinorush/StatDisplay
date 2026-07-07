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
                StatManager.AddPlayer(player, true);
            else if (!player.IsBot)
            {
                _notifySync.Send(true, player, SNet_ChannelType.SessionOrderCritical);
                StatManager.AddPlayer(player, false);
            }
            else
                StatManager.AddPlayer(player, StatManager.MasterHasMod);
        }

        internal static void ReceiveNotify(SNet_Player player)
        {
            StatManager.AddPlayer(player, true);
        }

        internal static void OnRemovePlayer(SNet_Player player)
        {
            StatManager.RemovePlayer(player);
        }
    }
}
