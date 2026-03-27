using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ElementsLoadService
    {
        public string ResolveElementsPath(string gameFolderPath)
        {
            if (string.IsNullOrWhiteSpace(gameFolderPath) || !Directory.Exists(gameFolderPath))
            {
                return string.Empty;
            }

            string elementsFile = Path.Combine(gameFolderPath, "data", "elements.data");
            if (!File.Exists(elementsFile))
            {
                string directElements = Path.Combine(gameFolderPath, "elements.data");
                if (File.Exists(directElements))
                {
                    elementsFile = directElements;
                }
                else
                {
                    try
                    {
                        string[] found = Directory.GetFiles(gameFolderPath, "elements.data", SearchOption.AllDirectories);
                        if (found.Length > 0)
                        {
                            Array.Sort(found, (a, b) => a.Length.CompareTo(b.Length));
                            elementsFile = found[0];
                        }
                    }
                    catch
                    { }
                }
            }

            return File.Exists(elementsFile) ? elementsFile : string.Empty;
        }

        public eListCollection LoadCollection(string elementsPath, ref ColorProgressBar.ColorProgressBar progressBar)
        {
            if (string.IsNullOrWhiteSpace(elementsPath))
            {
                throw new ArgumentException("Invalid elements path.", nameof(elementsPath));
            }

            return new eListCollection(elementsPath, ref progressBar);
        }

        public List<string> LoadExportRuleNames(eListCollection listCollection)
        {
            List<string> options = new List<string>();
            if (listCollection == null || listCollection.ConfigFile == null)
            {
                return options;
            }

            string rulesDir = Path.Combine(Application.StartupPath, "rules");
            if (!Directory.Exists(rulesDir))
            {
                return options;
            }

            string pattern = "PW_v" + listCollection.Version + "*.rules";
            string[] files = Directory.GetFiles(rulesDir, pattern);
            string prefix = rulesDir + Path.DirectorySeparatorChar;
            for (int i = 0; i < files.Length; i++)
            {
                string display = files[i].Replace("=", "=>");
                display = display.Replace(".rules", "");
                display = display.Replace(prefix, "");
                options.Add(display);
            }

            return options;
        }

        public bool TryLoadXrefs(eListCollection listCollection, out string[][] xrefs)
        {
            int listCount = listCollection != null && listCollection.Lists != null ? listCollection.Lists.Length : 0;
            xrefs = new string[listCount][];

            if (listCollection == null || listCollection.ConfigFile == null)
            {
                return false;
            }

            try
            {
                string configDir = Path.GetDirectoryName(listCollection.ConfigFile);
                if (string.IsNullOrWhiteSpace(configDir) || !Directory.Exists(configDir))
                {
                    return false;
                }

                string[] referencefiles = Directory.GetFiles(configDir, "references.txt");
                if (referencefiles.Length == 0)
                {
                    return false;
                }

                using (StreamReader sr = new StreamReader(referencefiles[0]))
                {
                    char[] chars = { ';', ',' };
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.StartsWith("#") || line.Length == 0)
                        {
                            continue;
                        }

                        string[] x = line.Split(chars);
                        int listIndex;
                        if (x.Length > 0 && int.TryParse(x[0], out listIndex) && listIndex < listCount)
                        {
                            xrefs[listIndex] = x;
                        }
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public eListConversation TryLoadConversationList(eListCollection listCollection)
        {
            if (listCollection == null)
            {
                return null;
            }
            if (listCollection.ConversationListIndex < 0 || listCollection.Lists.Length <= listCollection.ConversationListIndex)
            {
                return null;
            }

            try
            {
                return new eListConversation((byte[])listCollection.Lists[listCollection.ConversationListIndex].elementValues[0][0]);
            }
            catch
            {
                return null;
            }
        }
    }
}
