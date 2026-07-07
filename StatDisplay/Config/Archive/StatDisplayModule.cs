using System.Runtime.CompilerServices;
using TheArchive;
using TheArchive.Core;
using TheArchive.Core.Localization;
using TheArchive.Interfaces;

namespace StatDisplay.Config.Archive
{
    internal sealed class StatDisplayModule : IArchiveModule
    {
        public ILocalizationService LocalizationService { get; set; } = null!;
        public IArchiveLogger Logger { get; set; } = null!;

        public void Init()
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Load()
        {
            ArchiveMod.RegisterArchiveModule(typeof(StatDisplayModule));
        }
    }
}