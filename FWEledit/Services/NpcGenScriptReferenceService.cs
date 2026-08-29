using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace FWEledit
{
    public sealed class NpcGenScriptReferenceInfo
    {
        public string Target { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public int LineNumber { get; set; }
        public string Hint { get; set; }
        public string Code { get; set; }
    }

    public sealed class NpcGenScriptReferenceService
    {
        private static readonly Regex SpawnerCallRegex = new Regex(@"(?:GlobalActiveSpawner|ActiveSpawner)\s*\(", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex RealmConditionRegex = new Regex(@"(?:serverid|zoneid)\s*={2}\s*(\d+)|GetZoneID\s*\(", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public List<NpcGenScriptReferenceInfo> FindReferences(string npcGenFilePath, int triggerId, IEnumerable<int> entityIds)
        {
            List<NpcGenScriptReferenceInfo> references = new List<NpcGenScriptReferenceInfo>();
            string scriptDirectory = FindScriptDirectory(npcGenFilePath);
            if (string.IsNullOrWhiteSpace(scriptDirectory) || !Directory.Exists(scriptDirectory))
            {
                return references;
            }

            string triggerText = triggerId.ToString(System.Globalization.CultureInfo.InvariantCulture);
            HashSet<int> entitySet = new HashSet<int>((entityIds ?? Enumerable.Empty<int>()).Where(id => id > 0));
            foreach (string filePath in Directory.GetFiles(scriptDirectory, "*.lua", SearchOption.TopDirectoryOnly))
            {
                string[] lines;
                try
                {
                    lines = File.ReadAllLines(filePath);
                }
                catch
                {
                    continue;
                }

                HashSet<string> triggerSymbols = FindSymbolsAssignedToValue(lines, triggerText);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    string trimmed = line.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed))
                    {
                        continue;
                    }

                    if (LineReferencesTrigger(trimmed, triggerText, triggerSymbols))
                    {
                        references.Add(CreateReference("Trigger " + triggerText, filePath, i, lines));
                    }

                    foreach (int entityId in entitySet)
                    {
                        string entityText = entityId.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        if (ContainsWholeNumber(trimmed, entityText))
                        {
                            references.Add(CreateReference("Entity " + entityText, filePath, i, lines));
                        }
                    }
                }
            }

            return references
                .GroupBy(item => item.FilePath + "|" + item.LineNumber + "|" + item.Target)
                .Select(group => group.First())
                .OrderBy(item => item.FileName)
                .ThenBy(item => item.LineNumber)
                .ToList();
        }

        private static bool LineReferencesTrigger(string line, string triggerText, HashSet<string> triggerSymbols)
        {
            if (ContainsWholeNumber(line, triggerText))
            {
                return true;
            }

            if (!SpawnerCallRegex.IsMatch(line))
            {
                return false;
            }

            return triggerSymbols.Any(symbol => line.IndexOf(symbol, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static HashSet<string> FindSymbolsAssignedToValue(string[] lines, string value)
        {
            HashSet<string> symbols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Regex tableEntry = new Regex(@"^\s*\[(?:\d+|'[^']+'|""[^""]+"")\]\s*=\s*" + Regex.Escape(value) + @"\b", RegexOptions.Compiled);
            Regex variableAssign = new Regex(@"^\s*(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*" + Regex.Escape(value) + @"\b", RegexOptions.Compiled);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                Match variable = variableAssign.Match(line);
                if (variable.Success)
                {
                    symbols.Add(variable.Groups["name"].Value);
                    continue;
                }

                if (!tableEntry.IsMatch(line))
                {
                    continue;
                }

                string tableName = FindNearestTableName(lines, i);
                if (!string.IsNullOrWhiteSpace(tableName))
                {
                    symbols.Add(tableName);
                }
            }

            return symbols;
        }

        private static string FindNearestTableName(string[] lines, int lineIndex)
        {
            Regex tableStart = new Regex(@"^\s*(?:local\s+)?(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*\{", RegexOptions.Compiled);
            for (int i = lineIndex; i >= 0 && i >= lineIndex - 40; i--)
            {
                Match match = tableStart.Match(lines[i]);
                if (match.Success)
                {
                    return match.Groups["name"].Value;
                }
            }

            return string.Empty;
        }

        private static NpcGenScriptReferenceInfo CreateReference(string target, string filePath, int lineIndex, string[] lines)
        {
            return new NpcGenScriptReferenceInfo
            {
                Target = target,
                FileName = Path.GetFileName(filePath),
                FilePath = filePath,
                LineNumber = lineIndex + 1,
                Hint = FindNearbyRealmHint(lines, lineIndex),
                Code = lines[lineIndex].Trim()
            };
        }

        private static string FindNearbyRealmHint(string[] lines, int lineIndex)
        {
            int start = Math.Max(0, lineIndex - 12);
            for (int i = lineIndex; i >= start; i--)
            {
                Match match = RealmConditionRegex.Match(lines[i]);
                if (match.Success)
                {
                    return match.Groups.Count > 1 && match.Groups[1].Success
                        ? "realm condition: " + match.Groups[1].Value
                        : "uses GetZoneID()";
                }
            }

            return string.Empty;
        }

        private static bool ContainsWholeNumber(string text, string number)
        {
            return Regex.IsMatch(text, @"(?<!\d)" + Regex.Escape(number) + @"(?!\d)");
        }

        private static string FindScriptDirectory(string npcGenFilePath)
        {
            if (string.IsNullOrWhiteSpace(npcGenFilePath))
            {
                return string.Empty;
            }

            DirectoryInfo directory = new DirectoryInfo(Path.GetDirectoryName(npcGenFilePath) ?? string.Empty);
            for (DirectoryInfo current = directory; current != null; current = current.Parent)
            {
                string candidate = Path.Combine(current.FullName, "aiscript");
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }
            }

            return string.Empty;
        }
    }
}
