using System.Collections.Generic;

namespace FWEledit
{
    public sealed class ItemTransferPackageManifest
    {
        public string Format { get; set; }
        public int FormatVersion { get; set; }
        public int ElementsVersion { get; set; }
        public int SourceListIndex { get; set; }
        public string SourceListName { get; set; }
        public int OriginalItemIndex { get; set; }
        public int OriginalId { get; set; }
        public string OriginalName { get; set; }
        public string ExportedAtUtc { get; set; }
        public List<ItemTransferFieldValue> Fields { get; set; }
        public List<ItemTransferPathDataEntry> PathDataEntries { get; set; }
        public List<ItemTransferAssetEntry> Assets { get; set; }
    }

    public sealed class ItemTransferFieldValue
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
    }

    public sealed class ItemTransferPathDataEntry
    {
        public int PathId { get; set; }
        public string MappedPath { get; set; }
    }

    public sealed class ItemTransferAssetEntry
    {
        public string Package { get; set; }
        public string RelativePath { get; set; }
        public string MappedPath { get; set; }
        public string ZipPath { get; set; }
        public long Size { get; set; }
    }

    public sealed class ItemTransferExportResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public int AssetCount { get; set; }
        public int MissingAssetCount { get; set; }
        public List<string> MissingAssets { get; set; }
    }

    public sealed class ItemTransferProgressInfo
    {
        public string Stage { get; set; }
        public string Detail { get; set; }
        public int Current { get; set; }
        public int Total { get; set; }
        public bool IsIndeterminate { get; set; }
    }

    public sealed class ItemTransferImportResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public int TargetListIndex { get; set; }
        public int NewItemIndex { get; set; }
        public int NewId { get; set; }
        public int ImportedAssetCount { get; set; }
        public int RemappedPathIdCount { get; set; }
        public int MissingAssetCount { get; set; }
    }
}
