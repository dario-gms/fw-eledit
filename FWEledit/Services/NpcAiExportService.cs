using System;
using System.IO;
using System.Text;

namespace FWEledit
{
    public sealed class NpcAiExportService
    {
        public FileExportResult ExportNpcAi(eListCollection listCollection, string filePath)
        {
            FileExportResult result = new FileExportResult { Success = false };
            if (listCollection == null)
            {
                result.ErrorMessage = "No File Loaded!";
                return result;
            }
            if (string.IsNullOrWhiteSpace(filePath))
            {
                result.ErrorMessage = "Invalid output path.";
                return result;
            }
            if (listCollection.Lists.Length <= 38)
            {
                result.ErrorMessage = "NPC AI list not found.";
                return result;
            }

            try
            {
                using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.Unicode))
                {
                    for (int i = 0; i < listCollection.Lists[38].elementValues.Length; i++)
                    {
                        sw.WriteLine(
                            listCollection.GetValue(38, i, 0) + "\t" +
                            listCollection.GetValue(38, i, 2) + "\t" +
                            listCollection.GetValue(38, i, 64));
                    }
                }
                result.Success = true;
                return result;
            }
            catch
            {
                result.ErrorMessage = "SAVING ERROR!";
                return result;
            }
        }
    }
}
