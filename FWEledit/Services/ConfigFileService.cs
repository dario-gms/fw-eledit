using System;
using System.IO;

namespace FWEledit
{
    public sealed class ConfigFileService
    {
        public ConfigData Load(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("Invalid config path.", nameof(filePath));
            }

            using (StreamReader sr = new StreamReader(filePath))
            {
                int listCount = Convert.ToInt32(sr.ReadLine());

                int conversationListIndex;
                try
                {
                    conversationListIndex = Convert.ToInt32(sr.ReadLine());
                }
                catch
                {
                    conversationListIndex = 58;
                }

                string[] listNames = new string[listCount];
                string[] listOffsets = new string[listCount];
                string[][] fieldNames = new string[listCount][];
                string[][] fieldTypes = new string[listCount][];

                string line;
                for (int i = 0; i < listCount; i++)
                {
                    while ((line = sr.ReadLine()) == "")
                    {
                    }
                    listNames[i] = line;
                    listOffsets[i] = sr.ReadLine();
                    fieldNames[i] = sr.ReadLine().Split(new char[] { ';' });
                    fieldTypes[i] = sr.ReadLine().Split(new char[] { ';' });
                }

                return new ConfigData
                {
                    ConversationListIndex = conversationListIndex,
                    LoadedFileName = Path.GetFileName(filePath),
                    ListNames = listNames,
                    ListOffsets = listOffsets,
                    FieldNames = fieldNames,
                    FieldTypes = fieldTypes
                };
            }
        }

        public void Save(string filePath, ConfigData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("Invalid config path.", nameof(filePath));
            }

            using (StreamWriter sw = new StreamWriter(filePath))
            {
                int listCount = data.ListCount;
                sw.WriteLine(listCount);
                sw.WriteLine(data.ConversationListIndex);

                for (int i = 0; i < listCount; i++)
                {
                    sw.WriteLine();
                    sw.WriteLine(data.ListNames[i]);
                    sw.WriteLine(data.ListOffsets[i]);
                    sw.WriteLine(string.Join(";", data.FieldNames[i]));
                    sw.WriteLine(string.Join(";", data.FieldTypes[i]));
                }
            }
        }
    }
}
