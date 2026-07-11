using BepInEx.Unity.IL2CPP;

namespace StatDisplay.Dependencies
{
    internal static class ArchiveWrapper
    {
        public const string PLUGIN_GUID = "dev.AuriRex.gtfo.TheArchive";
        public static readonly bool HasArchive;

        static ArchiveWrapper()
        {
            if (IL2CPPChainloader.Instance.Plugins.TryGetValue(PLUGIN_GUID, out var info))
            {
                HasArchive = info.Metadata.Version.Major > 0;
                if (!HasArchive)
                    DinoLogger.Warning($"{EntryPoint.MODNAME} is only compatible with Auri's Archive release. Using Config file.");
            }
            else
                HasArchive = false;
        }
    }
}
