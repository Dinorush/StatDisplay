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
    [BepInPlugin("Dinorush." + MODNAME, MODNAME, "1.0.1")]
    [BepInDependency("dev.gtfomodding.gtfo-api", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(EWCWrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    internal sealed class EntryPoint : BasePlugin
    {
        public const string MODNAME = "StatDisplay";

        private IEnumerable<MethodInfo> _cleanupCallbacks = null!;
        private IEnumerable<MethodInfo> _enterCallbacks = null!;
        private IEnumerable<MethodInfo> _buildDoneCallbacks = null!;

        public override void Load()
        {
            new Harmony(MODNAME).PatchAll();
            CacheFrequentCallbacks();
            InvokeCallbacks<InvokeOnLoadAttribute>();
            LevelAPI.OnLevelCleanup += RunFrequentCallback(_cleanupCallbacks);
            LevelAPI.OnEnterLevel += RunFrequentCallback(_enterCallbacks);
            LevelAPI.OnBuildDone += RunFrequentCallback(_buildDoneCallbacks);
            AssetAPI.OnStartupAssetsLoaded += InvokeCallbacks<InvokeOnAssetLoadAttribute>;
            Log.LogMessage("Loaded " + MODNAME);
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
            Type[] typesFromAssembly = AccessTools.GetTypesFromAssembly(GetType().Assembly);
            var methods = typesFromAssembly.SelectMany(AccessTools.GetDeclaredMethods).Where(method => method.IsStatic);

            _cleanupCallbacks = from method in methods
                                where method.GetCustomAttribute<InvokeOnCleanupAttribute>() != null
                                select method;

            _enterCallbacks = from method in methods
                              where method.GetCustomAttribute<InvokeOnEnterAttribute>() != null
                              select method;

            _buildDoneCallbacks = from method in methods
                                  where method.GetCustomAttribute<InvokeOnBuildDoneAttribute>() != null
                                  select method;
        }

        private void InvokeCallbacks<T>() where T : Attribute
        {
            Type[] typesFromAssembly = AccessTools.GetTypesFromAssembly(GetType().Assembly);
            IEnumerable<MethodInfo> enumerable = from method in typesFromAssembly.SelectMany(AccessTools.GetDeclaredMethods)
                                                 where method.GetCustomAttribute<T>() != null
                                                 where method.IsStatic
                                                 select method;
            foreach (MethodInfo item in enumerable)
                item.Invoke(null, null);
        }
    }
}