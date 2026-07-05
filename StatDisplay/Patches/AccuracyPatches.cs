using StatDisplay.Data;
using StatDisplay.Dependencies;
using StatDisplay.Handler;
using StatDisplay.Utils.Extensions;
using Gear;
using HarmonyLib;
using SNetwork;
using System;
using UnityEngine;

namespace StatDisplay.Patches
{
    [HarmonyPatch]
    internal static class AccuracyPatches
    {
        struct CacheInfo
        {
            public AccSlotType slot;
            public ulong lookup;
            public readonly bool[,] history;
            public readonly short[,] delta;

            public readonly bool Valid => slot != AccSlotType.All;

            public CacheInfo()
            {
                slot = AccSlotType.All;
                lookup = 0;
                // Exclude FullShot and Fired (unused for history)
                history = new bool[(int)AccShotType.Count - 1, (int)AccStatType.Count - 1];
                delta = new short[(int)AccShotType.Count, (int)AccStatType.Count];
            }
        }

        private static CacheInfo _cache = new();

        private static void CacheInc(AccShotType shotType, AccStatType statType, bool checkHistory = false)
        {
            if (checkHistory)
            {
                if (_cache.history[(int)shotType, (int)statType])
                    return;
                _cache.history[(int)shotType, (int)statType] = true;
            }
            _cache.delta[(int)shotType, (int)statType]++;
        }

        private static void ResetHistory(AccShotType shotType)
        {
            for (int stat = 0; stat < (int)AccStatType.Count - 1; stat++)
                _cache.history[(int)shotType, stat] = false;
        }

        [HarmonyPatch(typeof(ShotgunSynced), nameof(Shotgun.Fire))]
        [HarmonyPatch(typeof(BulletWeaponSynced), nameof(BulletWeaponSynced.Fire))]
        [HarmonyPatch(typeof(Shotgun), nameof(Shotgun.Fire))]
        [HarmonyPatch(typeof(BulletWeapon), nameof(BulletWeapon.Fire))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void Pre_Fire(BulletWeapon __instance)
        {
            if (__instance.Owner == null) return;
            var owner = __instance.Owner.Owner;
            if (!owner.IsLocal && (!SNet.IsMaster || !owner.IsBot)) return;
            if (EWCWrapper.HasCWC(__instance)) return;

            _cache.slot = __instance.AmmoType.ToAccSlotType();
            _cache.lookup = owner.Lookup;

            CacheInc(AccShotType.Group, AccStatType.Fired);
        }

        [HarmonyPatch(typeof(ShotgunSynced), nameof(Shotgun.Fire))]
        [HarmonyPatch(typeof(BulletWeaponSynced), nameof(BulletWeaponSynced.Fire))]
        [HarmonyPatch(typeof(Shotgun), nameof(Shotgun.Fire))]
        [HarmonyPatch(typeof(BulletWeapon), nameof(BulletWeapon.Fire))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_Fire()
        {
            if (!_cache.Valid) return;

            StatHandler.AddAccuracyDelta(_cache.lookup, _cache.slot, _cache.delta);
            _cache.slot = AccSlotType.All;
            _cache.lookup = 0;
            var delta = _cache.delta;
            for (int shot = 0; shot < delta.GetLength(0); shot++)
                for (int stat = 0; stat < delta.GetLength(1); stat++)
                    delta[shot, stat] = 0;
            
            var history = _cache.history;
            for (int shot = 0; shot < history.GetLength(0); shot++)
                for (int stat = 0; stat < history.GetLength(1); stat++)
                    history[shot, stat] = false;

        }

        [HarmonyPatch(typeof(Weapon), nameof(Weapon.CastWeaponRay),
            new Type[] {
                typeof(Transform),
                typeof(Weapon.WeaponHitData),
                typeof(Vector3),
                typeof(int)
            }, new ArgumentType[] {
                ArgumentType.Normal,
                ArgumentType.Ref,
                ArgumentType.Normal,
                ArgumentType.Normal
            })]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_CastWeaponRay(Weapon.WeaponHitData weaponRayData)
        {
            // Pierce hits run with reduced ray distance
            if (!_cache.Valid || weaponRayData.maxRayDist < 100f) return;

            ResetHistory(AccShotType.Shot);
            CacheInc(AccShotType.Shot, AccStatType.Fired);
            CacheInc(AccShotType.Full, AccStatType.Fired);
        }

        [HarmonyPatch(typeof(Dam_EnemyDamageLimb), nameof(Dam_EnemyDamageLimb.BulletDamage))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_EnemyDamage(Dam_EnemyDamageLimb __instance)
        {
            if (!_cache.Valid) return;

            CacheInc(AccShotType.Shot, AccStatType.Hit, true);
            CacheInc(AccShotType.Group, AccStatType.Hit, true);
            CacheInc(AccShotType.Full, AccStatType.Hit);
            if (__instance.m_type == eLimbDamageType.Weakspot)
            {
                CacheInc(AccShotType.Shot, AccStatType.Crit, true);
                CacheInc(AccShotType.Group, AccStatType.Crit, true);
                CacheInc(AccShotType.Full, AccStatType.Crit);
            }
        }
    }
}
