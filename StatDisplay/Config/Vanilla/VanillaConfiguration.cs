using BepInEx;
using BepInEx.Configuration;
using GTFO.API.Utilities;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Numerics;

namespace StatDisplay.Config.Vanilla
{
    public class VanillaConfiguration : IConfiguration
    {
        private readonly ConfigEntry<PresetConfigSetting> _format;
        private readonly ConfigEntry<bool> _overrideFormat;
        private readonly ConfigEntry<bool> _formatFullName;
        private readonly ConfigEntry<string> _customFormat;
        public string Format { get; private set; }
        public bool FormatFullName => _formatFullName.Value;
        private readonly ConfigEntry<string> _offset;
        public Vector2 Offset { get; private set; }
        private readonly string[] _colors = (string[])IConfiguration.DefaultColors.Clone();
        private readonly ConfigEntry<string> _hitColor;
        private readonly ConfigEntry<string> _critColor;
        private readonly ConfigEntry<string> _pierceColor;
        private readonly ConfigEntry<string> _damageColor;
        private readonly ConfigEntry<string> _critDamageColor;

        private readonly ConfigEntry<PresetConfigSetting> _endScreenFormat;
        private readonly ConfigEntry<bool> _endScreenOverrideFormat;
        private readonly ConfigEntry<string> _endScreenCustomFormat;
        public string EndScreenFormat { get; private set; }

        public event Action? OnChanged;

        private readonly ConfigFile _configFile;

        public VanillaConfiguration()
        {
            _configFile = new ConfigFile(Path.Combine(Paths.ConfigPath, EntryPoint.MODNAME + ".cfg"), saveOnInit: true);
            string section = "Display Settings";
            string description = "Preset display settings to use.\n" +
                "- Hit: Shots Hit/Shots Fired\n" +
                "- Crit: Shots Crit/Shots Hit\n" +
                "- Pierce: All Hits/Shots Hit\n" +
                "- CritDamage: Crit Damage Dealt";
            _format = _configFile.Bind(section, "Display Format", IConfiguration.DefaultFormat, description);

            description = "Show full player names instead of shorthand names.";
            _formatFullName = _configFile.Bind(section, "Display Full Names", false, description);

            description = "x, y offset of stat data.";
            _offset = _configFile.Bind(section, "Display Offset", "0, 0", description);

            description = "Preset display format to use for the mission end screen. Displayed for each weapon.";
            _endScreenFormat = _configFile.Bind(section, "End Screen Display Format", IConfiguration.DefaultEndScreenFormat, description);

            section = "Preset Colors";
            description = "Color for Hit preset setting.";
            _hitColor = _configFile.Bind(section, "Hit Color", _colors[0], description);
            description = "Color for Crit preset setting.";
            _critColor = _configFile.Bind(section, "Crit Color", _colors[1], description);
            description = "Color for Pierce preset setting.";
            _pierceColor = _configFile.Bind(section, "Pierce Color", _colors[2], description);
            description = "Color for Damage preset setting.";
            _damageColor = _configFile.Bind(section, "Damage Color", _colors[3], description);
            description = "Color for CritDamage preset setting.";
            _critDamageColor = _configFile.Bind(section, "CritDamage Color", _colors[4], description);

            section = "Custom Settings";
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
            _customFormat = _configFile.Bind(section, "Custom Format", IConfiguration.BuildPresetFormat(IConfiguration.DefaultFormat), description);

            description = "Whether to use the custom format for the mission end screen instead of the preset format.";
            _endScreenOverrideFormat = _configFile.Bind(section, "End Screen Use Custom Format", false, description);

            description = "Custom display format to use for the mission end screen. Follows the same rules as Custom Format, but slot cannot be specified.";
            _endScreenCustomFormat = _configFile.Bind(section, "End Screen Custom Format", IConfiguration.BuildPresetFormat(IConfiguration.DefaultEndScreenFormat), description);

            CacheValues();

            LiveEditListener listener = LiveEdit.CreateListener(Paths.ConfigPath, EntryPoint.MODNAME + ".cfg", false);
            listener.FileChanged += OnFileChanged;
        }

        private void OnFileChanged(LiveEditEventArgs _)
        {
            _configFile.Reload();
            CacheValues();

            OnChanged?.Invoke();
        }

        [MemberNotNull(nameof(Format), nameof(EndScreenFormat))]
        private void CacheValues()
        {
            _colors[0] = _hitColor.Value;
            _colors[1] = _critColor.Value;
            _colors[2] = _pierceColor.Value;
            _colors[3] = _damageColor.Value;
            _colors[4] = _critDamageColor.Value;
            Offset = IConfiguration.ParseOffset(_offset.Value);
            Format = _overrideFormat.Value ? _customFormat.Value : IConfiguration.BuildPresetFormat(_format.Value, _colors);
            EndScreenFormat = _endScreenOverrideFormat.Value ? _endScreenCustomFormat.Value : IConfiguration.BuildPresetFormat(_endScreenFormat.Value, _colors);
        }
    }
}
