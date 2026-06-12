using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class WindowTitleService
    {
        public void Apply(Form target, eListCollection listCollection, string elementsFile)
        {
            if (target == null || listCollection == null)
            {
                return;
            }

            target.Text = BuildTitle(listCollection, elementsFile);
        }

        public string BuildTitle(eListCollection listCollection, string elementsFile)
        {
            string appTitle = BuildAppTitle();
            if (listCollection == null || listCollection.Lists == null || listCollection.Lists.Length == 0)
            {
                return appTitle;
            }

            string timestamp = string.Empty;
            if (listCollection.Lists[0].listOffset != null && listCollection.Lists[0].listOffset.Length > 0)
            {
                uint rawTimestamp = BitConverter.ToUInt32(listCollection.Lists[0].listOffset, 0);
                timestamp = ", Timestamp: " + FormatTimestamp(rawTimestamp);
            }

            return appTitle + " (" + elementsFile + " [Version: " + listCollection.Version.ToString() + timestamp + "])";
        }

        private static string BuildAppTitle()
        {
            string version = string.Empty;
            try
            {
                version = Application.ProductVersion;
            }
            catch
            {
            }

            return string.IsNullOrWhiteSpace(version)
                ? "FWEledit"
                : "FWEledit v" + version.Trim();
        }

        private string FormatTimestamp(uint timestamp)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            origin = origin.AddSeconds(timestamp);
            return origin.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}
