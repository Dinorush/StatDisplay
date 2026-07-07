using System;
using System.Numerics;
using System.Text;

namespace StatDisplay.Config
{
    public interface IConfiguration
    {
        public string Format { get; }
        public bool FormatFullName { get; }
        public Vector2 Offset { get; }
        public string EndScreenFormat { get; }
        public event Action? OnChanged;

        public static readonly string[] DefaultColors = new string[]
        {
            "#a0a0a0",
            "#ffff00",
            "#ff3080",
            "#a0a0a0",
            "#ffff00"
        };
        public const PresetConfigSetting DefaultFormat = PresetConfigSetting.Hit | PresetConfigSetting.Crit | PresetConfigSetting.Damage;
        public const PresetConfigSetting DefaultEndScreenFormat = PresetConfigSetting.Hit | PresetConfigSetting.Crit;
        public static string BuildPresetFormat(PresetConfigSetting settings) => BuildPresetFormat(settings, DefaultColors);
        public static string BuildPresetFormat(PresetConfigSetting settings, string[] colors)
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

            if (settings.HasFlag(PresetConfigSetting.Hit))
                sb.Append($"<{colors[0]}>{{Hit/Fired}}</color>");
            if (settings.HasFlag(PresetConfigSetting.Crit))
                AppendPrefixNotFirst('/', $"<{colors[1]}>{{Crit/Hit}}</color>");
            if (settings.HasFlag(PresetConfigSetting.Pierce))
                AppendPrefixNotFirst('/', $"<{colors[2]}>{{FullHit/Hit}}</color>");
            if (settings.HasFlag(PresetConfigSetting.Damage))
            {
                AppendPrefixNotFirst(' ', $"(<{colors[3]}>{{Damage}}</color>");
                if (settings.HasFlag(PresetConfigSetting.CritDamage))
                    sb.Append($"/<{colors[4]}>{{DamageCrit}}</color>");
                sb.Append(')');
            }
            else if (settings.HasFlag(PresetConfigSetting.CritDamage))
                AppendPrefixNotFirst(' ', $"(<{colors[4]}>{{DamageCrit}}</color>)");

            return sb.ToString();
        }

        public static Vector2 ParseOffset(string offset)
        {
            if (!offset.Contains(','))
                return Vector2.Zero;
            var split = offset.Split(',');
            if (!float.TryParse(split[0].Trim(), out var x) || !float.TryParse(split[1].Trim(), out var y))
                return Vector2.Zero;

            return new Vector2(x, y);
        }
    }

    [Flags]
    public enum PresetConfigSetting
    {
        None = 0,
        Hit = 1,
        Crit = 1 << 1,
        Pierce = 1 << 2,
        Damage = 1 << 3,
        CritDamage = 1 << 4
    }
}
