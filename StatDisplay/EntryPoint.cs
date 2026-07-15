using StatDisplay.Attributes;
using StatDisplay.Dependencies;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using GTFO.API;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace StatDisplay
{
    [BepInPlugin("Dinorush." + MODNAME, MODNAME, "1.1.3")]
    [BepInDependency("dev.gtfomodding.gtfo-api", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(EWCWrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(ArchiveWrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    internal sealed class EntryPoint : BasePlugin
    {
        public const string MODNAME = "StatDisplay";

        private IEnumerable<MethodInfo> _cleanupCallbacks = null!;
        private IEnumerable<MethodInfo> _enterCallbacks = null!;
        private IEnumerable<MethodInfo> _buildStartCallbacks = null!;
        private Type[] _allTypes = null!;

        public override void Load()
        {
            _allTypes = GetTypesSafe();
            var harmony = new Harmony(MODNAME);
            // Per-type patching, avoid Harmony.PatchAll() warnings if Archive doesn't exist
            foreach (var type in _allTypes)
                if (type.GetCustomAttribute<HarmonyPatch>() != null)
                    harmony.PatchAll(type);

            CacheFrequentCallbacks();
            InvokeCallbacks<InvokeOnLoadAttribute>();
            LevelAPI.OnLevelCleanup += RunFrequentCallback(_cleanupCallbacks);
            LevelAPI.OnEnterLevel += RunFrequentCallback(_enterCallbacks);
            LevelAPI.OnBuildStart += RunFrequentCallback(_buildStartCallbacks);
            AssetAPI.OnStartupAssetsLoaded += InvokeCallbacks<InvokeOnAssetLoadAttribute>;
            Log.LogMessage("Loaded " + MODNAME);
        }

        private Type[] GetTypesSafe()
        {
            IEnumerable<Type> allTypes;
            try
            {
                allTypes = GetType().Assembly.DefinedTypes;
            }
            catch (ReflectionTypeLoadException ex)
            {
                allTypes = ex.Types.Where(type => type != null)!;
            }

            if (!ArchiveWrapper.HasArchive)
                return allTypes.Where(type => !type.FullName!.Contains("Archive")).ToArray();
            else
                return allTypes.ToArray();
        }

        private static Action RunFrequentCallback(IEnumerable<MethodInfo> callbacks)
        {
            return () =>
            {
                foreach (var callback in callbacks)
                    callback.Invoke(null, null);
            };
        }

        private void CacheFrequentCallbacks()
        {
            var methods = _allTypes.SelectMany(AccessTools.GetDeclaredMethods).Where(method => method.IsStatic);

            _cleanupCallbacks = from method in methods
                                where method.GetCustomAttribute<InvokeOnCleanupAttribute>() != null
                                select method;

            _enterCallbacks = from method in methods
                              where method.GetCustomAttribute<InvokeOnEnterAttribute>() != null
                              select method;

            _buildStartCallbacks = from method in methods
                                  where method.GetCustomAttribute<InvokeOnBuildStartAttribute>() != null
                                  select method;
        }

        private void InvokeCallbacks<T>() where T : Attribute
        {
            IEnumerable<MethodInfo> enumerable = from method in _allTypes.SelectMany(AccessTools.GetDeclaredMethods)
                                                 where method.GetCustomAttribute<T>() != null
                                                 where method.IsStatic
                                                 select method;
            foreach (MethodInfo item in enumerable)
                item.Invoke(null, null);
        }
    }
}