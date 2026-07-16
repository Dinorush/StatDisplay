using StatDisplay.Handler;
using SNetwork;
using System.Collections.Generic;

namespace StatDisplay
{
    public static class StatManager
    {
        private static readonly HashSet<ulong> _moddedPlayers = new();
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
                        SetModdedPlayer(slot.player, MasterHasMod);
            }
        }
        public static bool PlayerHasMod(SNet_Player player)
        {
            if (player.IsLocal || (MasterHasMod && player.IsBot)) return true;
            return _remotePlayers.ContainsKey(player.Lookup);
        }

        internal static void AddPlayer(SNet_Player player)
        {
            StatHandler.AddPlayer(player, _moddedPlayers.Contains(player.Lookup));
        }

        internal static void SetModdedPlayer(SNet_Player player, bool hasMod = true)
        {
            if (hasMod)
            {
                if (!_moddedPlayers.Add(player.Lookup)) return;

                if (player.IsMaster)
                    MasterHasMod = hasMod;

                if (!player.IsLocal && !player.IsBot)
                    _remotePlayers.TryAdd(player.Lookup, player);
            }
            else if (!hasMod)
            {
                if (!_moddedPlayers.Remove(player.Lookup)) return;

                if (!player.IsLocal && !player.IsBot)
                    _remotePlayers.Remove(player.Lookup);
            }

            if (StatHandler.TryGetData(player, out var data))
            {
                data.HasMod = hasMod;
                if (player.CharacterIndex != -1)
                    StatHandler.RefreshMeshList();
            }
        }

        internal static void RemovePlayer(SNet_Player player)
        {
            StatHandler.RemovePlayer(player);
        }

        internal static void OnMasterSet()
        {
            MasterHasMod = SNet.IsMaster || _moddedPlayers.Contains(SNet.Master.Lookup);
            StatHandler.RefreshMeshList();
        }

        internal static void Clear()
        {
            _masterHasMod = false;
            _remotePlayers.Clear();
            StatHandler.Clear();
        }
    }
}
