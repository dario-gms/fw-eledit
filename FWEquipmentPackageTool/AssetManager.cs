using System;
using System.Collections.Generic;

namespace FWEledit
{
    public sealed class AssetManager
    {
        private readonly PckEntryReaderService pckEntryReaderService = new PckEntryReaderService();

        public static string GameRootPath { get; set; }

        public static string WorkspaceRootPath { get; set; }

        public bool TryReadPackageEntry(string packageName, string relativePath, out byte[] payload, out string error)
        {
            payload = null;
            error = string.Empty;

            try
            {
                string resolvedRelativePath;
                return pckEntryReaderService.TryReadFileFast(packageName, relativePath, out payload, out resolvedRelativePath, out error);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public bool TryEnumeratePckIndexEntries(string packageName, out List<string> entries)
        {
            entries = new List<string>();
            try
            {
                string error;
                return pckEntryReaderService.TryEnumerateEntries(packageName, out entries, out error);
            }
            catch
            {
                return false;
            }
        }
    }
}
