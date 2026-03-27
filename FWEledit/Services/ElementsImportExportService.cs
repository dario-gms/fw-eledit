using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ElementsImportExportService
    {
        public string ResolveRulesFilePath(string rulesLabel)
        {
            if (string.IsNullOrWhiteSpace(rulesLabel))
            {
                return string.Empty;
            }

            string fileName = rulesLabel.Replace("=>", "=");
            return Path.Combine(Application.StartupPath, "rules", fileName + ".rules");
        }

        public void ExportElementsWithRules(eListCollection listCollection, string rulesLabel, string outputPath)
        {
            if (listCollection == null)
            {
                throw new ArgumentNullException(nameof(listCollection));
            }
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException("Invalid output path.", nameof(outputPath));
            }

            string rulesFile = ResolveRulesFilePath(rulesLabel);
            listCollection.Export(rulesFile, outputPath);
        }

        public void ExportItems(eListCollection listCollection, int listIndex, int[] itemIndices, string folderPath, Action<int> progress)
        {
            if (listCollection == null)
            {
                throw new ArgumentNullException(nameof(listCollection));
            }
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                throw new ArgumentException("Invalid folder path.", nameof(folderPath));
            }
            if (itemIndices == null || itemIndices.Length == 0)
            {
                return;
            }

            eList list = listCollection.Lists[listIndex];
            for (int i = 0; i < itemIndices.Length; i++)
            {
                int index = itemIndices[i];
                string filePath = Path.Combine(folderPath, index.ToString());
                list.ExportItem(filePath, index);
                if (progress != null)
                {
                    progress(i + 1);
                }
            }
        }

        public void ImportItem(eListCollection listCollection, int listIndex, string filePath, int targetIndex)
        {
            if (listCollection == null)
            {
                throw new ArgumentNullException(nameof(listCollection));
            }
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("Invalid file path.", nameof(filePath));
            }

            listCollection.Lists[listIndex].ImportItem(filePath, targetIndex);
        }

        public int AddItemFromFile(eListCollection listCollection, int listIndex, string filePath, int templateIndex)
        {
            if (listCollection == null)
            {
                throw new ArgumentNullException(nameof(listCollection));
            }
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("Invalid file path.", nameof(filePath));
            }

            eList list = listCollection.Lists[listIndex];
            int newIndex = list.elementValues.Length;
            object[] template = new object[list.elementValues[templateIndex].Length];
            list.elementValues[templateIndex].CopyTo(template, 0);
            list.AddItem(template);
            list.ImportItem(filePath, newIndex);
            return newIndex;
        }

        public void ExportNpcListPairs(eListCollection listCollection, string outputPath)
        {
            if (listCollection == null)
            {
                throw new ArgumentNullException(nameof(listCollection));
            }
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException("Invalid output path.", nameof(outputPath));
            }

            using (StreamWriter sw = new StreamWriter(outputPath, false, Encoding.Unicode))
            {
                for (int i = 0; i < listCollection.Lists[38].elementValues.Length; i++)
                {
                    sw.WriteLine(listCollection.GetValue(38, i, 0) + "\t" + listCollection.GetValue(38, i, 2));
                }

                for (int i = 0; i < listCollection.Lists[57].elementValues.Length; i++)
                {
                    sw.WriteLine(listCollection.GetValue(57, i, 0) + "\t" + listCollection.GetValue(57, i, 1));
                }
            }
        }

        public void SaveListCountFile(eListCollection listCollection, string outputPath)
        {
            if (listCollection == null)
            {
                throw new ArgumentNullException(nameof(listCollection));
            }
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException("Invalid output path.", nameof(outputPath));
            }

            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            using (StreamWriter file = new StreamWriter(outputPath))
            {
                file.WriteLine("ver=" + listCollection.Version);
                for (int l = 0; l < listCollection.Lists.Length; l++)
                {
                    file.WriteLine(l + "=" + listCollection.Lists[l].elementValues.Length);
                }
            }
        }
    }
}
