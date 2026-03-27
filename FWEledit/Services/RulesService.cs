using System;
using System.IO;
using System.Text;

namespace FWEledit
{
    public sealed class RulesService
    {
        public eListCollection LoadCollection(string filePath, ref ColorProgressBar.ColorProgressBar progressBar)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("Invalid file path.", nameof(filePath));
            }
            return new eListCollection(filePath, ref progressBar);
        }

        public RuleConfig[] InitializeRules(eListCollection recent)
        {
            if (recent == null)
            {
                return new RuleConfig[0];
            }

            RuleConfig[] rules = new RuleConfig[recent.Lists.Length];
            for (int l = 0; l < recent.Lists.Length; l++)
            {
                rules[l] = new RuleConfig
                {
                    RemoveList = false,
                    ReplaceOffset = false,
                    Offset = string.Empty,
                    RemoveValues = new bool[recent.Lists[l].elementFields.Length]
                };
            }
            return rules;
        }

        public int CountMismatches(eListCollection baseCollection, eListCollection recentCollection, RuleConfig[] rules, int listIndexBase, int listIndexRecent, int baseFieldIndex, int recentFieldIndex)
        {
            if (baseCollection == null || recentCollection == null || rules == null)
            {
                return -1;
            }
            if (rules[listIndexRecent].RemoveValues[recentFieldIndex])
            {
                return -1;
            }

            if (baseFieldIndex >= baseCollection.Lists[listIndexBase].elementFields.Length)
            {
                return -1;
            }
            if (recentFieldIndex >= recentCollection.Lists[listIndexRecent].elementFields.Length)
            {
                return -1;
            }

            int mismatches = 0;
            for (int e = 0; e < recentCollection.Lists[listIndexRecent].elementValues.Length; e++)
            {
                if (e >= baseCollection.Lists[listIndexBase].elementValues.Length || e >= recentCollection.Lists[listIndexRecent].elementValues.Length)
                {
                    break;
                }

                if (baseCollection.GetValue(listIndexBase, e, baseFieldIndex) != recentCollection.GetValue(listIndexRecent, e, recentFieldIndex))
                {
                    mismatches++;
                }
            }

            return mismatches;
        }

        public string BuildRulesMessage(eListCollection baseCollection, eListCollection recentCollection, RuleConfig[] rules)
        {
            if (baseCollection == null || recentCollection == null || rules == null)
            {
                return string.Empty;
            }

            StringBuilder message = new StringBuilder();
            message.AppendLine("##############################");
            message.AppendLine("#### RULES FOR v" + recentCollection.Version + " -> v" + baseCollection.Version + " ####");
            message.AppendLine("##############################");
            message.AppendLine();
            message.AppendLine("SETVERSION|" + baseCollection.Version);
            message.AppendLine("SETSIGNATURE|" + baseCollection.Signature);
            message.AppendLine("SETCONVERSATIONLISTINDEX|" + baseCollection.ConversationListIndex);
            message.AppendLine();

            for (int l = 0; l < rules.Length; l++)
            {
                if (rules[l].RemoveList)
                {
                    message.AppendLine("REMOVELIST:" + l);
                }
            }

            message.AppendLine();
            for (int l = 0; l < rules.Length; l++)
            {
                if (rules[l].ReplaceOffset)
                {
                    message.AppendLine("REPLACEOFFSET:" + l + "|" + rules[l].Offset);
                }
            }

            bool breakLine = true;
            for (int l = 0; l < rules.Length; l++)
            {
                if (breakLine)
                {
                    message.AppendLine();
                    breakLine = false;
                }
                for (int f = 0; f < rules[l].RemoveValues.Length; f++)
                {
                    if (rules[l].RemoveValues[f])
                    {
                        message.AppendLine("REMOVEVALUE:" + l + ":" + f);
                        breakLine = true;
                    }
                }
            }

            return message.ToString();
        }

        public void ImportRules(string filePath, eListCollection baseCollection, RuleConfig[] rules)
        {
            if (rules == null || baseCollection == null)
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return;
            }

            using (StreamReader sr = new StreamReader(filePath))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line != "" && !line.StartsWith("#"))
                    {
                        if (line.StartsWith("REMOVELIST:"))
                        {
                            string[] values = line.Split(new char[] { ':' });
                            rules[Convert.ToInt32(values[1])].RemoveList = true;
                        }
                        if (line.StartsWith("REPLACEOFFSET:"))
                        {
                            string[] values = line.Split(new char[] { ':', '|' });
                            int l = Convert.ToInt32(values[1]);
                            rules[l].ReplaceOffset = true;
                            rules[l].Offset = baseCollection.GetOffset(l);
                        }
                        if (line.StartsWith("REMOVEVALUE:"))
                        {
                            string[] values = line.Split(new char[] { ':' });
                            rules[Convert.ToInt32(values[1])].RemoveValues[Convert.ToInt32(values[2])] = true;
                        }
                    }
                }
            }
        }

        public void ExportRules(string filePath, eListCollection baseCollection, eListCollection recentCollection, RuleConfig[] rules)
        {
            if (string.IsNullOrWhiteSpace(filePath) || baseCollection == null || recentCollection == null || rules == null)
            {
                return;
            }

            using (StreamWriter sw = new StreamWriter(filePath))
            {
                sw.WriteLine("##############################");
                sw.WriteLine("#### RULES FOR v" + recentCollection.Version + " -> v" + baseCollection.Version + " ####");
                sw.WriteLine("##############################");
                sw.WriteLine();
                sw.WriteLine("SETVERSION|" + baseCollection.Version);
                sw.WriteLine("SETSIGNATURE|" + baseCollection.Signature);
                sw.WriteLine("SETCONVERSATIONLISTINDEX|" + baseCollection.ConversationListIndex);
                sw.WriteLine();

                for (int l = 0; l < rules.Length; l++)
                {
                    if (rules[l].RemoveList)
                    {
                        sw.WriteLine("REMOVELIST:" + l);
                    }
                }

                sw.WriteLine();
                for (int l = 0; l < rules.Length; l++)
                {
                    if (rules[l].ReplaceOffset)
                    {
                        sw.WriteLine("REPLACEOFFSET:" + l + "|" + rules[l].Offset);
                    }
                }

                bool breakLine = true;
                for (int l = 0; l < rules.Length; l++)
                {
                    if (breakLine)
                    {
                        sw.WriteLine();
                        breakLine = false;
                    }
                    for (int f = 0; f < rules[l].RemoveValues.Length; f++)
                    {
                        if (rules[l].RemoveValues[f])
                        {
                            sw.WriteLine("REMOVEVALUE:" + l + ":" + f);
                            breakLine = true;
                        }
                    }
                }
            }
        }
    }
}
