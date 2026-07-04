using StatDisplay.Handler;
using SNetwork;
using System.Collections.Generic;
using StatDisplay.Attributes;
using System;

namespace StatDisplay
{
    public static class StatManager
    {
        private static readonly Dictionary<ulong, SNet_Player> _modPlayers = new();
        private static readonly Dictionary<ulong, SNet_Player> _remotePlayers = new();
        public static IReadOnlyCollection<SNet_Player> RemotePlayers => _remotePlayers.Values;
        public static bool MasterHasMod { get; private set; }
        public static bool PlayerHasMod(SNet_Player player) => _modPlayers.ContainsKey(player.Lookup);

        [InvokeOnLoad]
        private static void Init()
        {
            SNet_Events.OnMasterChanged += (Action)OnMasterSet;
        }

        internal static void AddPlayer(SNet_Player player)
        {
            if (_modPlayers.TryAdd(player.Lookup, player))
            {
                StatHandler.AddPlayer(player);
                if (!player.IsLocal && !player.IsBot)
                    _remotePlayers.Add(player.Lookup, player);

                if (player.IsMaster)
                {
                    MasterHasMod = true;
                    foreach (var slot in SNet.Slots.PlayerSlots)
                        if (slot.player != null && slot.player.IsBot)
                            AddPlayer(slot.player);
                }
            }
        }

        internal static void RemovePlayer(SNet_Player player)
        {
            if (_modPlayers.Remove(player.Lookup))
            {
                StatHandler.RemovePlayer(player);
                _remotePlayers.Remove(player.Lookup);
                if (player.IsMaster)
                {
                    MasterHasMod = false;
                    foreach (var slot in SNet.Slots.PlayerSlots)
                        if (slot.player != null && slot.player.IsBot)
                            AddPlayer(slot.player);
                }
            }
        }

        internal static void OnMasterSet()
        {
            MasterHasMod = _modPlayers.ContainsKey(SNet.Master.Lookup);
        }

        internal static void Clear()
        {
            MasterHasMod = false;
            _modPlayers.Clear();
            _remotePlayers.Clear();
            StatHandler.Clear();
        }
    }
}
