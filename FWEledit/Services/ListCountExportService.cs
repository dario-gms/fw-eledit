using System.IO;

namespace FWEledit
{
    public sealed class ListCountExportService
    {
        public bool ExportListCounts(eListCollection listCollection, string filePath)
        {
            if (listCollection == null || string.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            using (StreamWriter file = new StreamWriter(filePath))
            {
                file.WriteLine("ver=" + listCollection.Version);
                for (int l = 0; l < listCollection.Lists.Length; l++)
                {
                    file.WriteLine(l + "=" + listCollection.Lists[l].elementValues.Length);
                }
            }

            return true;
        }
    }
}
