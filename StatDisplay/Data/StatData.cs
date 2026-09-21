using Gear;
using Player;
using SNetwork;
using StatDisplay.Dependencies;
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
            public abstract double Value { get; }
            public virtual void AlignWith(Retriever? retriever) { }
        }

        struct SlotWeights
        {
            public float Pellet;
            public float Ammo;

            public SlotWeights()
            {
                Pellet = 1f;
                Ammo = 1f;
            }
        }

        private readonly long[,,] _accData;
        private readonly short[,,] _deltaAccData;
        private readonly double[,] _damData;
        private readonly float[,] _deltaDamData;
        private bool _needsAccSync = false;
        private bool _needsDamSync = false;

        private SlotWeights _primaryWeights = new();
        private SlotWeights _secondaryWeights = new();

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
            InitAccuracyWeights();
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

        internal void SetAccuracyWeights(Item item, AccSlotType slot)
        {
            if (EWCWrapper.TrySetAccuracyWeight(this, item, slot))
                return;

            var weapon = item.TryCast<BulletWeapon>();
            if (weapon == null) return;

            var archData = weapon.ArchetypeData;
            if (archData == null) return;

            bool isShotgun = Owner.IsLocal ? weapon.TryCast<Shotgun>() : weapon.TryCast<ShotgunSynced>();
            float pellet = isShotgun ? 1f / archData.ShotgunBulletCount : 1f;
            float ammo = archData.CostOfBullet;
            SetAccuracyWeights(slot, pellet, ammo);
        }

        internal void SetAccuracyWeights(AccSlotType slot, float pelletWeight, float ammoWeight)
        {
            if (slot == AccSlotType.Primary)
            {
                _primaryWeights = new()
                {
                    Pellet = pelletWeight,
                    Ammo = ammoWeight
                };
            }
            else if(slot == AccSlotType.Secondary)
            {
                _secondaryWeights = new()
                {
                    Pellet = pelletWeight,
                    Ammo = ammoWeight
                };
            }

            StatText.Update();
        }

        private void InitAccuracyWeights()
        {
            if (!PlayerBackpackManager.TryGetBackpack(Owner, out var bp)) return;

            if (bp.TryGetBackpackItem(InventorySlot.GearStandard, out var bpItem))
                SetAccuracyWeights(bpItem.Instance, AccSlotType.Primary);
            if (bp.TryGetBackpackItem(InventorySlot.GearSpecial, out bpItem))
                SetAccuracyWeights(bpItem.Instance, AccSlotType.Secondary);
        }

        public static Retriever CreateAccuracyRetriever(StatData data, AccSlotType[] slotTypes, AccShotType shotType, AccStatType statType, AccWeightType weightType) => new AccuracyRetriever(data, slotTypes, shotType, statType, weightType);
        public static Retriever CreateDamageRetriever(StatData data, DamSlotType[] slotTypes, DamStatType statType) => new DamageRetriever(data, slotTypes, statType);

        class AccuracyRetriever : Retriever
        {
            private readonly int[] _slotTypes;
            private readonly int _shotType;
            private readonly int _statType;
            private AccWeightType _weightType;

            public AccuracyRetriever(StatData data, AccSlotType[] slotTypes, AccShotType shotType, AccStatType statType, AccWeightType weightType) : base(data)
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
                _weightType = weightType;
            }

            public override void AlignWith(Retriever? retriever)
            {
                if (retriever == null || retriever is not AccuracyRetriever accRetriever) return;

                if (_weightType != AccWeightType.None)
                    accRetriever._weightType = _weightType;
                else
                    _weightType = accRetriever._weightType;
            }

            public override double Value
            {
                get
                {
                    if (_weightType != AccWeightType.None && _slotTypes.Length > 1)
                    {
                        double primary = _data._accData[(int)AccSlotType.Primary, _shotType, _statType];
                        double secondary = _data._accData[(int)AccSlotType.Secondary, _shotType, _statType];

                        if (_weightType.HasFlag(AccWeightType.Pellet))
                        {
                            primary *= _data._primaryWeights.Pellet;
                            secondary *= _data._secondaryWeights.Pellet;
                        }

                        if (_weightType.HasFlag(AccWeightType.Ammo))
                        {
                            float primaryWeight = _data._primaryWeights.Ammo;
                            float secondaryWeight = _data._secondaryWeights.Ammo;
                            float totalWeight = primaryWeight + secondaryWeight;
                            if (totalWeight > 0.0001f)
                            {
                                primaryWeight *= 2f / totalWeight;
                                secondaryWeight *= 2f / totalWeight;
                                primary *= primaryWeight;
                                secondary *= secondaryWeight;
                            }
                        }

                        return primary + secondary;
                    }
                    else if (_weightType.HasFlag(AccWeightType.Pellet) && _slotTypes.Length == 1)
                    {
                        var slot = _slotTypes[0];
                        double decValue = _data._accData[slot, _shotType, _statType];
                        if (slot == 0)
                            decValue *= _data._primaryWeights.Pellet;
                        else
                            decValue *= _data._secondaryWeights.Pellet;
                        return decValue;
                    }
                    else
                    {
                        double value = 0;
                        foreach (var slot in _slotTypes)
                            value += _data._accData[slot, _shotType, _statType];
                        return value;
                    }
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

            public override double Value
            {
                get
                {
                    double value = 0;
                    foreach (var slot in _slotTypes)
                        value += _data._damData[slot, _statType];
                    return value;
                }
            }
        }
    }
}
