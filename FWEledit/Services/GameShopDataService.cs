using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FWEledit
{
    public sealed class GameShopDataService
    {
        public const int SourceListIndex = -100;
        public const string SourceListName = "Goods in Game shop";

        private const int HeaderSize = 20;
        private const int RecordSize = 192;
        private const int ItemsCountOffset = 16;
        private const int IdOffset = 0;
        private const int NameOffset = 4;
        private const int NameByteLength = 64;
        private const int FileIconOffset = 68;
        private const int ItemIdOffset = 72;
        private const int QuantityOffset = 76;
        private const int IsHiddenOffset = 80;
        private const int CanGetConsumeScoreOffset = 84;
        private const int Unknown0Offset = 88;
        private const int GiftIdOffset = 92;
        private const int GiftRateOffset = 96;
        private const int BuyTypeMaskOffset = 100;
        private const int InternalPriceOffset = 104;
        private const int ExpireDateOffset = 108;
        private const int ExtSearchOffset = 112;
        private const int ExtSearchByteLength = 32;
        private const int NewItemIndexOffset = 144;
        private const int SellBeginTimeOffset = 148;
        private const int SellEndTimeOffset = 152;
        private const int DiscountBeginTimeOffset = 156;
        private const int DiscountEndTimeOffset = 160;
        private const int DiscountPriceOffset = 164;
        private const int LogoIconOffset = 168;
        private const int PreviewBackgroundPicOffset = 172;
        private const int PreviewPriorityOffset = 176;
        private const int DiscountItemShowIndexOffset = 180;
        private const int PriorityPreviewModelOffset = 184;
        private const int LimitTimesTaskIdOffset = 188;

        private readonly object syncRoot = new object();
        private string cachedPath = string.Empty;
        private long cachedLength;
        private long cachedTimestamp;
        private List<GameShopEntry> cachedEntries = new List<GameShopEntry>();

        public List<GameShopEntry> LoadEntries(string elementsPath)
        {
            string path = ResolvePath(elementsPath);
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return new List<GameShopEntry>();
            }

            FileInfo info;
            try
            {
                info = new FileInfo(path);
            }
            catch
            {
                return new List<GameShopEntry>();
            }

            lock (syncRoot)
            {
                if (string.Equals(cachedPath, info.FullName, StringComparison.OrdinalIgnoreCase)
                    && cachedLength == info.Length
                    && cachedTimestamp == info.LastWriteTimeUtc.Ticks)
                {
                    return new List<GameShopEntry>(cachedEntries);
                }

                List<GameShopEntry> entries = ParseFile(info.FullName);
                cachedPath = info.FullName;
                cachedLength = info.Length;
                cachedTimestamp = info.LastWriteTimeUtc.Ticks;
                cachedEntries = entries;
                return new List<GameShopEntry>(cachedEntries);
            }
        }

        public bool SaveEntry(string elementsPath, GameShopEntry entry, out string error)
        {
            error = string.Empty;
            if (entry == null)
            {
                error = "No game shop entry selected.";
                return false;
            }

            string path = ResolvePath(elementsPath);
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                error = "gshop.data was not found.";
                return false;
            }

            try
            {
                byte[] data = File.ReadAllBytes(path);
                int recordStart = entry.RecordOffset;
                if (recordStart < HeaderSize || recordStart + RecordSize > data.Length)
                {
                    error = "Invalid game shop entry offset.";
                    return false;
                }

                WriteInt32(data, recordStart + IdOffset, entry.Id);
                WriteUtf16String(data, recordStart + NameOffset, NameByteLength, entry.Name);
                WriteInt32(data, recordStart + FileIconOffset, entry.FileIcon);
                WriteInt32(data, recordStart + ItemIdOffset, entry.ItemId);
                WriteInt32(data, recordStart + QuantityOffset, entry.Quantity);
                WriteUInt32(data, recordStart + IsHiddenOffset, entry.IsHidden);
                WriteUInt32(data, recordStart + CanGetConsumeScoreOffset, entry.CanGetConsumeScore);
                WriteUInt32(data, recordStart + Unknown0Offset, entry.Unknown0);
                WriteInt32(data, recordStart + GiftIdOffset, entry.GiftId);
                WriteSingle(data, recordStart + GiftRateOffset, entry.GiftRate);
                WriteUInt32(data, recordStart + BuyTypeMaskOffset, entry.BuyTypeMask);
                WriteInt32(data, recordStart + InternalPriceOffset, entry.InternalPrice);
                WriteUInt32(data, recordStart + ExpireDateOffset, entry.ExpireDate);
                WriteUtf16String(data, recordStart + ExtSearchOffset, ExtSearchByteLength, entry.ExtSearch);
                WriteUInt32(data, recordStart + NewItemIndexOffset, entry.NewItemIndex);
                WriteUInt32(data, recordStart + SellBeginTimeOffset, entry.SellBeginTime);
                WriteUInt32(data, recordStart + SellEndTimeOffset, entry.SellEndTime);
                WriteUInt32(data, recordStart + DiscountBeginTimeOffset, entry.DiscountBeginTime);
                WriteUInt32(data, recordStart + DiscountEndTimeOffset, entry.DiscountEndTime);
                WriteInt32(data, recordStart + DiscountPriceOffset, entry.DiscountPrice);
                WriteInt32(data, recordStart + LogoIconOffset, entry.LogoIcon);
                WriteInt32(data, recordStart + PreviewBackgroundPicOffset, entry.PreviewBackgroundPic);
                WriteInt32(data, recordStart + PreviewPriorityOffset, entry.PreviewPriority);
                WriteUInt32(data, recordStart + DiscountItemShowIndexOffset, entry.DiscountItemShowIndex);
                WriteInt32(data, recordStart + PriorityPreviewModelOffset, entry.PriorityPreviewModel);
                WriteUInt32(data, recordStart + LimitTimesTaskIdOffset, entry.LimitTimesTaskId);

                File.WriteAllBytes(path, data);
                ClearCache();
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public void ClearCache()
        {
            lock (syncRoot)
            {
                cachedPath = string.Empty;
                cachedLength = 0;
                cachedTimestamp = 0;
                cachedEntries = new List<GameShopEntry>();
            }
        }

        public static string ResolvePath(string elementsPath)
        {
            if (string.IsNullOrWhiteSpace(elementsPath))
            {
                return string.Empty;
            }

            try
            {
                string elementsDirectory = Path.GetDirectoryName(elementsPath);
                if (string.IsNullOrWhiteSpace(elementsDirectory))
                {
                    return string.Empty;
                }

                string sameDirectory = Path.Combine(elementsDirectory, "gshop.data");
                if (File.Exists(sameDirectory))
                {
                    return sameDirectory;
                }

                DirectoryInfo parent = Directory.GetParent(elementsDirectory);
                if (parent != null)
                {
                    string siblingData = Path.Combine(parent.FullName, "data", "gshop.data");
                    if (File.Exists(siblingData))
                    {
                        return siblingData;
                    }
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        private static List<GameShopEntry> ParseFile(string path)
        {
            byte[] data;
            try
            {
                data = File.ReadAllBytes(path);
            }
            catch
            {
                return new List<GameShopEntry>();
            }

            List<GameShopEntry> entries = new List<GameShopEntry>();
            int declaredCount = ReadInt32Safe(data, ItemsCountOffset);
            if (declaredCount <= 0)
            {
                return entries;
            }

            int availableCount = Math.Max(0, (data.Length - HeaderSize) / RecordSize);
            int count = Math.Min(declaredCount, availableCount);
            for (int rowIndex = 0; rowIndex < count; rowIndex++)
            {
                int recordStart = HeaderSize + (rowIndex * RecordSize);
                int itemId = ReadInt32Safe(data, recordStart + ItemIdOffset);
                if (itemId <= 0)
                {
                    continue;
                }

                string name = ReadUtf16String(data, recordStart + NameOffset, NameByteLength);
                entries.Add(new GameShopEntry
                {
                    RowIndex = rowIndex,
                    RecordOffset = recordStart,
                    Id = ReadInt32Safe(data, recordStart + IdOffset),
                    FileIcon = ReadInt32Safe(data, recordStart + FileIconOffset),
                    Name = string.IsNullOrWhiteSpace(name) ? "Game shop item" : name,
                    ItemId = itemId,
                    Quantity = ReadInt32Safe(data, recordStart + QuantityOffset),
                    IsHidden = ReadUInt32Safe(data, recordStart + IsHiddenOffset),
                    CanGetConsumeScore = ReadUInt32Safe(data, recordStart + CanGetConsumeScoreOffset),
                    Unknown0 = ReadUInt32Safe(data, recordStart + Unknown0Offset),
                    GiftId = ReadInt32Safe(data, recordStart + GiftIdOffset),
                    GiftRate = ReadSingleSafe(data, recordStart + GiftRateOffset),
                    BuyTypeMask = ReadUInt32Safe(data, recordStart + BuyTypeMaskOffset),
                    InternalPrice = ReadInt32Safe(data, recordStart + InternalPriceOffset),
                    ExpireDate = ReadUInt32Safe(data, recordStart + ExpireDateOffset),
                    ExtSearch = ReadUtf16String(data, recordStart + ExtSearchOffset, ExtSearchByteLength),
                    NewItemIndex = ReadUInt32Safe(data, recordStart + NewItemIndexOffset),
                    SellBeginTime = ReadUInt32Safe(data, recordStart + SellBeginTimeOffset),
                    SellEndTime = ReadUInt32Safe(data, recordStart + SellEndTimeOffset),
                    DiscountBeginTime = ReadUInt32Safe(data, recordStart + DiscountBeginTimeOffset),
                    DiscountEndTime = ReadUInt32Safe(data, recordStart + DiscountEndTimeOffset),
                    DiscountPrice = ReadInt32Safe(data, recordStart + DiscountPriceOffset),
                    LogoIcon = ReadInt32Safe(data, recordStart + LogoIconOffset),
                    PreviewBackgroundPic = ReadInt32Safe(data, recordStart + PreviewBackgroundPicOffset),
                    PreviewPriority = ReadInt32Safe(data, recordStart + PreviewPriorityOffset),
                    DiscountItemShowIndex = ReadUInt32Safe(data, recordStart + DiscountItemShowIndexOffset),
                    PriorityPreviewModel = ReadInt32Safe(data, recordStart + PriorityPreviewModelOffset),
                    LimitTimesTaskId = ReadUInt32Safe(data, recordStart + LimitTimesTaskIdOffset)
                });
            }

            return entries;
        }

        private static int ReadInt32Safe(byte[] data, int offset)
        {
            if (data == null || offset < 0 || offset + 4 > data.Length)
            {
                return 0;
            }

            return BitConverter.ToInt32(data, offset);
        }

        private static uint ReadUInt32Safe(byte[] data, int offset)
        {
            if (data == null || offset < 0 || offset + 4 > data.Length)
            {
                return 0;
            }

            return BitConverter.ToUInt32(data, offset);
        }

        private static float ReadSingleSafe(byte[] data, int offset)
        {
            if (data == null || offset < 0 || offset + 4 > data.Length)
            {
                return 0;
            }

            return BitConverter.ToSingle(data, offset);
        }

        private static string ReadUtf16String(byte[] data, int offset, int byteLength)
        {
            if (data == null || offset < 0 || byteLength <= 0 || offset >= data.Length)
            {
                return string.Empty;
            }

            int safeLength = Math.Min(byteLength, data.Length - offset);
            string text = Encoding.Unicode.GetString(data, offset, safeLength);
            int zeroIndex = text.IndexOf('\0');
            if (zeroIndex >= 0)
            {
                text = text.Substring(0, zeroIndex);
            }

            return text.Trim();
        }

        private static void WriteInt32(byte[] data, int offset, int value)
        {
            Array.Copy(BitConverter.GetBytes(value), 0, data, offset, 4);
        }

        private static void WriteUInt32(byte[] data, int offset, uint value)
        {
            Array.Copy(BitConverter.GetBytes(value), 0, data, offset, 4);
        }

        private static void WriteSingle(byte[] data, int offset, float value)
        {
            Array.Copy(BitConverter.GetBytes(value), 0, data, offset, 4);
        }

        private static void WriteUtf16String(byte[] data, int offset, int byteLength, string value)
        {
            Array.Clear(data, offset, byteLength);
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            byte[] textBytes = Encoding.Unicode.GetBytes(value.Split('\0')[0]);
            int copyLength = Math.Min(textBytes.Length, byteLength);
            if (copyLength == byteLength && copyLength >= 2)
            {
                copyLength -= 2;
            }

            Array.Copy(textBytes, 0, data, offset, copyLength);
        }
    }
}
