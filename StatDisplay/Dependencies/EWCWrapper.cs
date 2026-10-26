using BepInEx.Unity.IL2CPP;
using Enemies;
using EWC.API;
using EWC.API.Accuracy;
using EWC.CustomWeapon;
using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.Properties.Traits;
using EWC.CustomWeapon.Structs;
using Gear;
using Player;
using StatDisplay.Data;
using StatDisplay.Handler;
using StatDisplay.Networking;
using StatDisplay.Utils.Extensions;
using System;
using System.Runtime.CompilerServices;

namespace StatDisplay.Dependencies
{
    internal static class EWCWrapper
    {
        public const string PLUGIN_GUID = "Dinorush.ExtraWeaponCustomization";
        public static readonly bool HasEWC;

        static EWCWrapper()
        {
            HasEWC = IL2CPPChainloader.Instance.Plugins.ContainsKey(PLUGIN_GUID);
            if (HasEWC)
                AddCallbacks_Unsafe();
        }

        public static bool HasCWC(BulletWeapon weapon)
        {
            return HasEWC && HasCWC_Unsafe(weapon);
        }

        public static void AddProjectileHitCallback(Action<ulong, DamSlotType> hitCallback)
        {
            if (HasEWC)
                AddHitCallback_Unsafe(hitCallback);
        }

        public static void AddProjectilePostHitCallback(Action hitCallback)
        {
            if (HasEWC)
                AddPostHitCallback_Unsafe(hitCallback);
        }

        public static void AddPreShotFiredCallback(Action<ulong, DamSlotType> fireCallback)
        {
            if (HasEWC)
                AddPreShotCallback_Unsafe(fireCallback);
        }

        public static void AddPostShotFiredCallback(Action fireCallback)
        {
            if (HasEWC)
                AddPostShotCallback_Unsafe(fireCallback);
        }

        public static bool TrySetAccuracyWeight(StatData data, Item item, AccSlotType slot)
        {
            return HasEWC && TrySetAccuracyWeight_Unsafe(data, item, slot);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool HasCWC_Unsafe(BulletWeapon weapon) => weapon.GetComponent<CustomWeaponComponent>() != null;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void AddCallbacks_Unsafe()
        {
            AccuracyAPI.OnAccuracyUpdate += (accuracy, delta) =>
            {
                if (accuracy.Slot == InventorySlot.GearClass) return;

                short[,] deltaArr = new short[(int)AccShotType.Count, (int)AccStatType.Count];
                void Copy(AccShotType shotType, StatDelta stat)
                {
                    deltaArr[(int)shotType, (int)AccStatType.Hit] = (short)stat.Hits;
                    deltaArr[(int)shotType, (int)AccStatType.Crit] = (short)stat.Crits;
                    deltaArr[(int)shotType, (int)AccStatType.Fired] = (short)stat.Count;
                }
                Copy(AccShotType.Shot, delta.Shots);
                Copy(AccShotType.Group, delta.Groups);
                Copy(AccShotType.Full, delta.FullShots);
                StatHandler.AddAccuracyDelta(accuracy.ParentStats.Owner.Owner.Lookup, accuracy.Slot.ToAccSlotType(), deltaArr);
            };

            DamageAPI.PreLocalDOTDamage += RecordDamageLocal;
            DamageAPI.PreLocalExplosiveDamage += RecordDamageLocal;
            DamageAPI.PreLocalShrapnelDamage += RecordDamageLocal;
            DamageAPI.PreDOTDamage += RecordPreDamageHost;
            DamageAPI.PreExplosiveDamage += RecordPreDamageHost;
            DamageAPI.PreShrapnelDamage += RecordPreDamageHost;
            DamageAPI.PostDOTDamage += RecordPostDamageHost;
            DamageAPI.PostExplosiveDamage += RecordPostDamageHost;
            DamageAPI.PostShrapnelDamage += RecordPostDamageHost;

            GearAPI.OnGearSpawned += (item, cwc) =>
            {
                if (!StatHandler.TryGetData(item.Owner.Owner, out var data)) return;

                var slot = item.AmmoType.ToAccSlotType();
                if (cwc == null)
                    data.SetAccuracyWeights(item, slot);
                else
                    SetAccuracyWeight_Unsafe(data, item, cwc, slot);
            };
        }

        private static void RecordDamageLocal(float damage, EnemyAgent enemy, Dam_EnemyDamageLimb limb, PlayerAgent? source, pCWC cwc)
        {
            if (NetworkManager.MasterHasMod) return;

            DamStatType stat = limb.m_type == eLimbDamageType.Weakspot ? DamStatType.Crit : DamStatType.Any;
            StatHandler.AddDamage(source!.Owner.Lookup, cwc.slot.ToDamSlotType(), stat, Math.Min(damage, limb.m_base.Health));
        }

        private static float _healthState = 0;
        private static void RecordPreDamageHost(float damage, EnemyAgent enemy, Dam_EnemyDamageLimb limb, PlayerAgent? source, pCWC cwc) => _healthState = limb.m_base.Health;
        private static void RecordPostDamageHost(float damage, EnemyAgent enemy, Dam_EnemyDamageLimb limb, PlayerAgent? source, pCWC cwc)
        {
            DamStatType stat = limb.m_type == eLimbDamageType.Weakspot ? DamStatType.Crit : DamStatType.Any;
            StatHandler.AddDamage(source!.Owner.Lookup, cwc.slot.ToDamSlotType(), stat, _healthState - limb.m_base.Health);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void AddHitCallback_Unsafe(Action<ulong, DamSlotType> hitCallback)
        {
            ProjectileAPI.OnProjectileHit += (comp, damageable) =>
            {
                var cwc = comp.Settings.CWC;
                if (cwc.Owner.Player == null) return;

                hitCallback(cwc.Owner.Player.Owner.Lookup, cwc.Weapon.AmmoType.ToDamSlotType());
            };
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void AddPostHitCallback_Unsafe(Action hitCallback)
        {
            ProjectileAPI.OnProjectilePostHit += (_, _) => hitCallback();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void AddPreShotCallback_Unsafe(Action<ulong, DamSlotType> fireCallback)
        {
            FireShotAPI.PreShotFired += (data, _, cwc) =>
            {
                if (data.owner == null || !cwc.Weapon.IsType(WeaponType.BulletWeapon) || !cwc.Owner.IsType(OwnerType.Managed)) return;

                fireCallback(data.owner.Owner.Lookup, data.shotInfo.CWC!.Weapon.Component.AmmoType.ToDamSlotType());
            };
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void AddPostShotCallback_Unsafe(Action fireCallback)
        {
            FireShotAPI.OnShotFired += (hitData, _, _, cwc) =>
            {
                if (!cwc.Weapon.IsType(WeaponType.BulletWeapon) || !cwc.Owner.IsType(OwnerType.Managed)) return;

                fireCallback();
            };
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool TrySetAccuracyWeight_Unsafe(StatData data, Item item, AccSlotType slot)
        {
            var cwc = item.GetComponent<CustomWeaponComponent>();
            if (cwc == null) return false;

            SetAccuracyWeight_Unsafe(data, item, cwc, slot);
            return true;
        }

        private static void SetAccuracyWeight_Unsafe(StatData data, Item item, CustomWeaponComponent cwc, AccSlotType slot)
        {
            if (!cwc.Owner.IsType(OwnerType.Player) || !cwc.Weapon.IsType(WeaponType.BulletWeapon)) return;

            var arch = cwc.Weapon.ArchetypeData;
            int pelletCount = cwc.Weapon.IsShotgun ? arch.ShotgunBulletCount : 1;
            if (cwc.TryGetTrait<MultiShot>(out var multiShot))
            {
                var shots = multiShot.Offsets.Length / 2 * ((int)multiShot.Repeat + 1);
                if (!multiShot.ForceSingleBullet)
                    shots *= pelletCount;

                if (multiShot.CancelShot)
                    pelletCount = shots;
                else
                    pelletCount += shots;
            }

            float ammo = arch.CostOfBullet;
            if (cwc.TryGetTrait<AmmoCap>(out var ammoCap))
                ammo /= ammoCap.AmmoCapRel;

            data.SetAccuracyWeights(slot, 1f / pelletCount, ammo);
        }
    }
}
