using System;

namespace FWEledit
{
    public static class ItemDescriptionCodec
    {
        public static string DecodeForEditor(string storedValue)
        {
            if (string.IsNullOrEmpty(storedValue))
            {
                return string.Empty;
            }
            return storedValue.Replace("\\r", Environment.NewLine).Replace("\\n", Environment.NewLine).Replace("\\t", "\t");
        }

        public static string EncodeForStorage(string editorValue)
        {
            if (string.IsNullOrEmpty(editorValue))
            {
                return string.Empty;
            }

            string normalized = editorValue.Replace("\r\n", "\n").Replace('\r', '\n');
            normalized = normalized.Replace("\\", "\\\\");
            normalized = normalized.Replace("\"", "\\\"");
            normalized = normalized.Replace("\t", "\\t");
            normalized = normalized.Replace("\n", "\\r");
            return normalized;
        }
    }
}
