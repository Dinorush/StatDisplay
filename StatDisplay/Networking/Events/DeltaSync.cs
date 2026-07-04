using StatDisplay.Data;
using System.Runtime.InteropServices;

namespace StatDisplay.Networking.Events
{
    internal sealed class AccDeltaSync : SyncedEvent<pAccDeltaData>
    {
        public override string GUID => "ACCSYN";

        protected override void Receive(ulong lookup, pAccDeltaData packet)
        {
            StatSyncManager.ReceiveData(packet);
        }
    }

    internal sealed class DamDeltaSync : SyncedEvent<pDamDeltaData>
    {
        public override string GUID => "DAMSYN";

        protected override void Receive(ulong lookup, pDamDeltaData packet)
        {
            StatSyncManager.ReceiveData(packet);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct pAccDeltaData
    {
        private const int Count = (int)AccSlotType.Count * (int)AccShotType.Count * (int)AccStatType.Count;

        public ulong Lookup;
        private fixed ushort _deltaData[Count];

        public static pAccDeltaData Create(ulong lookup, ushort[,,] deltaData)
        {
            pAccDeltaData packet = new()
            {
                Lookup = lookup,
                DeltaData = deltaData
            };
            return packet;
        }

        public ushort[,,] DeltaData
        {
            get
            {
                ushort[,,] delta = new ushort[(int)AccSlotType.Count, (int)AccShotType.Count, (int)AccStatType.Count];
                int count = 0;
                for (int slot = 0; slot < delta.GetLength(0); slot++)
                    for (int shot = 0; shot < delta.GetLength(1); shot++)
                        for (int stat = 0; stat < delta.GetLength(2); stat++)
                            delta[slot, shot, stat] = _deltaData[count++];
                return delta;
            }
            set
            {
                int count = 0;
                for (int slot = 0; slot < value.GetLength(0); slot++)
                    for (int shot = 0; shot < value.GetLength(1); shot++)
                        for (int stat = 0; stat < value.GetLength(2); stat++)
                            _deltaData[count++] = value[slot, shot, stat];
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct pDamDeltaData
    {
        private const int Count = (int)DamSlotType.Count * (int)DamStatType.Count;

        public ulong Lookup;
        private fixed float _deltaData[Count];

        public static pDamDeltaData Create(ulong lookup, float[,] deltaData)
        {
            pDamDeltaData packet = new()
            {
                Lookup = lookup,
                DeltaData = deltaData,
            };
            return packet;
        }

        public float[,] DeltaData
        {
            get
            {
                float[,] delta = new float[(int)DamSlotType.Count, (int)DamStatType.Count];
                int count = 0;
                for (int slot = 0; slot < delta.GetLength(0); slot++)
                    for (int stat = 0; stat < delta.GetLength(1); stat++)
                        delta[slot, stat] = _deltaData[count++];
                return delta;
            }
            set
            {
                int count = 0;
                for (int slot = 0; slot < value.GetLength(0); slot++)
                    for (int stat = 0; stat < value.GetLength(1); stat++)
                        _deltaData[count++] = value[slot, stat];
            }
        }
    }
}
