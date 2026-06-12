using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace FWEledit
{
    public sealed class EditableTitleDefinition
    {
        public int Id { get; set; }
        public string TitleText { get; set; }
        public string AccentHex { get; set; }
        public string Description { get; set; }
        public string[] AddonDescriptions { get; set; }
        public string IconPath { get; set; }
        public bool IsGraphicTitle { get; set; }
    }

    public static class TitleDefinitionCatalog
    {
        public const int TargetListIndex = -3;

        private static readonly object SyncRoot = new object();
        private static readonly Regex EntryRegex = new Regex(
            "title_definition\\[\\s*\"?(?<id>\\d+)\"?\\s*\\]\\s*=\\s*\\{(?<body>.*?)\\}",
            RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex NoteRegex = new Regex(
            "note\\s*=\\s*\"(?<value>(?:\\\\.|[^\"])*)\"",
            RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex DescriptionRegex = new Regex(
            "desc\\s*=\\s*\"(?<value>(?:\\\\.|[^\"])*)\"",
            RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex AddonDescriptionRegex = new Regex(
            "addon_desc\\d+\\s*=\\s*\"(?<value>(?:\\\\.|[^\"])*)\"",
            RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex IconBlockRegex = new Regex(
            "title_definition\\[\\s*\"?(?<id>\\d+)\"?\\s*\\]\\.icon\\s*=\\s*\\{(?<body>.*?)\\}",
            RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex IconImageRegex = new Regex(
            "image\\s*=\\s*\"(?<value>(?:\\\\.|[^\"])*)\"",
            RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex LeadingColorRegex = new Regex(
            "\\^(?<hex>[0-9a-fA-F]{6})",
            RegexOptions.Compiled);
        private static string cachedGameRoot = string.Empty;
        private static string cachedLuaText = string.Empty;
        private static bool cacheInitialized;
        private static List<ItemReferenceOption> cachedOptions = new List<ItemReferenceOption>();
        private static Dictionary<int, ItemReferenceOption> cachedById = new Dictionary<int, ItemReferenceOption>();
        private static Dictionary<string, ItemReferenceOption> cachedByName = new Dictionary<string, ItemReferenceOption>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<int, EditableTitleDefinition> cachedDefinitions = new Dictionary<int, EditableTitleDefinition>();

        public static List<ItemReferenceOption> BuildOptions()
        {
            EnsureCache();
            lock (SyncRoot)
            {
                return CloneOptions(cachedOptions);
            }
        }

        public static bool TryGetOptionById(int id, out ItemReferenceOption option)
        {
            option = null;
            if (id <= 0)
            {
                return false;
            }

            EnsureCache();
            lock (SyncRoot)
            {
                return cachedById.TryGetValue(id, out option);
            }
        }

        public static bool TryGetOptionByName(string name, out ItemReferenceOption option)
        {
            option = null;
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            EnsureCache();
            lock (SyncRoot)
            {
                return cachedByName.TryGetValue(name.Trim(), out option);
            }
        }

        public static bool TryGetEditableDefinition(int id, out EditableTitleDefinition definition)
        {
            definition = null;
            if (id <= 0)
            {
                return false;
            }

            EnsureCache();
            lock (SyncRoot)
            {
                if (!cachedDefinitions.TryGetValue(id, out EditableTitleDefinition cached))
                {
                    return false;
                }

                definition = CloneDefinition(cached);
                return definition != null;
            }
        }

        public static void InvalidateCache()
        {
            lock (SyncRoot)
            {
                cacheInitialized = false;
                cachedGameRoot = string.Empty;
                cachedLuaText = string.Empty;
                cachedOptions = new List<ItemReferenceOption>();
                cachedById = new Dictionary<int, ItemReferenceOption>();
                cachedByName = new Dictionary<string, ItemReferenceOption>(StringComparer.OrdinalIgnoreCase);
                cachedDefinitions = new Dictionary<int, EditableTitleDefinition>();
            }
        }

        public static bool SaveEditableDefinition(EditableTitleDefinition definition, AssetManager assetManager, out string error)
        {
            error = string.Empty;
            if (definition == null || definition.Id <= 0)
            {
                error = "Invalid title definition.";
                return false;
            }
            if (assetManager == null)
            {
                error = "Asset manager is not available.";
                return false;
            }

            EnsureCache();

            string currentLuaText;
            lock (SyncRoot)
            {
                currentLuaText = cachedLuaText ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(currentLuaText))
            {
                error = "Unable to load title definitions from script.pck.";
                return false;
            }

            string updatedBlock = BuildDefinitionBlock(definition);
            string pattern = "title_definition\\[\\s*\"?" + definition.Id.ToString(CultureInfo.InvariantCulture) + "\"?\\s*\\]\\s*=\\s*\\{.*?\\}";
            string updatedLuaText;
            if (Regex.IsMatch(currentLuaText, pattern, RegexOptions.Singleline))
            {
                updatedLuaText = Regex.Replace(currentLuaText, pattern, updatedBlock, RegexOptions.Singleline);
            }
            else
            {
                int insertAt = currentLuaText.IndexOf("title_dir = {", StringComparison.Ordinal);
                updatedLuaText = insertAt >= 0
                    ? currentLuaText.Insert(insertAt, updatedBlock + Environment.NewLine + Environment.NewLine)
                    : currentLuaText + Environment.NewLine + Environment.NewLine + updatedBlock + Environment.NewLine;
            }

            if (!assetManager.EnsurePackageExtracted("script"))
            {
                error = "Unable to extract script.pck.";
                return false;
            }

            string extractedRoot = assetManager.GetExtractedPackageRoot("script");
            if (string.IsNullOrWhiteSpace(extractedRoot) || !Directory.Exists(extractedRoot))
            {
                error = "The extracted script package was not found.";
                return false;
            }

            string configDirectory = Path.Combine(extractedRoot, "config");
            Directory.CreateDirectory(configDirectory);
            string targetPath = Path.Combine(configDirectory, "title_def_u.lua");

            string tempSourcePath = Path.Combine(Path.GetTempPath(), "FWEledit", "lua-cache", "title_def_u_edit.lua");
            Directory.CreateDirectory(Path.GetDirectoryName(tempSourcePath) ?? Path.GetTempPath());
            File.WriteAllText(tempSourcePath, updatedLuaText, new UTF8Encoding(false));

            if (!CompileLuaFile(tempSourcePath, targetPath, out error))
            {
                return false;
            }

            assetManager.MarkWorkspaceFileChanged(targetPath);
            assetManager.MarkWorkspacePackageDirty("script");

            if (!assetManager.ApplyWorkspacePackageToGame("script", out string applySummary))
            {
                error = string.IsNullOrWhiteSpace(applySummary)
                    ? "Unable to update script.pck."
                    : applySummary;
                return false;
            }

            Dictionary<int, string> iconPathsById = ParseGraphicIconPaths(updatedLuaText);
            Dictionary<int, EditableTitleDefinition> updatedDefinitions = ParseEditableDefinitions(updatedLuaText, iconPathsById);
            List<ItemReferenceOption> updatedOptions = BuildOptionsFromDefinitions(updatedDefinitions);
            lock (SyncRoot)
            {
                cachedLuaText = updatedLuaText;
                cachedDefinitions = updatedDefinitions;
                cachedOptions = updatedOptions;
                cachedById = new Dictionary<int, ItemReferenceOption>();
                cachedByName = new Dictionary<string, ItemReferenceOption>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < cachedOptions.Count; i++)
                {
                    ItemReferenceOption option = cachedOptions[i];
                    if (option == null)
                    {
                        continue;
                    }

                    if (!cachedById.ContainsKey(option.Id))
                    {
                        cachedById.Add(option.Id, option);
                    }
                    if (!string.IsNullOrWhiteSpace(option.Name) && !cachedByName.ContainsKey(option.Name))
                    {
                        cachedByName.Add(option.Name, option);
                    }
                }
            }

            return true;
        }

        public static string FormatDisplay(string rawValue)
        {
            int id;
            if (!int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out id) || id <= 0)
            {
                return rawValue ?? string.Empty;
            }

            ItemReferenceOption option;
            if (TryGetOptionById(id, out option) && option != null && !string.IsNullOrWhiteSpace(option.Name))
            {
                return option.Name;
            }

            return rawValue ?? string.Empty;
        }

        public static string NormalizeInput(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            string trimmed = value.Trim();
            int numericValue;
            if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out numericValue))
            {
                return numericValue.ToString(CultureInfo.InvariantCulture);
            }

            ItemReferenceOption option;
            if (TryGetOptionByName(trimmed, out option) && option != null)
            {
                return option.Id.ToString(CultureInfo.InvariantCulture);
            }

            int separator = trimmed.IndexOf(" - ", StringComparison.OrdinalIgnoreCase);
            if (separator > 0)
            {
                string leadingId = trimmed.Substring(0, separator).Trim();
                if (int.TryParse(leadingId, NumberStyles.Integer, CultureInfo.InvariantCulture, out numericValue))
                {
                    return numericValue.ToString(CultureInfo.InvariantCulture);
                }
            }

            return trimmed;
        }

        private static void EnsureCache()
        {
            string gameRoot = AssetManager.GameRootPath ?? string.Empty;
            lock (SyncRoot)
            {
                if (cacheInitialized && string.Equals(cachedGameRoot, gameRoot, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                cachedGameRoot = gameRoot;
                cacheInitialized = true;
                cachedLuaText = string.Empty;
                cachedOptions = new List<ItemReferenceOption>();
                cachedById = new Dictionary<int, ItemReferenceOption>();
                cachedByName = new Dictionary<string, ItemReferenceOption>(StringComparer.OrdinalIgnoreCase);
                cachedDefinitions = new Dictionary<int, EditableTitleDefinition>();
            }

            string luaText = LoadTitleDefinitionLua(gameRoot);
            Dictionary<int, string> iconPathsById = ParseGraphicIconPaths(luaText);
            Dictionary<int, EditableTitleDefinition> parsedDefinitions = ParseEditableDefinitions(luaText, iconPathsById);
            List<ItemReferenceOption> parsedOptions = BuildOptionsFromDefinitions(parsedDefinitions);

            lock (SyncRoot)
            {
                cachedLuaText = luaText ?? string.Empty;
                cachedDefinitions = parsedDefinitions;
                cachedOptions = parsedOptions;
                cachedById = new Dictionary<int, ItemReferenceOption>();
                cachedByName = new Dictionary<string, ItemReferenceOption>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < cachedOptions.Count; i++)
                {
                    ItemReferenceOption option = cachedOptions[i];
                    if (option == null)
                    {
                        continue;
                    }

                    if (!cachedById.ContainsKey(option.Id))
                    {
                        cachedById.Add(option.Id, option);
                    }
                    if (!string.IsNullOrWhiteSpace(option.Name) && !cachedByName.ContainsKey(option.Name))
                    {
                        cachedByName.Add(option.Name, option);
                    }
                }
            }
        }

        private static string LoadTitleDefinitionLua(string gameRoot)
        {
            if (string.IsNullOrWhiteSpace(gameRoot))
            {
                return string.Empty;
            }

            try
            {
                PckEntryReaderService reader = new PckEntryReaderService();
                byte[] payload;
                string error;
                if (reader.TryReadFile("script", "config\\title_def_u.lua", out payload, out error)
                    && payload != null
                    && payload.Length > 0)
                {
                    string tempDirectory = Path.Combine(Path.GetTempPath(), "FWEledit", "lua-cache");
                    Directory.CreateDirectory(tempDirectory);
                    string sourcePath = Path.Combine(tempDirectory, "title_def_u.lua");
                    File.WriteAllBytes(sourcePath, payload);
                    return DecompileLuaFile(sourcePath);
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        private static Dictionary<int, EditableTitleDefinition> ParseEditableDefinitions(string luaText, Dictionary<int, string> iconPathsById)
        {
            Dictionary<int, EditableTitleDefinition> definitions = new Dictionary<int, EditableTitleDefinition>();
            if (string.IsNullOrWhiteSpace(luaText))
            {
                return definitions;
            }
            MatchCollection matches = EntryRegex.Matches(luaText);
            for (int i = 0; i < matches.Count; i++)
            {
                Match match = matches[i];
                int id;
                if (match == null
                    || !int.TryParse(match.Groups["id"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out id)
                    || id <= 0)
                {
                    continue;
                }

                string body = match.Groups["body"].Value ?? string.Empty;
                string rawNote = ExtractFieldRaw(body, NoteRegex);
                string note = DecodeScriptLiteralPreserveFormatting(rawNote);
                string description = DecodeScriptLiteralPreserveFormatting(ExtractFieldRaw(body, DescriptionRegex));
                string iconPath;
                iconPathsById.TryGetValue(id, out iconPath);
                string accentHex = ExtractLeadingColor(rawNote);
                EditableTitleDefinition definition = new EditableTitleDefinition
                {
                    Id = id,
                    AccentHex = accentHex,
                    TitleText = StripLeadingColorCode(note),
                    Description = description,
                    AddonDescriptions = new string[5],
                    IconPath = iconPath ?? string.Empty,
                    IsGraphicTitle = !string.IsNullOrWhiteSpace(iconPath)
                };

                for (int addonIndex = 1; addonIndex <= 5; addonIndex++)
                {
                    definition.AddonDescriptions[addonIndex - 1] = DecodeScriptLiteralPreserveFormatting(
                        ExtractNamedStringFieldRaw(body, "addon_desc" + addonIndex.ToString(CultureInfo.InvariantCulture)));
                }

                definitions[id] = definition;
            }

            return definitions;
        }

        private static List<ItemReferenceOption> BuildOptionsFromDefinitions(Dictionary<int, EditableTitleDefinition> definitions)
        {
            List<ItemReferenceOption> options = new List<ItemReferenceOption>();
            if (definitions != null)
            {
                foreach (KeyValuePair<int, EditableTitleDefinition> pair in definitions)
                {
                    ItemReferenceOption option = BuildDisplayOption(pair.Value);
                    if (option != null)
                    {
                        options.Add(option);
                    }
                }
            }

            options.Sort(delegate (ItemReferenceOption left, ItemReferenceOption right)
            {
                string leftName = left != null ? left.Name ?? string.Empty : string.Empty;
                string rightName = right != null ? right.Name ?? string.Empty : string.Empty;
                int compare = string.Compare(leftName, rightName, StringComparison.OrdinalIgnoreCase);
                if (compare != 0)
                {
                    return compare;
                }

                int leftId = left != null ? left.Id : 0;
                int rightId = right != null ? right.Id : 0;
                return leftId.CompareTo(rightId);
            });

            return options;
        }

        private static ItemReferenceOption BuildDisplayOption(EditableTitleDefinition definition)
        {
            if (definition == null || definition.Id <= 0)
            {
                return null;
            }

            string noteRaw = ComposeNoteRaw(definition);
            string label = CleanScriptText(noteRaw);
            if (string.IsNullOrWhiteSpace(label))
            {
                label = "Title " + definition.Id.ToString(CultureInfo.InvariantCulture);
            }

            List<string> addonDescriptions = new List<string>();
            if (definition.AddonDescriptions != null)
            {
                for (int i = 0; i < definition.AddonDescriptions.Length; i++)
                {
                    string display = CleanScriptText(definition.AddonDescriptions[i]);
                    if (!string.IsNullOrWhiteSpace(display))
                    {
                        addonDescriptions.Add(display);
                    }
                }
            }

            string description = CleanScriptText(definition.Description);
            return new ItemReferenceOption
            {
                ListIndex = TargetListIndex,
                ElementIndex = -1,
                Id = definition.Id,
                Name = label,
                ListName = "Title definitions",
                IconKey = definition.IconPath ?? string.Empty,
                Quality = -1,
                Description = BuildTitleDetails(description, addonDescriptions, definition.IconPath, definition.AccentHex),
                SecondaryText = BuildSecondaryText(description, addonDescriptions, definition.IsGraphicTitle, definition.AccentHex),
                AccentHex = definition.AccentHex ?? string.Empty,
                Kind = definition.IsGraphicTitle ? "Graphic title" : "Title"
            };
        }

        private static Dictionary<int, string> ParseGraphicIconPaths(string luaText)
        {
            Dictionary<int, string> iconPaths = new Dictionary<int, string>();
            if (string.IsNullOrWhiteSpace(luaText))
            {
                return iconPaths;
            }

            MatchCollection matches = IconBlockRegex.Matches(luaText);
            for (int i = 0; i < matches.Count; i++)
            {
                Match match = matches[i];
                int id;
                if (match == null
                    || !int.TryParse(match.Groups["id"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out id)
                    || id <= 0)
                {
                    continue;
                }

                string iconPath = ExtractFieldValue(match.Groups["body"].Value ?? string.Empty, IconImageRegex);
                if (string.IsNullOrWhiteSpace(iconPath))
                {
                    continue;
                }

                iconPaths[id] = NormalizeImagePath(iconPath);
            }

            return iconPaths;
        }

        private static string BuildTitleDetails(string description, List<string> addonDescriptions, string iconPath, string accentHex)
        {
            List<string> lines = new List<string>();
            if (!string.IsNullOrWhiteSpace(description))
            {
                lines.Add(description.Trim());
            }

            if (!string.IsNullOrWhiteSpace(accentHex))
            {
                if (lines.Count > 0)
                {
                    lines.Add(string.Empty);
                }

                lines.Add("Title color: #" + accentHex.Trim().TrimStart('#').ToUpperInvariant());
            }

            if (!string.IsNullOrWhiteSpace(iconPath))
            {
                if (lines.Count > 0)
                {
                    lines.Add(string.Empty);
                }

                lines.Add("Graphic title");
                lines.Add("Asset: " + iconPath);
            }

            if (addonDescriptions != null && addonDescriptions.Count > 0)
            {
                if (lines.Count > 0)
                {
                    lines.Add(string.Empty);
                }

                lines.Add("Bonuses:");
                for (int i = 0; i < addonDescriptions.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(addonDescriptions[i]))
                    {
                        lines.Add("- " + addonDescriptions[i]);
                    }
                }
            }

            return string.Join(Environment.NewLine, lines.ToArray()).Trim();
        }

        private static string BuildSecondaryText(string description, List<string> addonDescriptions, bool isGraphicTitle, string accentHex)
        {
            string colorLabel = string.IsNullOrWhiteSpace(accentHex)
                ? string.Empty
                : "Color #" + accentHex.Trim().TrimStart('#').ToUpperInvariant();

            if (addonDescriptions != null)
            {
                for (int i = 0; i < addonDescriptions.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(addonDescriptions[i]))
                    {
                        if (isGraphicTitle && !string.IsNullOrWhiteSpace(colorLabel))
                        {
                            return "Graphic title | " + colorLabel + " | " + addonDescriptions[i];
                        }

                        if (isGraphicTitle)
                        {
                            return "Graphic title | " + addonDescriptions[i];
                        }

                        return !string.IsNullOrWhiteSpace(colorLabel)
                            ? colorLabel + " | " + addonDescriptions[i]
                            : addonDescriptions[i];
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(description))
            {
                if (isGraphicTitle && !string.IsNullOrWhiteSpace(colorLabel))
                {
                    return "Graphic title | " + colorLabel + " | " + description;
                }

                if (isGraphicTitle)
                {
                    return "Graphic title | " + description;
                }

                return !string.IsNullOrWhiteSpace(colorLabel)
                    ? colorLabel + " | " + description
                    : description;
            }

            if (isGraphicTitle && !string.IsNullOrWhiteSpace(colorLabel))
            {
                return "Graphic title | " + colorLabel;
            }

            if (isGraphicTitle)
            {
                return "Graphic title";
            }

            return !string.IsNullOrWhiteSpace(colorLabel)
                ? colorLabel
                : "Title";
        }

        private static string ExtractLeadingColor(string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return string.Empty;
            }

            Match match = LeadingColorRegex.Match(rawValue);
            if (!match.Success)
            {
                return string.Empty;
            }

            return (match.Groups["hex"].Value ?? string.Empty).Trim();
        }

        private static string ExtractFieldRaw(string body, Regex regex)
        {
            if (string.IsNullOrWhiteSpace(body) || regex == null)
            {
                return string.Empty;
            }

            Match match = regex.Match(body);
            return match.Success ? match.Groups["value"].Value ?? string.Empty : string.Empty;
        }

        private static string ExtractFieldValue(string body, Regex regex)
        {
            return CleanScriptText(ExtractFieldRaw(body, regex));
        }

        private static List<string> ExtractFieldValues(string body, Regex regex)
        {
            List<string> values = new List<string>();
            if (string.IsNullOrWhiteSpace(body) || regex == null)
            {
                return values;
            }

            MatchCollection matches = regex.Matches(body);
            for (int i = 0; i < matches.Count; i++)
            {
                string value = CleanScriptText(matches[i].Groups["value"].Value ?? string.Empty);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    values.Add(value);
                }
            }

            return values;
        }

        private static string ExtractNamedStringFieldRaw(string body, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(body) || string.IsNullOrWhiteSpace(fieldName))
            {
                return string.Empty;
            }

            Match match = Regex.Match(
                body,
                Regex.Escape(fieldName) + "\\s*=\\s*\"(?<value>(?:\\\\.|[^\"])*)\"",
                RegexOptions.Singleline);
            return match.Success ? match.Groups["value"].Value ?? string.Empty : string.Empty;
        }

        private static string StripLeadingColorCode(string value)
        {
            string cleaned = value ?? string.Empty;
            cleaned = LeadingColorRegex.Replace(cleaned, string.Empty, 1);
            return cleaned.Trim();
        }

        private static string ComposeNoteRaw(EditableTitleDefinition definition)
        {
            if (definition == null)
            {
                return string.Empty;
            }

            string titleText = (definition.TitleText ?? string.Empty).Trim();
            string accentHex = (definition.AccentHex ?? string.Empty).Trim().TrimStart('#');
            if (accentHex.Length == 6)
            {
                return "^" + accentHex.ToUpperInvariant() + titleText;
            }

            return titleText;
        }

        private static string BuildDefinitionBlock(EditableTitleDefinition definition)
        {
            string idText = definition.Id.ToString(CultureInfo.InvariantCulture);
            StringBuilder builder = new StringBuilder();
            builder.Append("title_definition[\"").Append(idText).Append("\"] = {").Append(Environment.NewLine);
            builder.Append("  id = ").Append(idText).Append(",").Append(Environment.NewLine);
            builder.Append("  note = \"").Append(EscapeLuaString(ComposeNoteRaw(definition))).Append("\",").Append(Environment.NewLine);

            string[] addonDescriptions = definition.AddonDescriptions ?? new string[0];
            for (int i = 0; i < addonDescriptions.Length && i < 5; i++)
            {
                if (!string.IsNullOrWhiteSpace(addonDescriptions[i]))
                {
                    builder.Append("  addon_desc")
                        .Append((i + 1).ToString(CultureInfo.InvariantCulture))
                        .Append(" = \"")
                        .Append(EscapeLuaString(addonDescriptions[i]))
                        .Append("\",")
                        .Append(Environment.NewLine);
                }
            }

            builder.Append("  desc = \"")
                .Append(EscapeLuaString(definition.Description ?? string.Empty))
                .Append("\"")
                .Append(Environment.NewLine)
                .Append("}");
            return builder.ToString();
        }

        private static string EscapeLuaString(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("\r\n", "\\n")
                .Replace("\n", "\\n")
                .Replace("\r", "\\n")
                .Replace("\t", "\\t")
                .Replace("\"", "\\\"")
                .Replace("%", "%%");
        }

        private static string NormalizeImagePath(string value)
        {
            return (value ?? string.Empty).Trim().Replace('/', '\\');
        }

        private static string DecompileLuaFile(string sourcePath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            {
                return string.Empty;
            }

            string unluacJar = FindBundledUnluacJar();
            if (string.IsNullOrWhiteSpace(unluacJar) || !File.Exists(unluacJar))
            {
                return string.Empty;
            }

            string workingDirectory = Path.GetDirectoryName(unluacJar) ?? string.Empty;
            return ExecuteProcessCaptureOutput(
                "java",
                "-jar \"" + unluacJar + "\" \"" + sourcePath + "\"",
                workingDirectory);
        }

        private static string FindBundledUnluacJar()
        {
            string assemblyDirectory = string.Empty;
            try
            {
                assemblyDirectory = Path.GetDirectoryName(typeof(TitleDefinitionCatalog).Assembly.Location) ?? string.Empty;
            }
            catch
            {
                assemblyDirectory = string.Empty;
            }

            string baseDirectory = !string.IsNullOrWhiteSpace(assemblyDirectory)
                ? assemblyDirectory
                : (AppDomain.CurrentDomain.BaseDirectory ?? string.Empty);
            string[] directCandidates = new[]
            {
                Path.Combine(baseDirectory, "tools", "lua", "unluac.jar"),
                Path.Combine(baseDirectory, "unluac.jar")
            };

            for (int i = 0; i < directCandidates.Length; i++)
            {
                if (File.Exists(directCandidates[i]))
                {
                    return directCandidates[i];
                }
            }

            string current = baseDirectory;
            for (int depth = 0; depth < 8 && !string.IsNullOrWhiteSpace(current); depth++)
            {
                string solutionCandidate = Path.Combine(current, "FWEledit.sln");
                if (File.Exists(solutionCandidate))
                {
                    string repoCandidate = Path.Combine(current, "tools", "lua", "unluac.jar");
                    if (File.Exists(repoCandidate))
                    {
                        return repoCandidate;
                    }
                }

                DirectoryInfo parentInfo = Directory.GetParent(current);
                current = parentInfo != null ? parentInfo.FullName : string.Empty;
            }

            return string.Empty;
        }

        private static bool CompileLuaFile(string sourcePath, string outputPath, out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            {
                error = "The Lua source file was not found.";
                return false;
            }

            string luacPath = FindBundledLuacExecutable();
            if (string.IsNullOrWhiteSpace(luacPath) || !File.Exists(luacPath))
            {
                error = "luac.exe was not found.";
                return false;
            }

            string workingDirectory = Path.GetDirectoryName(luacPath) ?? string.Empty;
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = luacPath,
                Arguments = "-o \"" + outputPath + "\" \"" + sourcePath + "\"",
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            try
            {
                using (Process process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        error = "Unable to start luac.exe.";
                        return false;
                    }

                    string output = process.StandardOutput.ReadToEnd();
                    string stdError = process.StandardError.ReadToEnd();
                    process.WaitForExit(30000);
                    if (!process.HasExited)
                    {
                        try
                        {
                            process.Kill();
                        }
                        catch
                        {
                        }

                        error = "luac.exe timed out.";
                        return false;
                    }

                    if (process.ExitCode != 0)
                    {
                        error = string.IsNullOrWhiteSpace(stdError)
                            ? (string.IsNullOrWhiteSpace(output) ? "luac.exe failed." : output.Trim())
                            : stdError.Trim();
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }

            return File.Exists(outputPath);
        }

        private static string FindBundledLuacExecutable()
        {
            string assemblyDirectory = string.Empty;
            try
            {
                assemblyDirectory = Path.GetDirectoryName(typeof(TitleDefinitionCatalog).Assembly.Location) ?? string.Empty;
            }
            catch
            {
                assemblyDirectory = string.Empty;
            }

            string baseDirectory = !string.IsNullOrWhiteSpace(assemblyDirectory)
                ? assemblyDirectory
                : (AppDomain.CurrentDomain.BaseDirectory ?? string.Empty);
            string[] directCandidates = new[]
            {
                Path.Combine(baseDirectory, "tools", "lua", "luac.exe"),
                Path.Combine(baseDirectory, "luac.exe")
            };

            for (int i = 0; i < directCandidates.Length; i++)
            {
                if (File.Exists(directCandidates[i]))
                {
                    return directCandidates[i];
                }
            }

            string current = baseDirectory;
            for (int depth = 0; depth < 8 && !string.IsNullOrWhiteSpace(current); depth++)
            {
                string solutionCandidate = Path.Combine(current, "FWEledit.sln");
                if (File.Exists(solutionCandidate))
                {
                    string repoCandidate = Path.Combine(current, "tools", "lua", "luac.exe");
                    if (File.Exists(repoCandidate))
                    {
                        return repoCandidate;
                    }
                }

                DirectoryInfo parentInfo = Directory.GetParent(current);
                current = parentInfo != null ? parentInfo.FullName : string.Empty;
            }

            return string.Empty;
        }

        private static string ExecuteProcessCaptureOutput(string fileName, string arguments, string workingDirectory)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory) ? string.Empty : workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            try
            {
                using (Process process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        return string.Empty;
                    }

                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(30000);
                    if (!process.HasExited || process.ExitCode != 0)
                    {
                        return string.Empty;
                    }

                    return DecodeLuaEscapedUtf8(output);
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string CleanScriptText(string text)
        {
            string cleaned = DecodeLuaEscapedUtf8(text ?? string.Empty);
            try
            {
                cleaned = Extensions.ColorClean(cleaned);
            }
            catch
            {
            }

            cleaned = cleaned
                .Replace("\\r", Environment.NewLine)
                .Replace("\\n", Environment.NewLine)
                .Replace("\\t", "\t")
                .Replace("\\\"", "\"")
                .Replace("%%", "%")
                .Replace("\r", Environment.NewLine)
                .Trim();

            while (cleaned.StartsWith("%s", StringComparison.Ordinal))
            {
                cleaned = cleaned.Substring(2).TrimStart();
            }

            return cleaned;
        }

        private static string DecodeScriptLiteralPreserveFormatting(string text)
        {
            string cleaned = DecodeLuaEscapedUtf8(text ?? string.Empty);
            cleaned = cleaned
                .Replace("\\r", Environment.NewLine)
                .Replace("\\n", Environment.NewLine)
                .Replace("\\t", "\t")
                .Replace("\\\"", "\"")
                .Replace("%%", "%")
                .Replace("\r", Environment.NewLine);

            return cleaned.Trim();
        }

        private static string DecodeLuaEscapedUtf8(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            return Regex.Replace(
                text,
                "(?:\\\\\\d{1,3})+",
                delegate (Match match)
                {
                    try
                    {
                        MatchCollection digits = Regex.Matches(match.Value, "\\\\(\\d{1,3})");
                        byte[] bytes = new byte[digits.Count];
                        for (int i = 0; i < digits.Count; i++)
                        {
                            bytes[i] = byte.Parse(digits[i].Groups[1].Value, CultureInfo.InvariantCulture);
                        }

                        return Encoding.UTF8.GetString(bytes);
                    }
                    catch
                    {
                        return match.Value;
                    }
                });
        }

        private static List<ItemReferenceOption> CloneOptions(List<ItemReferenceOption> source)
        {
            List<ItemReferenceOption> clone = new List<ItemReferenceOption>();
            if (source == null)
            {
                return clone;
            }

            for (int i = 0; i < source.Count; i++)
            {
                ItemReferenceOption option = source[i];
                if (option == null)
                {
                    continue;
                }

                clone.Add(new ItemReferenceOption
                {
                    ListIndex = option.ListIndex,
                    ElementIndex = option.ElementIndex,
                    Id = option.Id,
                    Name = option.Name,
                    ListName = option.ListName,
                    IconKey = option.IconKey,
                    Quality = option.Quality,
                    Description = option.Description,
                    SecondaryText = option.SecondaryText,
                    AccentHex = option.AccentHex,
                    Kind = option.Kind
                });
            }

            return clone;
        }

        private static EditableTitleDefinition CloneDefinition(EditableTitleDefinition definition)
        {
            if (definition == null)
            {
                return null;
            }

            string[] addonDescriptions = definition.AddonDescriptions != null
                ? (string[])definition.AddonDescriptions.Clone()
                : new string[5];
            if (addonDescriptions.Length < 5)
            {
                Array.Resize(ref addonDescriptions, 5);
            }

            return new EditableTitleDefinition
            {
                Id = definition.Id,
                TitleText = definition.TitleText,
                AccentHex = definition.AccentHex,
                Description = definition.Description,
                AddonDescriptions = addonDescriptions,
                IconPath = definition.IconPath,
                IsGraphicTitle = definition.IsGraphicTitle
            };
        }
    }
}
