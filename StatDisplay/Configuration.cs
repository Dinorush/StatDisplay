using BepInEx;
using BepInEx.Configuration;
using GTFO.API.Utilities;
using StatDisplay.Attributes;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Numerics;
using System.Text;

namespace StatDisplay
{
    public static class Configuration
    {
        private static readonly ConfigEntry<PresetSettings> _format;
        private static readonly ConfigEntry<bool> _overrideFormat;
        private static readonly ConfigEntry<bool> _formatFullName;
        private static readonly ConfigEntry<string> _customFormat;
        public static string Format { get; set; }
        public static bool FormatFullName => _formatFullName.Value;
        private static readonly ConfigEntry<string> _offset;
        public static Vector2 Offset { get; private set; }

        private static readonly ConfigEntry<PresetSettings> _endScreenFormat;
        private static readonly ConfigEntry<bool> _endScreenOverrideFormat;
        private static readonly ConfigEntry<string> _endScreenCustomFormat;
        public static string EndScreenFormat { get; set; }

        private const PresetSettings DefaultFormat = PresetSettings.Hit | PresetSettings.Crit | PresetSettings.Damage;
        private const PresetSettings DefaultEndScreenFormat = PresetSettings.Hit | PresetSettings.Crit;

        public static event Action? OnReload;

        private readonly static ConfigFile _configFile;

        [InvokeOnLoad]
        private static void Init()
        {
            LiveEditListener listener = LiveEdit.CreateListener(Paths.ConfigPath, EntryPoint.MODNAME + ".cfg", false);
            listener.FileChanged += OnFileChanged;
        }

        static Configuration()
        {
            _configFile = new ConfigFile(Path.Combine(Paths.ConfigPath, EntryPoint.MODNAME + ".cfg"), saveOnInit: true);
            string section = "Display Settings";
            string description = "Preset display settings to use.\n" +
                "- Hit: Shots Hit/Shots Fired\n" +
                "- Crit: Shots Crit/Shots Hit\n" +
                "- Pierce: All Hits/Shots Hit\n" +
                "- Damage: All Damage Dealt";
            _format = _configFile.Bind(section, "Display Format", DefaultFormat, description);

            description = "x, y offset of accuracy data.";
            _offset = _configFile.Bind(section, "Display Offset", "0, 0", description);

            description = "Show full player names instead of shorthand names.";
            _formatFullName = _configFile.Bind(section, "Display Full Names", false, description);

            description = "Preset display format to use for the mission end screen. Displayed for each weapon.";
            _endScreenFormat = _configFile.Bind(section, "End Screen Display Format", DefaultEndScreenFormat, description);

            description = "Whether to use the custom format instead of the preset format.";
            _overrideFormat = _configFile.Bind(section, "Use Custom Format", false, description);

            description = "Custom display format to use. Use variables by placing them in \"{}\", e.g. {PrimaryShotHit}.\n" +
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
                "- Crit: Damage from weakspot hits.";
            _customFormat = _configFile.Bind(section, "Custom Format", BuildPresetFormat(DefaultFormat), description);

            description = "Whether to use the custom format for the mission end screen instead of the preset format.";
            _endScreenOverrideFormat = _configFile.Bind(section, "End Screen Use Custom Format", false, description);

            description = "Custom display format to use for the mission end screen. Follows the same rules as Custom Format, but slot cannot be specified.";
            _endScreenCustomFormat = _configFile.Bind(section, "End Screen Custom Format", BuildPresetFormat(DefaultEndScreenFormat), description);

            CacheValues();
        }

        private static void OnFileChanged(LiveEditEventArgs _)
        {
            _configFile.Reload();
            CacheValues();

            OnReload?.Invoke();
        }

        [MemberNotNull(nameof(Format), nameof(EndScreenFormat))]
        private static void CacheValues()
        {
            Offset = ParseOffset();
            Format = _overrideFormat.Value ? _customFormat.Value : BuildPresetFormat(_format.Value);
            EndScreenFormat = _endScreenOverrideFormat.Value ? _endScreenCustomFormat.Value : BuildPresetFormat(_endScreenFormat.Value);
        }

        private static string BuildPresetFormat(PresetSettings settings)
        {
            StringBuilder sb = new();
            void AppendPrefixNotFirst(char prefix, string value)
            {
                if (sb.Length == 0)
                    sb.Append(value);
                else
                {
                    sb.Append(prefix);
                    sb.Append(value);
                }
            }
            if (settings.HasFlag(PresetSettings.Hit))
                sb.Append("<#a0a0a0>{Hit/Fired}</color>");
            if (settings.HasFlag(PresetSettings.Crit))
                AppendPrefixNotFirst('/', "<#ffff00>{Crit/Hit}</color>");
            if (settings.HasFlag(PresetSettings.Pierce))
                AppendPrefixNotFirst('/', "<#ff3080>{FullHit/Hit}</color>");
            if (settings.HasFlag(PresetSettings.Damage))
                AppendPrefixNotFirst(' ', "(<#a0a0a0>{Damage}</color>)");
            return sb.ToString();
        }

        private static Vector2 ParseOffset()
        {
            if (!_offset.Value.Contains(','))
                return Vector2.Zero;
            var split = _offset.Value.Split(',');
            if (!float.TryParse(split[0].Trim(), out var x) || !float.TryParse(split[1].Trim(), out var y))
                return Vector2.Zero;

            return new Vector2(x, y);
        }

        [Flags]
        enum PresetSettings
        {
            None = 0,
            Hit = 1,
            Crit = 1 << 1,
            Pierce = 1 << 2,
            Damage = 1 << 3
        }
    }
}
