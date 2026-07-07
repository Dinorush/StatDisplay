using BepInEx.Unity.IL2CPP;

namespace StatDisplay.Dependencies
{
    internal static class ArchiveWrapper
    {
        public const string PLUGIN_GUID = "dev.AuriRex.gtfo.TheArchive";
        public static readonly bool HasArchive;

        static ArchiveWrapper()
        {
            HasArchive = IL2CPPChainloader.Instance.Plugins.ContainsKey(PLUGIN_GUID);
        }
    }
}
