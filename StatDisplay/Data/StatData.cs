using SNetwork;
using System;
using System.Diagnostics.CodeAnalysis;

namespace StatDisplay.Data
{
    public sealed class StatData
    {
        public abstract class Retriever
        {
            protected readonly StatData _data;
            public Retriever(StatData data) => _data = data;
            public abstract long Value { get; }
        }

        private readonly long[,,] _accData;
        private readonly short[,,] _deltaAccData;
        private readonly double[,] _damData;
        private readonly float[,] _deltaDamData;
        private bool _needsAccSync = false;
        private bool _needsDamSync = false;
        
        public bool HasMod { get => StatText.HasMod; set => StatText.HasMod = value; }
        public readonly StatParser StatText;
        public readonly SNet_Player Owner;

        public StatData(SNet_Player player, bool hasMod)
        {
            _accData = new long[(int)AccSlotType.Count, (int)AccShotType.Count, (int)AccStatType.Count];
            _deltaAccData = new short[(int)AccSlotType.Count, (int)AccShotType.Count, (int)AccStatType.Count];
            _damData = new double[(int)DamSlotType.Count, (int)DamStatType.Count];
            _deltaDamData = new float[(int)DamSlotType.Count, (int)DamStatType.Count];

            StatText = new StatParser(this, player, hasMod);
            Owner = player;
        }

        public long GetAccuracy(AccSlotType slot, AccShotType shot, AccStatType stat)
        {
            long value;
            if (slot == AccSlotType.All)
                value = _accData[(int)AccSlotType.Primary, (int)shot, (int)stat] + _accData[(int)AccSlotType.Secondary, (int)shot, (int)stat];
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

            for (int slot = 0; slot < _damData.GetLength(0); slot++)
            {
                for (int stat = 0; stat < _damData.GetLength(1); stat++)
                {
                    _damData[slot, stat] = 0;
                    _deltaDamData[slot, stat] = 0;
                }
            }

            _needsAccSync = false;
            _needsDamSync = false;
            StatText.Update(true);
        }

        public void AddAccuracy(AccSlotType slot, short[,] deltaData)
        {
            _needsAccSync = true;
            for (int shot = 0; shot < deltaData.GetLength(0); shot++)
            {
                for (int stat = 0; stat < deltaData.GetLength(1); stat++)
                {
                    short val = deltaData[shot, stat];
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

        public bool PopRemoteAccDelta([MaybeNullWhen(false)] out short[,,] deltaAccuracy)
        {
            if (!_needsAccSync)
            {
                deltaAccuracy = null;
                return false;
            }

            deltaAccuracy = (short[,,])_deltaAccData.Clone();
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

        public void AddRemoteAccDelta(short[,,] deltaData)
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

        public static Retriever CreateAccuracyRetriever(StatData data, AccSlotType[] slotTypes, AccShotType shotType, AccStatType statType) => new AccuracyRetriever(data, slotTypes, shotType, statType);
        public static Retriever CreateDamageRetriever(StatData data, DamSlotType[] slotTypes, DamStatType statType) => new DamageRetriever(data, slotTypes, statType);

        class AccuracyRetriever : Retriever
        {
            private readonly int[] _slotTypes;
            private readonly int _shotType;
            private readonly int _statType;

            public AccuracyRetriever(StatData data, AccSlotType[] slotTypes, AccShotType shotType, AccStatType statType) : base(data)
            {
                if (slotTypes.Length > 0)
                {
                    if (slotTypes[0] == AccSlotType.All)
                        _slotTypes = new int[2] { (int)AccSlotType.Primary, (int)AccSlotType.Secondary };
                    else
                        _slotTypes = Array.ConvertAll(slotTypes, type => (int)type);
                }
                else
                    _slotTypes = Array.Empty<int>();
                _shotType = (int)shotType;
                _statType = (int)statType;
            }

            public override long Value
            {
                get
                {
                    long value = 0;
                    foreach (var slot in _slotTypes)
                        value += _data._accData[slot, _shotType, _statType];
                    return value;
                }
            }
        }

        class DamageRetriever : Retriever
        {
            private readonly int[] _slotTypes;
            private readonly int _statType;

            public DamageRetriever(StatData data, DamSlotType[] slotTypes, DamStatType statType) : base(data)
            {
                if (slotTypes.Length > 0)
                {
                    if (slotTypes[0] == DamSlotType.All)
                    {
                        _slotTypes = new int[(int)DamSlotType.Count];
                        for (int i = 0; i < (int)DamSlotType.Count; i++)
                            _slotTypes[i] = i;
                    }
                    else
                        _slotTypes = Array.ConvertAll(slotTypes, type => (int)type);
                }
                else
                    _slotTypes = Array.Empty<int>();
                _statType = (int)statType;
            }

            public override long Value
            {
                get
                {
                    double value = 0;
                    foreach (var slot in _slotTypes)
                        value += _data._damData[slot, _statType];
                    return (long)value;
                }
            }
        }
    }
}
