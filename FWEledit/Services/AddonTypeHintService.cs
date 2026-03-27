using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FWEledit
{
    public sealed class AddonTypeHintService
    {
        public Dictionary<int, string> LoadHints(string appPath, string gameRootPath)
        {
            Dictionary<int, string> hints = new Dictionary<int, string>();
            try
            {
                List<string> candidates = new List<string>();
                if (!string.IsNullOrWhiteSpace(appPath))
                {
                    candidates.Add(Path.Combine(appPath, "resources", "data", "addon_table_en.txt"));
                    candidates.Add(Path.Combine(appPath, "resources", "data", "addon_table_pt.txt"));
                    candidates.Add(Path.Combine(appPath, "resources", "data", "addon_table.txt"));
                }
                if (!string.IsNullOrWhiteSpace(gameRootPath))
                {
                    candidates.Add(Path.Combine(gameRootPath, "data", "addon_table_en.txt"));
                    candidates.Add(Path.Combine(gameRootPath, "data", "addon_table_pt.txt"));
                    candidates.Add(Path.Combine(gameRootPath, "data", "addon_table.txt"));
                }

                string path = candidates.FirstOrDefault(p => File.Exists(p)) ?? string.Empty;
                if (string.IsNullOrWhiteSpace(path))
                {
                    return hints;
                }

                string fileName = Path.GetFileName(path) ?? string.Empty;
                bool allowCjk = fileName.IndexOf("_en", StringComparison.OrdinalIgnoreCase) >= 0
                    || fileName.IndexOf("_pt", StringComparison.OrdinalIgnoreCase) >= 0;

                using (StreamReader sr = new StreamReader(path, Encoding.UTF8, true))
                {
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            continue;
                        }
                        string trimmed = line.Trim();
                        if (!trimmed.StartsWith("//type:", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        string payload = trimmed.Substring(7).Trim();
                        if (string.IsNullOrWhiteSpace(payload))
                        {
                            continue;
                        }

                        string[] parts = payload.Split(new char[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length == 0)
                        {
                            continue;
                        }
                        int typeId;
                        if (!int.TryParse(parts[0], out typeId) || typeId < 0)
                        {
                            continue;
                        }
                        string desc = parts.Length > 1 ? parts[1].Trim() : string.Empty;
                        if (!allowCjk && ContainsCjk(desc))
                        {
                            continue;
                        }
                        if (!hints.ContainsKey(typeId))
                        {
                            hints.Add(typeId, desc);
                        }
                    }
                }
            }
            catch
            {
            }

            return hints;
        }

        private static bool ContainsCjk(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            for (int i = 0; i < value.Length; i++)
            {
                int code = value[i];
                if ((code >= 0x4E00 && code <= 0x9FFF)
                    || (code >= 0x3400 && code <= 0x4DBF)
                    || (code >= 0xF900 && code <= 0xFAFF))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
