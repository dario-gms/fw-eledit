using System;
using System.Collections.Generic;
using System.IO;

namespace FWEledit
{
    public sealed class ReplaceMappingFileService
    {
        public Dictionary<string, string> LoadSimpleMapping(string filePath)
        {
            Dictionary<string, string> map = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return map;
            }

            foreach (string rawLine in File.ReadAllLines(filePath))
            {
                string line = rawLine ?? string.Empty;
                if (line.StartsWith("#") || line.Length == 0)
                {
                    continue;
                }
                int splitIndex = line.IndexOf('=');
                if (splitIndex <= 0 || splitIndex >= line.Length - 1)
                {
                    continue;
                }

                string key = line.Substring(0, splitIndex).Trim();
                string value = line.Substring(splitIndex + 1).Trim();
                if (key.Length == 0)
                {
                    continue;
                }
                map[key] = value;
            }

            return map;
        }

        public Dictionary<string, string[]> LoadTomeMapping(string filePath)
        {
            Dictionary<string, string[]> map = new Dictionary<string, string[]>();
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return map;
            }

            foreach (string rawLine in File.ReadAllLines(filePath))
            {
                string line = rawLine ?? string.Empty;
                if (line.StartsWith("#") || line.Length == 0)
                {
                    continue;
                }
                int splitIndex = line.IndexOf('=');
                if (splitIndex <= 0 || splitIndex >= line.Length - 1)
                {
                    continue;
                }

                string key = line.Substring(0, splitIndex).Trim();
                string value = line.Substring(splitIndex + 1).Trim();
                if (key.Length == 0)
                {
                    continue;
                }
                string[] tokens = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < tokens.Length; i++)
                {
                    tokens[i] = tokens[i].Trim();
                }
                map[key] = tokens;
            }

            return map;
        }
    }
}
