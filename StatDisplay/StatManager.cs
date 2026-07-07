using StatDisplay.Handler;
using SNetwork;
using System.Collections.Generic;
using StatDisplay.Attributes;
using System;

namespace StatDisplay
{
    public static class StatManager
    {
        private static readonly Dictionary<ulong, SNet_Player> _remotePlayers = new();
        public static IReadOnlyCollection<SNet_Player> RemotePlayers => _remotePlayers.Values;
        private static bool _masterHasMod = false;
        public static bool MasterHasMod
        {
            get => _masterHasMod;
            private set
            {
                if (_masterHasMod == value) return;
                _masterHasMod = value;

                foreach (var slot in SNet.Slots.PlayerSlots)
                    if (slot.player != null && slot.player.IsBot)
                        StatHandler.AddPlayer(slot.player, MasterHasMod, onlyIfExists: true);
            }
        }
        public static bool PlayerHasMod(SNet_Player player)
        {
            if (player.IsLocal || (MasterHasMod && player.IsBot)) return true;
            return _remotePlayers.ContainsKey(player.Lookup);
        }

        [InvokeOnLoad]
        private static void Init()
        {
            SNet_Events.OnMasterChanged += (Action)OnMasterSet;
        }

        internal static void AddPlayer(SNet_Player player, bool hasMod)
        {
            if (hasMod)
            {
                if (player.IsMaster)
                    MasterHasMod = true;
                if (!player.IsLocal && !player.IsBot)
                    _remotePlayers.TryAdd(player.Lookup, player);
            }
            StatHandler.AddPlayer(player, hasMod);
        }

        internal static void RemovePlayer(SNet_Player player)
        {
            StatHandler.RemovePlayer(player);
            _remotePlayers.Remove(player.Lookup);
        }

        internal static void OnMasterSet()
        {
            MasterHasMod = SNet.IsMaster || _remotePlayers.ContainsKey(SNet.Master.Lookup);
            StatHandler.OnMasterSet();
        }

        internal static void Clear()
        {
            _masterHasMod = false;
            _remotePlayers.Clear();
            StatHandler.Clear();
        }
    }
}
