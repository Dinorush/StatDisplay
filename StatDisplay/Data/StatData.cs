using SNetwork;
using System.Diagnostics.CodeAnalysis;

namespace StatDisplay.Data
{
    public sealed class StatData
    {
        public abstract class Retriever
        {
            protected readonly StatData _data;
            public Retriever(StatData data) => _data = data;
            public abstract ulong Value { get; }
        }

        private readonly ulong[,,] _accData;
        private readonly ushort[,,] _deltaAccData;
        private readonly double[,] _damData;
        private readonly float[,] _deltaDamData;
        private bool _needsAccSync = false;
        private bool _needsDamSync = false;
        public StatParser StatText { get; private set; }

        public StatData(SNet_Player player)
        {
            _accData = new ulong[(int)AccSlotType.Count, (int)AccShotType.Count, (int)AccStatType.Count];
            _deltaAccData = new ushort[(int)AccSlotType.Count, (int)AccShotType.Count, (int)AccStatType.Count];
            _damData = new double[(int)DamSlotType.Count, (int)DamStatType.Count];
            _deltaDamData = new float[(int)DamSlotType.Count, (int)DamStatType.Count];

            StatText = new StatParser(this, player);
        }

        public ulong GetAccuracy(AccSlotType slot, AccShotType shot, AccStatType stat)
        {
            ulong value;
            if (slot == AccSlotType.All)
                value = _accData[(int)AccSlotType.Main, (int)shot, (int)stat] + _accData[(int)AccSlotType.Special, (int)shot, (int)stat];
            else
                value = _accData[(int)slot, (int)shot, (int)stat];
            return value;
        }

        public double GetDamage(DamSlotType slot, DamStatType stat)
        {
            double value = 0;
            if (slot == DamSlotType.All)
            {
                for (int i = 0; i < (int)DamSlotType.Count; i++)
                    value += _damData[i, (int)stat];
            }
            else
                value += _damData[(int)slot, (int)stat];
            return value;
        }

        public void Reset()
        {
            for (int slot = 0; slot < _accData.GetLength(0); slot++)
            {
                for (int shot = 0; shot < _accData.GetLength(1); shot++)
                {
                    for (int stat = 0; stat < _accData.GetLength(2); stat++)
                    {
                        _accData[slot, shot, stat] = 0;
                        _deltaAccData[slot, shot, stat] = 0;
                    }
                }
            }
            _needsAccSync = false;
            _needsDamSync = false;
            StatText.Update(true);
        }

        public void AddAccuracy(AccSlotType slot, ushort[,] deltaData)
        {
            _needsAccSync = true;
            for (int shot = 0; shot < deltaData.GetLength(0); shot++)
            {
                for (int stat = 0; stat < deltaData.GetLength(1); stat++)
                {
                    ushort val = deltaData[shot, stat];
                    _accData[(int)slot, shot, stat] += val;
                    _deltaAccData[(int)slot, shot, stat] += val;
                }
            }

            StatText.Update();
        }

        public void AddDamage(DamSlotType slot, DamStatType stat, float value)
        {
            _needsDamSync = true;
            _damData[(int)slot, (int)stat] += value;
            _deltaDamData[(int)slot, (int)stat] += value;
            if (stat != DamStatType.Any)
            {
                _damData[(int)slot, 0] += value;
                _deltaDamData[(int)slot, 0] += value;
            }

            StatText.Update();
        }

        public bool PopRemoteAccDelta([MaybeNullWhen(false)] out ushort[,,] deltaAccuracy)
        {
            if (!_needsAccSync)
            {
                deltaAccuracy = null;
                return false;
            }

            deltaAccuracy = (ushort[,,])_deltaAccData.Clone();
            for (int slot = 0; slot < _deltaAccData.GetLength(0); slot++)
                for (int shot = 0; shot < _deltaAccData.GetLength(1); shot++)
                    for (int stat = 0; stat < _deltaAccData.GetLength(2); stat++)
                        _deltaAccData[slot, shot, stat] = 0;

            _needsAccSync = false;
            return true;
        }

        public bool PopRemoteDamDelta([MaybeNullWhen(false)] out float[,] deltaDamage)
        {
            if (!_needsDamSync)
            {
                deltaDamage = null;
                return false;
            }

            deltaDamage = (float[,])_deltaDamData.Clone();
            for (int slot = 0; slot < _deltaDamData.GetLength(0); slot++)
                for (int stat = 0; stat < _deltaDamData.GetLength(1); stat++)
                    _deltaDamData[slot, stat] = 0;

            _needsDamSync = false;
            return true;
        }

        public void AddRemoteAccDelta(ushort[,,] deltaData)
        {
            for (int slot = 0; slot < deltaData.GetLength(0); slot++)
                for (int shot = 0; shot < deltaData.GetLength(1); shot++)
                    for (int stat = 0; stat < deltaData.GetLength(2); stat++)
                        _accData[slot, shot, stat] += deltaData[slot, shot, stat];

            StatText.Update(true);
        }

        public void AddRemoteDamDelta(float[,] deltaDamage)
        {
            for (int slot = 0; slot < deltaDamage.GetLength(0); slot++)
                for (int stat = 0; stat < deltaDamage.GetLength(1); stat++)
                    _damData[slot, stat] += deltaDamage[slot, stat];

            StatText.Update(true);
        }

        public static Retriever CreateAccuracyRetriever(StatData data, AccSlotType slotType, AccShotType shotType, AccStatType statType) => new AccuracyRetriever(data, slotType, shotType, statType);
        public static Retriever CreateDamageRetriever(StatData data, DamSlotType slotType, DamStatType statType) => new DamageRetriever(data, slotType, statType);

        class AccuracyRetriever : Retriever
        {
            private readonly int _slotType;
            private readonly int _shotType;
            private readonly int _statType;
            private readonly bool _isAllSlot;
            public AccuracyRetriever(StatData data, AccSlotType slotType, AccShotType shotType, AccStatType statType) : base(data)
            {
                _slotType = (int)slotType;
                _shotType = (int)shotType;
                _statType = (int)statType;
                _isAllSlot = slotType == AccSlotType.All;
            }

            public override ulong Value
            {
                get
                {
                    ulong value;
                    if (_isAllSlot)
                        value = _data._accData[(int)AccSlotType.Main, _shotType, _statType] + _data._accData[(int)AccSlotType.Special, _shotType, _statType];
                    else
                        value = _data._accData[_slotType, _shotType, _statType];
                    return value;
                }
            }
        }

        class DamageRetriever : Retriever
        {
            private readonly int _slotType;
            private readonly int _statType;
            private readonly bool _isAllSlot;
            public DamageRetriever(StatData data, DamSlotType slotType, DamStatType statType) : base(data)
            {
                _slotType = (int)slotType;
                _statType = (int)statType;
                _isAllSlot = slotType == DamSlotType.All;
            }
            public override ulong Value
            {
                get
                {
                    double value = 0;
                    if (_isAllSlot)
                    {
                        for (int i = 0; i < (int)DamSlotType.Count; i++)
                            value += _data._damData[i, _statType];
                    }
                    else
                        value += _data._damData[_slotType, _statType];
                    return (ulong)value;
                }
            }
        }
    }
}
