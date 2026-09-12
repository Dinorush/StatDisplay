using OptionalNetworking;
using SNetwork;
using StatDisplay.Attributes;
using StatDisplay.Data;
using StatDisplay.Handler;
using System.Runtime.InteropServices;

namespace StatDisplay.Networking
{
    internal static class StatSyncManager
    {
        private readonly static CustomDataSender<pAccDeltaData> _accSender;
        private readonly static CustomDataSender<pDamDeltaData> _damSender;

        static StatSyncManager()
        {
            _accSender = NetworkManager.ModInfo.AddCustomDataSender<pAccDeltaData>(ReceiveData, null, SenderMode.OtherPlayers);
            _damSender = NetworkManager.ModInfo.AddCustomDataSender<pDamDeltaData>(ReceiveData, null, SenderMode.OtherPlayers);
        }

        [InvokeOnLoad]
        private static void Init()
        {

        }

        internal static void BroadcastAccData(StatData data)
        {
            if (!data.PopRemoteAccDelta(out var deltaAccuracy)) return;

            _accSender.SendData(data.Owner, pAccDeltaData.Create(deltaAccuracy));
        }

        internal static void BroadcastDamData(StatData data)
        {
            if (!data.PopRemoteDamDelta(out var deltaDamage)) return;

            _damSender.SendData(data.Owner, pDamDeltaData.Create(deltaDamage));
        }

        internal static void ReceiveData(SNet_Player player, pAccDeltaData data) => StatHandler.AddRemoteAccDelta(player.Lookup, data.DeltaData);
        internal static void ReceiveData(SNet_Player player, pDamDeltaData data) => StatHandler.AddRemoteDamDelta(player.Lookup, data.DeltaData);
    }
}
