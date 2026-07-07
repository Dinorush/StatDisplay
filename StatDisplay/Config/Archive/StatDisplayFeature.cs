using StatDisplay.Handler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using TheArchive.Core.Attributes.Feature;
using TheArchive.Core.Attributes.Feature.Members;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.FeaturesAPI.Settings;
using TheArchive.Core.Models;
using TheArchive.Utilities;

namespace StatDisplay.Config.Archive
{
    [EnableFeatureByDefault]
    public class StatDisplayFeature : Feature, IConfiguration
    {
        public static StatDisplayFeature Instance { get; private set; } = null!;

        public override string Name => "Stat Display";

        public override string Description => "Displays in-game accuracy and damage.";

        public override FeatureGroup Group => FeatureGroups.Hud;

        public override bool SkipInitialOnEnable => true;

        public string Format { get; private set; } = string.Empty;

        public bool FormatFullName => Settings.FormatFullName;

        public Vector2 Offset { get; private set; }

        public string EndScreenFormat { get; private set; } = string.Empty;

        public event Action? OnChanged;

        private readonly string[] _colors = new string[5];

        public override void Init()
        {
            Instance = this;
            if (Settings.IsFirstTime)
            {
                Settings.Format = new()
                {
                    PresetConfigSetting.Hit,
                    PresetConfigSetting.Crit,
                    PresetConfigSetting.Damage
                };

                Settings.EndScreenFormat = new()
                {
                    PresetConfigSetting.Hit,
                    PresetConfigSetting.Crit
                };

                Settings.IsFirstTime = false;
            }
            CacheValues();
        }

        public override void OnFeatureSettingChanged(FeatureSetting setting)
        {
            CacheValues();
            OnChanged?.Invoke();
        }

        private void CacheValues()
        {
            _colors[0] = Settings.HitColor.ToHexString();
            _colors[1] = Settings.CritColor.ToHexString();
            _colors[2] = Settings.PierceColor.ToHexString();
            _colors[3] = Settings.DamageColor.ToHexString();
            _colors[4] = Settings.CritDamageColor.ToHexString();
            Offset = IConfiguration.ParseOffset(Settings.Offset);
            Format = Settings.OverrideFormat ? Settings.CustomFormat : IConfiguration.BuildPresetFormat((PresetConfigSetting)Settings.Format.Sum(x => (int)x), _colors);
            EndScreenFormat = Settings.EndScreenOverrideFormat ? Settings.EndScreenCustomFormat : IConfiguration.BuildPresetFormat((PresetConfigSetting)Settings.EndScreenFormat.Sum(x => (int)x), _colors);
        }

        public override void OnEnable()
        {
            CacheValues();
            OnChanged?.Invoke();
            StatHandler.SetVisible(true);
        }

        public override void OnDisable()
        {
            StatHandler.SetVisible(false);
        }

        [FeatureConfig]
        public static StatDisplaySettings Settings { get; set; } = null!;

        public class StatDisplaySettings
        {
            [FSDisplayName("Display Format")]
            [FSDescription("Preset display settings to use.\n" +
                "- Hit: Shots Hit/Shots Fired\n" +
                "- Crit: Shots Crit/Shots Hit\n" +
                "- Pierce: All Hits/Shots Hit\n" +
                "- Damage: All Damage Dealt\n" +
                "- CritDamage: Crit Damage Dealt")]
            public List<PresetConfigSetting> Format { get; set; } = new();

            [FSDisplayName("Display Full Names")]
            [FSDescription("Show full player names instead of shorthand names.")]
            public bool FormatFullName { get; set; } = false;

            [FSDisplayName("Display Offset")]
            [FSDescription("x, y offset of stat data.")]
            public string Offset { get; set; } = "0, 0";

            [FSDisplayName("End Screen Display Format")]
            [FSDescription("Preset display format to use for the mission end screen. Displayed for each weapon.\n" +
                "- Hit: Shots Hit/Shots Fired\n" +
                "- Crit: Shots Crit/Shots Hit\n" +
                "- Pierce: All Hits/Shots Hit\n" +
                "- Damage: All Damage Dealt\n" +
                "- CritDamage: Crit Damage Dealt")]
            public List<PresetConfigSetting> EndScreenFormat { get; set; } = new();

            [FSHeader("Preset Colors")]
            [FSDisplayName("Hit Color")]
            [FSDescription("Color for Hit preset setting.")]
            public SColor HitColor { get; set; } = new(0.63f, 0.63f, 0.63f);

            [FSDisplayName("Crit Color")]
            [FSDescription("Color for Crit preset setting.")]
            public SColor CritColor { get; set; } = new(1f, 1f, 0f);

            [FSDisplayName("Pierce Color")]
            [FSDescription("Color for Pierce preset setting.")]
            public SColor PierceColor { get; set; } = new(1f, 0.2f, 0.5f);

            [FSDisplayName("Damage Color")]
            [FSDescription("Color for Damage preset setting.")]
            public SColor DamageColor { get; set; } = new(0.63f, 0.63f, 0.63f);

            [FSDisplayName("Crit Damage Color")]
            [FSDescription("Color for Crit Damage preset setting.")]
            public SColor CritDamageColor { get; set; } = new(1f, 1f, 0f);

            [FSHeader("Custom")]
            [FSDisplayName("Use Custom Format")]
            [FSDescription("Whether to use the custom format instead of the preset format.")]
            public bool OverrideFormat { get; set; } = false;

            [FSDisplayName("Custom Format")]
            [FSMaxLength(255)]
            [FSDescription("Custom display format to use. Use variables by placing them in \"{}\", e.g. {PrimaryShotHit}.\n" +
                "Percentages can be done by separating two variables with a \'/\', e.g. {FullHit/GroupFired}.\n" +
                "Variables are composed of slot type, shot type, and stat type. They may occur in any order and are case-insensitive.\n" +
                "Accepted slots (Unspecified = All):\n" +
                "- All\n" +
                "- Primary = Main\n" +
                "- Secondary = Special\n" +
                "- Melee (Damage Only)\n" +
                "- Tool = Class (Damage Only)\n" +
                "Accepted shot types (Unspecified = Shots; Unused for Damage):\n" +
                "- Shot: Counts for each individual bullet.\n" +
                "- Group: Counts for each weapon shot (once per shotgun blast).\n" +
                "- Full: Includes all hits. Counts each damaging effect as a shot fired (including EWC effects).\n" +
                "Accepted accuracy stat types (Unspecified = Hit):\n" +
                "- Hit: Number of hits.\n" +
                "- Crit: Number of crits.\n" +
                "- Fired: Number of times fired.\n" +
                "Accepted damage stat types (Unspecified = Any):\n" +
                "- Any: Damage from any hit.\n" +
                "- Crit: Damage from weakspot hits.")]
            public string CustomFormat { get; set; } = IConfiguration.BuildPresetFormat(IConfiguration.DefaultFormat);

            [FSDisplayName("End Screen Use Custom Format")]
            [FSDescription("Whether to use the custom format for the mission end screen instead of the preset format.")]
            public bool EndScreenOverrideFormat { get; set; } = false;

            [FSDisplayName("End Screen Custom Format")]
            [FSMaxLength(255)]
            [FSDescription("Custom display format to use for the mission end screen. Use variables by placing them in \"{}\", e.g. {ShotHit}.\n" +
                "Percentages can be done by separating two variables with a \'/\', e.g. {FullHit/GroupFired}.\n" +
                "Variables are composed of shot type and stat type. They may occur in any order and are case-insensitive.\n" +
                "Accepted shot types (Unspecified = Shots; Unused for Damage):\n" +
                "- Shot: Counts for each individual bullet.\n" +
                "- Group: Counts for each weapon shot (once per shotgun blast).\n" +
                "- Full: Includes all hits. Counts each damaging effect as a shot fired (including EWC effects).\n" +
                "Accepted accuracy stat types (Unspecified = Hit):\n" +
                "- Hit: Number of hits.\n" +
                "- Crit: Number of crits.\n" +
                "- Fired: Number of times fired.\n" +
                "Accepted damage stat types (Unspecified = Any):\n" +
                "- Any: Damage from any hit.\n" +
                "- Crit: Damage from weakspot hits.")]
            public string EndScreenCustomFormat { get; set; } = IConfiguration.BuildPresetFormat(IConfiguration.DefaultEndScreenFormat);

            [FSHide]
            public bool IsFirstTime { get; set; } = true;
        }
    }
}
