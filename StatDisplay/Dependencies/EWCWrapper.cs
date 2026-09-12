using BepInEx.Unity.IL2CPP;
using Enemies;
using EWC.API;
using EWC.API.Accuracy;
using EWC.CustomWeapon;
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
            FireShotAPI.PreShotFired += (data, _, _) =>
            {
                if (data.owner == null) return;

                fireCallback(data.owner.Owner.Lookup, data.shotInfo.CWC!.Weapon.Component.AmmoType.ToDamSlotType());
            };
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void AddPostShotCallback_Unsafe(Action fireCallback)
        {
            FireShotAPI.OnShotFired += (_, _, _, weaponType) =>
            {
                if (!weaponType.HasFlag(EWC.CustomWeapon.Enums.WeaponType.BulletWeapon)) return;

                fireCallback();
            };
        }
    }
}
