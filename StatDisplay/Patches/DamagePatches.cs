using Agents;
using Gear;
using HarmonyLib;
using Player;
using SNetwork;
using StatDisplay.Attributes;
using StatDisplay.Data;
using StatDisplay.Dependencies;
using StatDisplay.Handler;
using StatDisplay.Utils.Extensions;
using System;

namespace StatDisplay.Patches
{
    [HarmonyPatch]
    internal static class DamagePatches
    {
        [InvokeOnLoad]
        private static void Init()
        {
            EWCWrapper.AddProjectileHitCallback((lookup, slot) =>
            {
                _cache.slot = slot;
                _cache.lookup = lookup;
            });
        }

        struct CacheInfo
        {
            public DamSlotType slot;
            public ulong lookup;

            public readonly bool Valid => slot != DamSlotType.All && lookup != 0;

            public CacheInfo()
            {
                slot = DamSlotType.All;
                lookup = 0;
            }
        }

        private static CacheInfo _cache = new();
        private static readonly Random _random = new();

        private static void ClearCache()
        {
            _cache.slot = DamSlotType.All;
            _cache.lookup = 0;
        }

        [HarmonyPatch(typeof(ShotgunSynced), nameof(Shotgun.Fire))]
        [HarmonyPatch(typeof(BulletWeaponSynced), nameof(BulletWeaponSynced.Fire))]
        [HarmonyPatch(typeof(Shotgun), nameof(Shotgun.Fire))]
        [HarmonyPatch(typeof(BulletWeapon), nameof(BulletWeapon.Fire))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void Pre_Fire(BulletWeapon __instance)
        {
            _cache.slot = __instance.AmmoType.ToDamSlotType();
            _cache.lookup = __instance.Owner.Owner.Lookup;
        }

        [HarmonyPatch(typeof(ShotgunSynced), nameof(Shotgun.Fire))]
        [HarmonyPatch(typeof(BulletWeaponSynced), nameof(BulletWeaponSynced.Fire))]
        [HarmonyPatch(typeof(Shotgun), nameof(Shotgun.Fire))]
        [HarmonyPatch(typeof(BulletWeapon), nameof(BulletWeapon.Fire))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_Fire()
        {
            ClearCache();
        }

        [HarmonyPatch(typeof(MeleeWeaponFirstPerson), nameof(MeleeWeaponFirstPerson.DoAttackDamage))]
        [HarmonyPatch(typeof(MeleeWeaponThirdPerson), nameof(MeleeWeaponThirdPerson.DoAttackDamage))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void Pre_Attack(ItemEquippable __instance)
        {
            _cache.slot = DamSlotType.Melee;
            _cache.lookup = __instance.Owner.Owner.Lookup;
        }

        [HarmonyPatch(typeof(MeleeWeaponFirstPerson), nameof(MeleeWeaponFirstPerson.DoAttackDamage))]
        [HarmonyPatch(typeof(MeleeWeaponThirdPerson), nameof(MeleeWeaponThirdPerson.DoAttackDamage))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_Attack()
        {
            ClearCache();
        }

        [HarmonyPatch(typeof(SentryGunInstance_Firing_Bullets), nameof(SentryGunInstance_Firing_Bullets.UpdateFireMaster))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void Pre_SentryFire(SentryGunInstance_Firing_Bullets __instance)
        {
            CacheTool(__instance.m_core.Owner);
        }

        [HarmonyPatch(typeof(SentryGunInstance_Firing_Bullets), nameof(SentryGunInstance_Firing_Bullets.UpdateFireMaster))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_SentryFire()
        {
            ClearCache();
        }

        [HarmonyPatch(typeof(MineDeployerInstance_Detonate_Explosive), nameof(MineDeployerInstance_Detonate_Explosive.DoExplode))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        public static void ExplodePrefix(MineDeployerInstance_Detonate_Explosive __instance)
        {
            CacheTool(__instance.m_core.Owner);
        }

        [HarmonyPatch(typeof(MineDeployerInstance_Detonate_Explosive), nameof(MineDeployerInstance_Detonate_Explosive.DoExplode))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        public static void ExplodePostfix()
        {
            ClearCache();
        }

        private static void CacheTool(PlayerAgent? owner)
        {
            _cache.slot = DamSlotType.Tool;
            _cache.lookup = owner?.Owner.Lookup ?? 0;
        }

        [HarmonyPatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.BulletDamage))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void Pre_BulletDamage(Dam_EnemyDamageBase __instance, int limbID, float dam, ref uint gearCategoryId)
        {
            if (!_cache.Valid || limbID == -1) return;

            gearCategoryId = (uint)_cache.slot + 1;
            if (!StatManager.MasterHasMod)
            {
                DamStatType stat = __instance.DamageLimbs[limbID].m_type == eLimbDamageType.Weakspot ? DamStatType.Crit : DamStatType.Any;
                StatHandler.AddDamage(_cache.lookup, _cache.slot, stat, Math.Min(dam, __instance.Health));
            }
        }

        [HarmonyPatch(typeof(Dam_EnemyDamageLimb), nameof(Dam_EnemyDamageLimb.MeleeDamage))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void Pre_MeleeDamage(Dam_EnemyDamageLimb __instance, Agent sourceAgent, float dam)
        {
            if (!StatManager.MasterHasMod)
            {
                DamStatType stat = __instance.m_type == eLimbDamageType.Weakspot ? DamStatType.Crit : DamStatType.Any;
                StatHandler.AddDamage(sourceAgent.Cast<PlayerAgent>().Owner.Lookup, DamSlotType.Melee, stat, Math.Min(dam, __instance.m_base.Health));
            }
        }

        [HarmonyPatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ReceiveBulletDamage))]
        [HarmonyPatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ReceiveMeleeDamage))]
        [HarmonyPatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ReceiveExplosionDamage))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void Pre_RecvEnemyDamage(Dam_EnemyDamageBase __instance, ref float __state)
        {
            __state = __instance.Health;
        }

        [HarmonyPatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ReceiveBulletDamage))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_RecvBulletDamage(Dam_EnemyDamageBase __instance, pBulletDamageData data, float __state)
        {
            // Sentry gun without an owner (level spawned) should be ignored.
            if (CacheDamageHost(__instance, data.limbID, __state) || _cache.slot == DamSlotType.Tool) return;

            DamSlotType slot;
            if (data.gearCategoryId > 0)
            {
                slot = (DamSlotType)(data.gearCategoryId - 1);
                if (slot >= DamSlotType.Count) return;
            }
            else // Unmodded client
            {
                if (!data.source.TryGet(out var agent)) return;
                var player = agent.Cast<PlayerAgent>();
                var inventory = player.Inventory.Cast<PlayerInventorySynced>();
                if (inventory.m_equipStatus == PlayerInventorySynced.EquipStatus.Unequipping)
                {
                    slot = inventory.m_queuedEquipItem.AmmoType switch
                    {
                        AmmoType.Standard => DamSlotType.Main,
                        AmmoType.Special => DamSlotType.Special,
                        _ => inventory.WieldedSlot.ToDamSlotType(),
                    };
                }
                else
                    slot = inventory.WieldedSlot.ToDamSlotType();

                // Indeterminate; received bullet damage but source gun is unobtainable. 
                if (slot != DamSlotType.Main && slot != DamSlotType.Special)
                    slot = _random.NextSingle() < 0.5f ? DamSlotType.Main : DamSlotType.Special;
            }

            CacheDamage(__instance, data.limbID, __state, data.source, slot);
        }

        [HarmonyPatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ReceiveMeleeDamage))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_RecvMeleeDamage(Dam_EnemyDamageBase __instance, pFullDamageData data, float __state)
        {
            if (CacheDamageHost(__instance, data.limbID, __state)) return;
            CacheDamage(__instance, data.limbID, __state, data.source, DamSlotType.Melee);
        }

        [HarmonyPatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ReceiveExplosionDamage))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_RecvExplosionDamage(Dam_EnemyDamageBase __instance, pExplosionDamageData data, float __state)
        {
            CacheDamageHost(__instance, data.limbID, __state);
        }

        private static bool CacheDamageHost(Dam_EnemyDamageBase damBase, byte limbID, float prevHealth)
        {
            if (!_cache.Valid) return false;

            DamStatType stat = damBase.DamageLimbs[limbID].m_type == eLimbDamageType.Weakspot ? DamStatType.Crit : DamStatType.Any;
            StatHandler.AddDamage(_cache.lookup, _cache.slot, stat, prevHealth - damBase.Health);
            return true;
        }
        private static void CacheDamage(Dam_EnemyDamageBase damBase, byte limbID, float prevHealth, pAgent agent, DamSlotType slot)
        {
            if (!agent.TryGet(out var player)) return;

            var lookup = player.Cast<PlayerAgent>().Owner.Lookup;
            DamStatType stat = damBase.DamageLimbs[limbID].m_type == eLimbDamageType.Weakspot ? DamStatType.Crit : DamStatType.Any;
            StatHandler.AddDamage(lookup, slot, stat, prevHealth - damBase.Health);
        }
    }
}
