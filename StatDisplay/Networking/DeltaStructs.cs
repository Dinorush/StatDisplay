using StatDisplay.Data;
using System.Runtime.InteropServices;

namespace StatDisplay.Networking
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct pAccDeltaData
    {
        private const int Count = (int)AccSlotType.Count * (int)AccShotType.Count * (int)AccStatType.Count;

        private fixed short _deltaData[Count];

        public static pAccDeltaData Create(short[,,] deltaData)
        {
            pAccDeltaData packet = new()
            {
                DeltaData = deltaData
            };
            return packet;
        }

        public short[,,] DeltaData
        {
            get
            {
                short[,,] delta = new short[(int)AccSlotType.Count, (int)AccShotType.Count, (int)AccStatType.Count];
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

        private fixed float _deltaData[Count];

        public static pDamDeltaData Create(float[,] deltaData)
        {
            pDamDeltaData packet = new()
            {
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
