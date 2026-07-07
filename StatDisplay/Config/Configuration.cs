using StatDisplay.Attributes;
using StatDisplay.Config.Archive;
using StatDisplay.Config.Vanilla;
using StatDisplay.Dependencies;
using System.Runtime.CompilerServices;

namespace StatDisplay.Config
{
    public static class Configuration
    {
        public static readonly IConfiguration Config;

        [InvokeOnLoad]
        private static void Init() { }

        static Configuration()
        {
            if (ArchiveWrapper.HasArchive)
                Config = LoadArchive();
            else
                Config = new VanillaConfiguration();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IConfiguration LoadArchive()
        {
            StatDisplayModule.Load();
            return StatDisplayFeature.Instance;
        }
    }
}
