using StatDisplay.Networking.Events;
using StatDisplay.Attributes;
using StatDisplay.Data;
using StatDisplay.Handler;

namespace StatDisplay.Networking
{
    internal static class StatSyncManager
    {
        private readonly static AccDeltaSync _accSync = new();
        private readonly static DamDeltaSync _damSync = new();

        [InvokeOnLoad]
        private static void Init()
        {
            _accSync.Setup();
            _damSync.Setup();
        }

        internal static void BroadcastAccData(ulong lookup, StatData data)
        {
            if (!data.PopRemoteAccDelta(out var deltaAccuracy)) return;

            foreach (var player in StatManager.RemotePlayers)
                _accSync.Send(pAccDeltaData.Create(lookup, deltaAccuracy), player);
        }

        internal static void BroadcastDamData(ulong lookup, StatData data)
        {
            if (!data.PopRemoteDamDelta(out var deltaDamage)) return;

            foreach (var player in StatManager.RemotePlayers)
                _damSync.Send(pDamDeltaData.Create(lookup, deltaDamage), player);
        }

        internal static void ReceiveData(pAccDeltaData data) => StatHandler.AddRemoteAccDelta(data.Lookup, data.DeltaData);
        internal static void ReceiveData(pDamDeltaData data) => StatHandler.AddRemoteDamDelta(data.Lookup, data.DeltaData);
    }
}
