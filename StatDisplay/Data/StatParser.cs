using Player;
using SNetwork;
using StatDisplay.Config;
using StatDisplay.Utils.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;

namespace StatDisplay.Data
{
    public sealed class StatParser
    {
        const char StartToken = '{';
        const char EndToken = '}';
        const string NoModFormat = "N/A (<#a0a0a0>{Damage}</color>)";
        public static readonly string[] SlotNames = new string[] { "RED", "GRE", "BLU", "PUR", "EX" };

        private List<string> _strings;
        private List<Token> _tokens;
        private StringBuilder _builder;
        private readonly StatData _parent;
        private readonly string _characterName;
        private int _characterSlot;
        private string FormattedSlotName
        {
            get
            {
                string name;
                if (Configuration.Config.FormatFullName)
                    name = _characterName;
                else
                    name = _characterSlot < 4 ? SlotNames[_characterSlot] : SlotNames[^1] + (_characterSlot - 3);
                return $"<#{ColorUtility.ToHtmlStringRGB(PlayerManager.Current.m_playerColors[_characterSlot])}>{name}</color>: ";
            }
        }

        private string _text = "";
        public string Text
        {
            get
            {
                if (_needsUpdate)
                    UpdateInner();
                return _text;
            }
        }
        private TextMeshPro? _textMesh = null;
        public TextMeshPro? TextMesh => _textMesh;

        private bool _hasMod = false;
        public bool HasMod
        {
            get => _hasMod;
            set
            {
                if (_hasMod != value)
                {
                    _hasMod = value;
                    SetFormat();
                }
            }
        }

        private bool _needsUpdate = false;
        private bool _meshNeedsUpdate = false;

        public StatParser(StatData data, SNet_Player player, bool hasMod)
        {
            _characterName = player.NickName;
            _parent = data;
            _hasMod = hasMod;
            SetFormat();
            Configuration.Config.OnChanged += SetFormat;
        }

        public void SetToSlot(TextMeshPro textMesh, int slot)
        {
            _textMesh = textMesh;
            _characterSlot = slot;
            Update(true);
        }

        public void SetToEmpty()
        {
            _textMesh = null;
            Update(true);
        }

        public void UpdateMesh()
        {
            if (_meshNeedsUpdate)
            {
                _meshNeedsUpdate = false;
                TextMesh?.SetText(Text);
            }
        }

        public void Update(bool updateMesh = false)
        {
            _needsUpdate = true;
            _meshNeedsUpdate = !updateMesh;
            if (updateMesh)
                TextMesh?.SetText(Text);
        }

        public string GetEndscreenText(InventorySlot slot)
        {
            ExtractFormatInfo(Configuration.Config.EndScreenFormat, out var builder, out var strings, out var tokens, slot);
            return CreateText(builder, strings, tokens);
        }

        [MemberNotNull(nameof(_builder), nameof(_strings), nameof(_tokens))]
        private void SetFormat()
        {
            ExtractFormatInfo(_hasMod ? Configuration.Config.Format : NoModFormat, out _builder, out _strings, out _tokens);
            Update();
        }

        private void ExtractFormatInfo(string format, out StringBuilder builder, out List<string> strings, out List<Token> tokens, InventorySlot restrictSlot = InventorySlot.None)
        {
            strings = new();
            tokens = new();
            int lastIndex = 0;
            int endIndex;
            for (int index = format.IndexOf(StartToken); index != -1; index = format.IndexOf(StartToken, endIndex + 1))
            {
                endIndex = format.IndexOf(EndToken, index + 1);
                if (endIndex == -1) break;

                strings.Add(format[lastIndex..index]);
                tokens.Add(new Token(_parent, format[(index + 1)..endIndex], restrictSlot));
                lastIndex = endIndex + 1;
            }
            strings.Add(format[lastIndex..]);
            builder = new(6 + _strings.Sum(segment => segment.Length) + _tokens.Count * 11);
        }

        private void UpdateInner()
        {
            _builder.Append(FormattedSlotName);
            _text = CreateText(_builder, _strings, _tokens);
            _needsUpdate = false;
        }

        private static string CreateText(StringBuilder builder, List<string> strings, List<Token> tokens)
        {
            for (int i = 0; i < strings.Count; i++)
            {
                builder.Append(strings[i]);
                if (i < tokens.Count)
                    builder.Append(tokens[i].GetString());
            }
            var text = builder.ToString();
            builder.Clear();
            return text;
        }

        class Token
        {
            // Exclude Count/All
            private readonly static string[] s_accSlotTypes = Array.ConvertAll(Enum.GetValues<AccSlotType>()[..(int)AccSlotType.Count], slot => slot.ToString().ToLower());
            private readonly static string[] s_accShotTypes = Array.ConvertAll(Enum.GetValues<AccShotType>()[..(int)AccShotType.Count], shot => shot.ToString().ToLower());
            private readonly static string[] s_accStatTypes = Array.ConvertAll(Enum.GetValues<AccStatType>()[..(int)AccStatType.Count], stat => stat.ToString().ToLower());
            private readonly static string[] s_accWeightTypes = Array.ConvertAll(Enum.GetValues<AccWeightType>()[1..], stat => stat.ToString().ToLower());
            private readonly static string[] s_damSlotTypes = Array.ConvertAll(Enum.GetValues<DamSlotType>()[..(int)DamSlotType.Count], slot => slot.ToString().ToLower());
            private readonly static string[] s_damStatTypes = Array.ConvertAll(Enum.GetValues<DamStatType>()[..(int)DamStatType.Count], stat => stat.ToString().ToLower());

            private readonly static Dictionary<string, AccSlotType> s_accSlotExtraMap = new()
            {
                { "main", AccSlotType.Primary },
                { "special", AccSlotType.Secondary }
            };
            private readonly static Dictionary<string, DamSlotType> s_damSlotExtraMap = new()
            {
                { "main", DamSlotType.Primary },
                { "special", DamSlotType.Secondary },
                { "class", DamSlotType.Tool }
            };

            private readonly StatData.Retriever _base;
            private readonly StatData.Retriever? _divider;

            public Token(StatData data, string text, InventorySlot slot)
            {
                int divIndex = text.IndexOf('/');
                if (divIndex != -1)
                {
                    _divider = ParseRetriever(data, text[(divIndex + 1)..].Trim().ToLower(), slot);
                    text = text[..divIndex];
                }
                else
                    _divider = null;

                _base = ParseRetriever(data, text.Trim().ToLower(), slot);
                _base.AlignWith(_divider);
            }

            public string GetString()
            {
                var baseValue = _base.Value;
                if (_divider != null)
                {
                    var divValue = _divider.Value;
                    if (divValue == 0) return baseValue > 0 ? "100%" : "-";
                    return (baseValue / divValue).ToString("P0");
                }
                return ((long)baseValue).ToString();
            }
            
            private static StatData.Retriever ParseRetriever(StatData data, string text, InventorySlot slot)
            {
                return text.Contains("damage", StringComparison.CurrentCultureIgnoreCase) ? ParseDamageRetriever(data, text, slot) : ParseAccuracyRetriever(data, text, slot);
            }

            private static StatData.Retriever ParseDamageRetriever(StatData data, string text, InventorySlot slot)
            {
                DamSlotType[] slotTypes;
                if (slot != InventorySlot.None)
                    slotTypes = new[] { slot.ToDamSlotType() };
                else if (!TryGetEnumsInText(text, s_damSlotTypes, s_damSlotExtraMap, out slotTypes))
                    slotTypes = new[] { DamSlotType.All };

                if (!TryGetEnumInText(text, s_damStatTypes, out DamStatType statType))
                    statType = DamStatType.Any;

                return StatData.CreateDamageRetriever(data, slotTypes, statType);
            }

            private static StatData.Retriever ParseAccuracyRetriever(StatData data, string text, InventorySlot slot)
            {
                RemoveWeightFromText(ref text, out AccWeightType weightType);

                AccSlotType[] slotTypes;
                if (slot != InventorySlot.None)
                    slotTypes = new[] { slot.ToAccSlotType() };
                else if (!TryGetEnumsInText(text, s_accSlotTypes, s_accSlotExtraMap, out slotTypes))
                    slotTypes = new[] { AccSlotType.All };

                if (!TryGetEnumInText(text, s_accShotTypes, out AccShotType shotType))
                    shotType = AccShotType.Shot;
                if (!TryGetEnumInText(text, s_accStatTypes, out AccStatType statType))
                    statType = AccStatType.Hit;

                return StatData.CreateAccuracyRetriever(data, slotTypes, shotType, statType, weightType);
            }

            private static bool TryGetEnumInText<T>(string text, string[] enumStrings, out T value) where T : struct
            {
                int index = -1;
                int length = 0;
                foreach (var str in enumStrings)
                {
                    if ((index = text.IndexOf(str)) > -1)
                    {
                        length = str.Length;
                        break;
                    }
                }

                if (index != -1)
                {
                    var enumText = text.Substring(index, length);
                    if (enumText.TryToEnum(out value))
                        return true;
                    else
                    {
                        DinoLogger.Error($"Unable to parse {typeof(T).Name} from {enumText} in token \"{text}\"");
                        return false;
                    }
                }

                value = default;
                return false;
            }

            private static void RemoveWeightFromText(ref string text, out AccWeightType value)
            {
                int start = text.IndexOf('(');
                if (start == -1)
                {
                    value = AccWeightType.None;
                    return;
                }

                int end = text.LastIndexOf(')');
                if (end == -1)
                {
                    value = AccWeightType.None;
                    return;
                }

                var searchStr = text[(start + 1)..end];
                text = text.Remove(start, end - start + 1);
                if (!TryGetEnumInText(searchStr, s_accWeightTypes, out value))
                    value = AccWeightType.None;
            }

            private static bool TryGetEnumsInText<T>(string text, string[] enumStrings, Dictionary<string, T> extraStrings, out T[] value) where T : struct
            {
                List<(int index, int length)> enums = new();
                foreach (var str in enumStrings)
                {
                    int index = text.IndexOf(str);
                    if (index > -1)
                        enums.Add((index, str.Length));
                }

                HashSet<T> result = new(enums.Count);
                foreach ((var str, var enumVal) in extraStrings)
                    if (text.IndexOf(str) > -1)
                        result.Add(enumVal);

                foreach ((var index, var length) in enums)
                {
                    var enumText = text.Substring(index, length);
                    if (enumText.TryToEnum(out T enumVal))
                        result.Add(enumVal);
                    else
                        DinoLogger.Error($"Unable to parse {typeof(T).Name} from {enumText} in token \"{text}\"");
                }

                if (result.Count > 0)
                {
                    value = result.ToArray();
                    return true;
                }

                value = Array.Empty<T>();
                return false;
            }
        }
    }
}
