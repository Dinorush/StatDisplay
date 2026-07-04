using SNetwork;

namespace StatDisplay.Networking.Events
{
    internal sealed class NotifySync : SyncedEvent<bool>
    {
        public override string GUID => "NOTIF";

        protected override void Receive(ulong lookup, bool _)
        {
            if (SNet.TryGetPlayer(lookup, out SNet_Player player))
                ModSyncManager.ReceiveNotify(player);
        }
    }
}
