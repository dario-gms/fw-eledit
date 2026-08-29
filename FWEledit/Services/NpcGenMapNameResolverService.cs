using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace FWEledit
{
    public sealed class NpcGenMapNameResolverService
    {
        private readonly PckEntryReaderService pckReader = new PckEntryReaderService();
        private readonly Dictionary<string, Dictionary<string, string>> cacheByGameRoot =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        public string ResolveDisplayName(string npcGenFilePath, string technicalMapName)
        {
            if (string.IsNullOrWhiteSpace(technicalMapName))
            {
                return string.Empty;
            }

            string realName = ResolveRealName(npcGenFilePath, technicalMapName);
            if (string.IsNullOrWhiteSpace(realName)
                || string.Equals(realName, technicalMapName, StringComparison.OrdinalIgnoreCase))
            {
                return technicalMapName;
            }

            return realName + " (" + technicalMapName + ")";
        }

        private string ResolveRealName(string npcGenFilePath, string technicalMapName)
        {
            List<string> roots = BuildGameRootCandidates(npcGenFilePath);
            for (int i = 0; i < roots.Count; i++)
            {
                Dictionary<string, string> namesByPath = LoadNamesByPath(roots[i]);
                string realName;
                string exactPath = NormalizeMapPath(technicalMapName, true);
                if (namesByPath != null
                    && !string.IsNullOrWhiteSpace(exactPath)
                    && namesByPath.TryGetValue(exactPath, out realName))
                {
                    return realName;
                }

                string normalizedPath = NormalizeMapPath(technicalMapName, false);
                if (namesByPath != null
                    && !string.IsNullOrWhiteSpace(normalizedPath)
                    && namesByPath.TryGetValue(normalizedPath, out realName))
                {
                    return realName;
                }
            }

            return string.Empty;
        }

        private Dictionary<string, string> LoadNamesByPath(string gameRoot)
        {
            if (string.IsNullOrWhiteSpace(gameRoot) || !Directory.Exists(gameRoot))
            {
                return null;
            }

            string canonicalRoot;
            try
            {
                canonicalRoot = Path.GetFullPath(gameRoot.Trim()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch
            {
                canonicalRoot = gameRoot.Trim();
            }

            Dictionary<string, string> cached;
            if (cacheByGameRoot.TryGetValue(canonicalRoot, out cached))
            {
                return cached;
            }

            Dictionary<string, string> namesByPath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string> stringValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            string[] scriptNames =
            {
                "InstanceStr.lua",
                "InstanceWorld.lua",
                "InstanceBattle.lua",
                "InstanceDynamic.lua"
            };

            for (int i = 0; i < scriptNames.Length; i++)
            {
                string text;
                if (TryReadInstanceScript(canonicalRoot, scriptNames[i], out text))
                {
                    ParseStringAssignments(text, stringValues);
                }
            }

            for (int i = 0; i < scriptNames.Length; i++)
            {
                string text;
                if (TryReadInstanceScript(canonicalRoot, scriptNames[i], out text))
                {
                    ParseBytecodePrecinctNames(text, namesByPath);
                    ParseBytecodeMapPaths(text, stringValues, namesByPath);
                    ParseInstanceDefinitions(text, stringValues, namesByPath);
                }
            }

            AddDerivedInstanceMappings(stringValues, namesByPath);

            cacheByGameRoot[canonicalRoot] = namesByPath;
            return namesByPath;
        }

        private bool TryReadInstanceScript(string gameRoot, string scriptName, out string text)
        {
            text = string.Empty;

            string[] looseCandidates =
            {
                Path.Combine(gameRoot, "script", scriptName),
                Path.Combine(gameRoot, "data", "script", scriptName),
                Path.Combine(gameRoot, "resources", "script", scriptName)
            };

            for (int i = 0; i < looseCandidates.Length; i++)
            {
                if (File.Exists(looseCandidates[i]))
                {
                    byte[] payload = File.ReadAllBytes(looseCandidates[i]);
                    text = DecodeText(payload);
                    return !string.IsNullOrWhiteSpace(text);
                }
            }

            string previousGameRoot = AssetManager.GameRootPath;
            try
            {
                AssetManager.GameRootPath = gameRoot;
                string[] packagedCandidates =
                {
                    Path.Combine("script", scriptName),
                    Path.Combine("data", "script", scriptName),
                    scriptName
                };

                for (int i = 0; i < packagedCandidates.Length; i++)
                {
                    if (TryReadPackagedScript("configs", packagedCandidates[i], out text)
                        || TryReadPackagedScript("script", packagedCandidates[i], out text))
                    {
                        return !string.IsNullOrWhiteSpace(text);
                    }
                }
            }
            catch
            {
            }
            finally
            {
                AssetManager.GameRootPath = previousGameRoot;
            }

            return false;
        }

        private bool TryReadPackagedScript(string packageName, string relativePath, out string text)
        {
            text = string.Empty;
            byte[] payload;
            string resolvedRelativePath;
            string error;
            if (pckReader.TryReadFileFast(packageName, relativePath, out payload, out resolvedRelativePath, out error)
                && payload != null
                && payload.Length > 0)
            {
                text = DecodeText(payload);
                return !string.IsNullOrWhiteSpace(text);
            }

            return false;
        }

        private static void ParseStringAssignments(string text, Dictionary<string, string> stringValues)
        {
            if (string.IsNullOrWhiteSpace(text) || stringValues == null)
            {
                return;
            }

            string withoutComments = StripLuaLineComments(text);
            Regex assignmentRegex = new Regex(
                @"(?<key>[A-Za-z_][A-Za-z0-9_]*(?:\s*\[\s*\d+\s*\])?)\s*=\s*(?<quote>[""'])(?<value>(?:\\.|(?!\k<quote>).)*)\k<quote>",
                RegexOptions.Singleline);
            MatchCollection matches = assignmentRegex.Matches(withoutComments);
            for (int i = 0; i < matches.Count; i++)
            {
                string key = NormalizeReference(matches[i].Groups["key"].Value);
                string value = UnescapeLuaString(matches[i].Groups["value"].Value);
                if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
                {
                    stringValues[key] = value.Trim();
                }
            }

            ParseIndexedStringTables(withoutComments, stringValues);
            ParseBytecodeStringAssignments(text, stringValues);
        }

        private static void ParseBytecodeStringAssignments(string text, Dictionary<string, string> stringValues)
        {
            if (stringValues == null || !LooksLikeLuaBytecode(text))
            {
                return;
            }

            List<string> strings = ExtractPrintableStrings(text);
            for (int i = 0; i < strings.Count - 1; i++)
            {
                Match nameMatch = Regex.Match(strings[i], @"^name_(?<id>\d+)$", RegexOptions.IgnoreCase);
                if (!nameMatch.Success)
                {
                    continue;
                }

                string value = strings[i + 1].Trim();
                if (IsBytecodeValueCandidate(value))
                {
                    stringValues[NormalizeReference(strings[i])] = value;
                }
            }
        }

        private static void ParseBytecodeMapPaths(
            string text,
            Dictionary<string, string> stringValues,
            Dictionary<string, string> namesByPath)
        {
            if (stringValues == null || namesByPath == null || !LooksLikeLuaBytecode(text))
            {
                return;
            }

            List<string> strings = ExtractPrintableStrings(text);
            for (int i = 0; i < strings.Count; i++)
            {
                Match pathMatch = Regex.Match(strings[i], @"^[sS](?<id>\d+)$");
                if (!pathMatch.Success)
                {
                    continue;
                }

                string normalizedPath = NormalizeMapPath(strings[i], false);
                string nameKey = "name_" + pathMatch.Groups["id"].Value;
                string name;
                if (!string.IsNullOrWhiteSpace(normalizedPath)
                    && stringValues.TryGetValue(nameKey, out name)
                    && !string.IsNullOrWhiteSpace(name)
                    && !namesByPath.ContainsKey(normalizedPath))
                {
                    namesByPath[normalizedPath] = name.Trim();
                }
            }
        }

        private static void ParseBytecodePrecinctNames(string text, Dictionary<string, string> namesByPath)
        {
            if (namesByPath == null || !LooksLikeLuaBytecode(text))
            {
                return;
            }

            List<string> strings = ExtractPrintableStrings(text);
            Dictionary<string, List<string>> namesByMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < strings.Count - 1; i++)
            {
                Match keyMatch = Regex.Match(strings[i], @"^PrecinctHintText_(?<map>\d+)_", RegexOptions.IgnoreCase);
                if (!keyMatch.Success)
                {
                    continue;
                }

                string name = CleanPrecinctDisplayName(strings[i + 1]);
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                string mapPath = NormalizeMapPath("s" + keyMatch.Groups["map"].Value, false);
                if (string.Equals(mapPath, "s8", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(name, "The Crucible", StringComparison.OrdinalIgnoreCase))
                {
                    mapPath = "s0";
                }

                List<string> names;
                if (!namesByMap.TryGetValue(mapPath, out names))
                {
                    names = new List<string>();
                    namesByMap[mapPath] = names;
                }

                if (!ContainsIgnoreCase(names, name))
                {
                    names.Add(name);
                }
            }

            foreach (KeyValuePair<string, List<string>> entry in namesByMap)
            {
                if (!string.IsNullOrWhiteSpace(entry.Key) && entry.Value.Count > 0 && !namesByPath.ContainsKey(entry.Key))
                {
                    namesByPath[entry.Key] = string.Join(" / ", entry.Value.ToArray());
                }
            }
        }

        private static string CleanPrecinctDisplayName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string cleaned = Regex.Replace(value, @"\^[0-9A-Fa-f]{6}", string.Empty);
            cleaned = Regex.Replace(cleaned, @"\^O\d+", string.Empty);
            cleaned = cleaned.Replace("\0", string.Empty).Trim();
            if (cleaned.Length == 0
                || !LooksLikeReadablePlaceName(cleaned)
                || cleaned.IndexOf("Suitable Level", StringComparison.OrdinalIgnoreCase) >= 0
                || cleaned.IndexOf("Instance Entrance", StringComparison.OrdinalIgnoreCase) >= 0
                || cleaned.IndexOf("Job:", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return string.Empty;
            }

            return cleaned;
        }

        private static bool LooksLikeReadablePlaceName(string value)
        {
            int letters = 0;
            int badSymbols = 0;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (char.IsLetter(c))
                {
                    letters++;
                    continue;
                }

                if (!char.IsWhiteSpace(c)
                    && !char.IsDigit(c)
                    && c != '\''
                    && c != '-'
                    && c != ':'
                    && c != ','
                    && c != '.'
                    && c != '('
                    && c != ')')
                {
                    badSymbols++;
                }
            }

            return letters >= 3 && badSymbols == 0;
        }

        private static bool ContainsIgnoreCase(List<string> values, string value)
        {
            for (int i = 0; i < values.Count; i++)
            {
                if (string.Equals(values[i], value, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static List<string> ExtractPrintableStrings(string text)
        {
            List<string> strings = new List<string>();
            MatchCollection matches = Regex.Matches(text ?? string.Empty, @"[\x20-\x7E]{2,}");
            for (int i = 0; i < matches.Count; i++)
            {
                string value = matches[i].Value.Trim();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    strings.Add(value);
                }
            }

            return strings;
        }

        private static bool LooksLikeLuaBytecode(string text)
        {
            return !string.IsNullOrEmpty(text) && text.StartsWith("\x1BLua", StringComparison.Ordinal);
        }

        private static bool IsBytecodeValueCandidate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return !Regex.IsMatch(value, @"^(name_|desc_|NpcMark_|PrecinctHintText_|s\d+$|S\d+$)", RegexOptions.IgnoreCase);
        }

        private static void ParseIndexedStringTables(string text, Dictionary<string, string> stringValues)
        {
            Regex tableRegex = new Regex(
                @"(?<table>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*\{(?<body>.*?)\}",
                RegexOptions.Singleline);
            MatchCollection tableMatches = tableRegex.Matches(text ?? string.Empty);
            for (int i = 0; i < tableMatches.Count; i++)
            {
                string tableName = tableMatches[i].Groups["table"].Value;
                string body = tableMatches[i].Groups["body"].Value;
                if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(body))
                {
                    continue;
                }

                Regex keyedValueRegex = new Regex(
                    @"\[\s*(?<index>\d+)\s*\]\s*=\s*(?<quote>[""'])(?<value>(?:\\.|(?!\k<quote>).)*)\k<quote>",
                    RegexOptions.Singleline);
                MatchCollection keyedMatches = keyedValueRegex.Matches(body);
                for (int j = 0; j < keyedMatches.Count; j++)
                {
                    AddIndexedStringValue(
                        stringValues,
                        tableName,
                        keyedMatches[j].Groups["index"].Value,
                        keyedMatches[j].Groups["value"].Value);
                }

                Regex sequentialValueRegex = new Regex(
                    @"(?:^|[,;\r\n])\s*(?<quote>[""'])(?<value>(?:\\.|(?!\k<quote>).)*)\k<quote>",
                    RegexOptions.Singleline);
                MatchCollection sequentialMatches = sequentialValueRegex.Matches(body);
                for (int j = 0; j < sequentialMatches.Count; j++)
                {
                    AddIndexedStringValue(
                        stringValues,
                        tableName,
                        (j + 1).ToString(),
                        sequentialMatches[j].Groups["value"].Value);
                }
            }
        }

        private static void AddIndexedStringValue(
            Dictionary<string, string> stringValues,
            string tableName,
            string index,
            string rawValue)
        {
            string value = UnescapeLuaString(rawValue).Trim();
            if (string.IsNullOrWhiteSpace(tableName)
                || string.IsNullOrWhiteSpace(index)
                || string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            stringValues[NormalizeReference(tableName + "[" + index + "]")] = value;
        }

        private static void ParseInstanceDefinitions(
            string text,
            Dictionary<string, string> stringValues,
            Dictionary<string, string> namesByPath)
        {
            if (string.IsNullOrWhiteSpace(text) || namesByPath == null)
            {
                return;
            }

            string withoutComments = StripLuaLineComments(text);
            Regex instanceRegex = new Regex(
                @"(?:Instance\s*)?\[\s*\d+\s*\]\s*=\s*\{(?<body>.*?)\}",
                RegexOptions.Singleline);
            MatchCollection matches = instanceRegex.Matches(withoutComments);
            for (int i = 0; i < matches.Count; i++)
            {
                string body = matches[i].Groups["body"].Value;
                string path = ExtractLuaFieldValue(body, "path", stringValues);
                string name = ExtractLuaFieldValue(body, "name", stringValues);
                if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                AddMapName(namesByPath, path, name.Trim());
            }
        }

        private static string ExtractLuaFieldValue(string body, string fieldName, Dictionary<string, string> stringValues)
        {
            Regex regex = new Regex(
                @"(?:^|[,;\r\n])\s*" + Regex.Escape(fieldName) + @"\s*=\s*(?:(?<quote>[""'])(?<literal>(?:\\.|(?!\k<quote>).)*)\k<quote>|(?<ref>[A-Za-z_][A-Za-z0-9_]*(?:\s*\[\s*\d+\s*\])?))",
                RegexOptions.Singleline);
            Match match = regex.Match(body ?? string.Empty);
            if (!match.Success)
            {
                return string.Empty;
            }

            if (match.Groups["literal"].Success)
            {
                return UnescapeLuaString(match.Groups["literal"].Value).Trim();
            }

            string reference = NormalizeReference(match.Groups["ref"].Value);
            string value;
            return stringValues != null && stringValues.TryGetValue(reference, out value) ? value.Trim() : reference;
        }

        private static void AddDerivedInstanceMappings(
            Dictionary<string, string> stringValues,
            Dictionary<string, string> namesByPath)
        {
            if (stringValues == null || namesByPath == null)
            {
                return;
            }

            for (int i = 1; i <= 99; i++)
            {
                AddNameById(namesByPath, stringValues, "i" + i, 200 + i);
            }

            for (int i = 1; i <= 99; i++)
            {
                AddNameById(namesByPath, stringValues, "d" + i, 215 + i);
            }

            AddSpecialInstanceNameMappings(namesByPath, stringValues);
            AddNameById(namesByPath, stringValues, "k0", 200);
            AddNameById(namesByPath, stringValues, "k3", 235);

            for (int i = 1; i <= 6; i++)
            {
                AddNameById(namesByPath, stringValues, "r" + i, 249 + i);
            }

            AddMapName(namesByPath, "r2", LookupNameById(stringValues, 251, "Dysil's Crux"));
            AddMapName(namesByPath, "r3", LookupNameById(stringValues, 252, "Dysil's Crux"));
            AddMapName(namesByPath, "r5", LookupNameById(stringValues, 254, "Summit of Elements"));
            AddMapName(namesByPath, "r6", LookupNameById(stringValues, 255, "Summit of Elements"));
        }

        private static void AddSpecialInstanceNameMappings(
            Dictionary<string, string> namesByPath,
            Dictionary<string, string> stringValues)
        {
            AddNameById(namesByPath, stringValues, "a1", 210);
            AddNameById(namesByPath, stringValues, "a2", 211);
            AddNameById(namesByPath, stringValues, "a3", 212);
            AddNameById(namesByPath, stringValues, "a5", 214);
            AddNameById(namesByPath, stringValues, "a6", 215);
            AddNameById(namesByPath, stringValues, "a7", 222);
            AddNameById(namesByPath, stringValues, "a8", 224);
            AddNameById(namesByPath, stringValues, "a9", 225);
            AddNameById(namesByPath, stringValues, "a10", 226);
            AddNameById(namesByPath, stringValues, "a14", 231);
            AddNameById(namesByPath, stringValues, "a19", 237);
            AddNameById(namesByPath, stringValues, "a21", 242);
        }

        private static string LookupNameById(Dictionary<string, string> stringValues, int nameId, string fallback)
        {
            string name;
            return stringValues != null
                && stringValues.TryGetValue("name_" + nameId.ToString(), out name)
                && !string.IsNullOrWhiteSpace(name)
                ? name.Trim()
                : fallback;
        }

        private static void AddNameById(
            Dictionary<string, string> namesByPath,
            Dictionary<string, string> stringValues,
            string path,
            int nameId)
        {
            string normalizedPath = NormalizeMapPath(path, false);
            string name;
            if (!string.IsNullOrWhiteSpace(normalizedPath)
                && !namesByPath.ContainsKey(normalizedPath)
                && stringValues.TryGetValue("name_" + nameId.ToString(), out name)
                && !string.IsNullOrWhiteSpace(name))
            {
                namesByPath[normalizedPath] = name.Trim();
            }
        }

        private static void AddMapName(Dictionary<string, string> namesByPath, string path, string name)
        {
            if (namesByPath == null || string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            string exactPath = NormalizeMapPath(path, true);
            if (!string.IsNullOrWhiteSpace(exactPath) && !namesByPath.ContainsKey(exactPath))
            {
                namesByPath[exactPath] = name.Trim();
            }

            string normalizedPath = NormalizeMapPath(path, false);
            if (!string.IsNullOrWhiteSpace(normalizedPath) && !namesByPath.ContainsKey(normalizedPath))
            {
                namesByPath[normalizedPath] = name.Trim();
            }
        }

        private static List<string> BuildGameRootCandidates(string npcGenFilePath)
        {
            List<string> roots = new List<string>();
            AddRoot(roots, AssetManager.GameRootPath);

            try
            {
                string directory = Path.GetDirectoryName(npcGenFilePath);
                while (!string.IsNullOrWhiteSpace(directory))
                {
                    if (HasClientMapNameResources(directory))
                    {
                        AddRoot(roots, directory);
                    }

                    AddChildRoots(roots, directory);

                    DirectoryInfo parent = Directory.GetParent(directory);
                    directory = parent != null ? parent.FullName : string.Empty;
                }
            }
            catch
            {
            }

            return roots;
        }

        private static void AddChildRoots(List<string> roots, string directory)
        {
            try
            {
                DirectoryInfo parent = new DirectoryInfo(directory);
                DirectoryInfo[] children = parent.Exists ? parent.GetDirectories() : new DirectoryInfo[0];
                for (int i = 0; i < children.Length; i++)
                {
                    if (HasClientMapNameResources(children[i].FullName))
                    {
                        AddRoot(roots, children[i].FullName);
                    }
                }
            }
            catch
            {
            }
        }

        private static bool HasClientMapNameResources(string root)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                return false;
            }

            return File.Exists(Path.Combine(root, "resources", "script.pck"))
                || File.Exists(Path.Combine(root, "resources", "configs.pck"))
                || File.Exists(Path.Combine(root, "script", "InstanceWorld.lua"));
        }

        private static void AddRoot(List<string> roots, string root)
        {
            if (roots == null || string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            {
                return;
            }

            string canonical;
            try
            {
                canonical = Path.GetFullPath(root.Trim()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch
            {
                canonical = root.Trim();
            }

            for (int i = 0; i < roots.Count; i++)
            {
                if (string.Equals(roots[i], canonical, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            roots.Add(canonical);
        }

        private static string DecodeText(byte[] payload)
        {
            if (payload == null || payload.Length == 0)
            {
                return string.Empty;
            }

            if (payload.Length >= 2 && payload[0] == 0xFF && payload[1] == 0xFE)
            {
                return Encoding.Unicode.GetString(payload);
            }

            if (payload.Length >= 3 && payload[0] == 0xEF && payload[1] == 0xBB && payload[2] == 0xBF)
            {
                return Encoding.UTF8.GetString(payload);
            }

            if (payload.Length >= 4 && payload[0] == 0x1B && payload[1] == 0x4C && payload[2] == 0x75 && payload[3] == 0x61)
            {
                return Encoding.GetEncoding(28591).GetString(payload);
            }

            return Encoding.GetEncoding(936).GetString(payload);
        }

        private static string NormalizeMapPath(string path, bool preserveNumericPadding)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            string normalized = path.Trim().Trim('"', '\'')
                .Replace('/', '\\')
                .Trim('\\');
            if (normalized.StartsWith("maps\\", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(5);
            }

            int separator = normalized.IndexOf('\\');
            normalized = separator >= 0 ? normalized.Substring(0, separator) : normalized;

            Match match = Regex.Match(normalized, @"^(?<prefix>[A-Za-z]+)(?<number>\d+)$");
            if (match.Success)
            {
                if (preserveNumericPadding)
                {
                    return match.Groups["prefix"].Value.ToLowerInvariant()
                        + match.Groups["number"].Value.ToLowerInvariant();
                }

                return match.Groups["prefix"].Value.ToLowerInvariant()
                    + int.Parse(match.Groups["number"].Value).ToString();
            }

            return normalized.ToLowerInvariant();
        }

        private static string NormalizeReference(string reference)
        {
            return Regex.Replace(reference ?? string.Empty, @"\s+", string.Empty);
        }

        private static string StripLuaLineComments(string text)
        {
            return Regex.Replace(text ?? string.Empty, @"--[^\r\n]*", string.Empty);
        }

        private static string UnescapeLuaString(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\\"", "\"")
                .Replace("\\'", "'")
                .Replace("\\\\", "\\")
                .Replace("\\n", "\n")
                .Replace("\\t", "\t");
        }
    }
}
