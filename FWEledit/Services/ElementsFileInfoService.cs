using System;
using System.IO;

namespace FWEledit
{
    public sealed class ElementsFileInfoService
    {
        public ElementsFileInfo ReadFileInfo(string filePath)
        {
            ElementsFileInfo info = new ElementsFileInfo { Success = false, FilePath = filePath ?? string.Empty };
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                info.ErrorMessage = "No File!";
                return info;
            }

            try
            {
                using (FileStream fs = File.OpenRead(filePath))
                using (BinaryReader br = new BinaryReader(fs))
                {
                    info.Version = br.ReadInt16();
                    info.Signature = br.ReadInt16();
                    if (info.Version >= 10)
                    {
                        info.Timestamp = br.ReadInt32();
                    }
                }
                info.Success = true;
                if (info.Timestamp != 0)
                {
                    DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                    origin = origin.AddSeconds(info.Timestamp);
                    info.TimestampText = origin.ToString("yyyy-MM-dd HH:mm:ss");
                }
                return info;
            }
            catch (Exception ex)
            {
                info.ErrorMessage = ex.Message;
                return info;
            }
        }
    }
}
