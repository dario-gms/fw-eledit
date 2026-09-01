using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace FWEledit
{
    public sealed class CustomIconImportService
    {
        private const string CustomIconPrefix = "surfaces\\icon\\custom\\";
        private const string IconsetTextPath = "iconset\\iconlist_ivtr0.txt";
        private const string IconsetImagePath = "iconset\\iconlist_ivtr0.dds";

        public List<CustomIconEntry> BuildEntries(CacheSave database)
        {
            List<CustomIconEntry> entries = new List<CustomIconEntry>();
            if (database == null || database.pathById == null)
            {
                return entries;
            }

            foreach (KeyValuePair<int, string> item in database.pathById)
            {
                if (item.Key <= 0 || !IsCustomIconPath(item.Value))
                {
                    continue;
                }

                Bitmap thumb = TryLoadThumbnail(database, item.Value);
                entries.Add(new CustomIconEntry
                {
                    PathId = item.Key,
                    Path = NormalizeMappedPath(item.Value),
                    Name = Path.GetFileName(item.Value),
                    Thumbnail = thumb
                });
            }

            return entries;
        }

        public bool TryImportFromImage(
            string sourcePath,
            CacheSave database,
            AssetManager assetManager,
            out CustomIconEntry entry,
            out string error)
        {
            entry = null;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            {
                error = "Source image not found.";
                return false;
            }
            if (database == null)
            {
                error = "Database unavailable.";
                return false;
            }
            if (assetManager == null)
            {
                error = "Asset manager unavailable.";
                return false;
            }

            try
            {
                string relativePath = BuildImportedIconPath(sourcePath);
                string mappedPath = "surfaces\\" + relativePath;
                string iconFileName = Path.GetFileName(relativePath);
                string stagingRoot = Path.Combine(Path.GetTempPath(), "FWEledit", "pck-stage", "custom-icon-surfaces", Guid.NewGuid().ToString("N"));
                string stagedFile = Path.Combine(stagingRoot, relativePath);

                Directory.CreateDirectory(Path.GetDirectoryName(stagedFile));
                using (Bitmap source = LoadSourceBitmap(sourcePath))
                using (Bitmap converted = BuildIconBitmap(source, GetAtlasIconWidth(database), GetAtlasIconHeight(database)))
                {
                    WriteDdsDxt3(stagedFile, converted);
                    if (!TryStageIconsetEntry(database, assetManager, stagingRoot, iconFileName, converted, out error))
                    {
                        return false;
                    }
                }

                string importError;
                if (!assetManager.ImportStagedPackageAssets("surfaces", stagingRoot, out importError))
                {
                    error = string.IsNullOrWhiteSpace(importError)
                        ? "Failed to update surfaces.pck."
                        : importError;
                    return false;
                }

                int pathId;
                if (!TryResolveOrCreatePathId(database, assetManager, mappedPath, out pathId, out error))
                {
                    return false;
                }

                entry = new CustomIconEntry
                {
                    PathId = pathId,
                    Path = mappedPath,
                    Name = Path.GetFileName(mappedPath),
                    Thumbnail = TryLoadThumbnail(database, mappedPath)
                };
                RegisterImportedIconInDatabase(database, iconFileName);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static bool IsCustomIconPath(string mappedPath)
        {
            string normalized = NormalizeMappedPath(mappedPath);
            return normalized.StartsWith(CustomIconPrefix, StringComparison.OrdinalIgnoreCase)
                && IsSupportedImageExtension(normalized);
        }

        private static Bitmap TryLoadThumbnail(CacheSave database, string mappedPath)
        {
            if (database == null || string.IsNullOrWhiteSpace(mappedPath))
            {
                return null;
            }

            try
            {
                Bitmap image = database.images(mappedPath);
                if (image == null)
                {
                    return null;
                }
                return new Bitmap(image);
            }
            catch
            {
                return null;
            }
        }

        private static bool TryResolveOrCreatePathId(
            CacheSave database,
            AssetManager assetManager,
            string mappedPath,
            out int pathId,
            out string error)
        {
            pathId = 0;
            error = string.Empty;

            if (assetManager.TryFindPathIdByMappedPath(mappedPath, out pathId) && pathId > 0)
            {
                EnsureDatabasePath(database, pathId, mappedPath);
                return true;
            }

            int candidate = FindNewPathId(database, assetManager);
            for (int attempts = 0; attempts < 200000; attempts++)
            {
                int resolvedPathId;
                if (assetManager.TryUpsertWorkspacePathDataEntry(candidate, mappedPath, candidate, mappedPath, out resolvedPathId, out error))
                {
                    pathId = resolvedPathId > 0 ? resolvedPathId : candidate;
                    EnsureDatabasePath(database, pathId, mappedPath);
                    return true;
                }

                if (!string.IsNullOrWhiteSpace(error)
                    && error.StartsWith("PathID already mapped to another path:", StringComparison.OrdinalIgnoreCase))
                {
                    candidate++;
                    continue;
                }

                return false;
            }

            error = "Failed to allocate a collision-free PathID.";
            return false;
        }

        private static void EnsureDatabasePath(CacheSave database, int pathId, string mappedPath)
        {
            if (database == null || pathId <= 0)
            {
                return;
            }

            if (database.pathById == null)
            {
                database.pathById = new SortedList<int, string>();
            }

            database.pathById[pathId] = mappedPath;
        }

        private static int FindNewPathId(CacheSave database, AssetManager assetManager)
        {
            int maxInDatabase = 0;
            if (database != null && database.pathById != null && database.pathById.Count > 0)
            {
                maxInDatabase = database.pathById.Keys[database.pathById.Count - 1];
            }

            int maxInPathData = assetManager != null ? assetManager.GetMaxPathId() : 0;
            int candidate = Math.Max(maxInDatabase, maxInPathData) + 1;
            if (candidate <= 0)
            {
                candidate = 1;
            }

            while (database != null && database.pathById != null && database.pathById.ContainsKey(candidate))
            {
                candidate++;
            }

            return candidate;
        }

        private static Bitmap LoadSourceBitmap(string sourcePath)
        {
            using (Bitmap loaded = new Bitmap(sourcePath))
            {
                return new Bitmap(loaded);
            }
        }

        private static Bitmap BuildIconBitmap(Bitmap source, int width, int height)
        {
            if (source == null || source.Width <= 0 || source.Height <= 0)
            {
                throw new InvalidOperationException("Invalid source image.");
            }

            Bitmap result = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(result))
            {
                graphics.Clear(Color.Transparent);
                graphics.CompositingMode = CompositingMode.SourceOver;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.SmoothingMode = SmoothingMode.HighQuality;

                float scale = Math.Min((float)width / source.Width, (float)height / source.Height);
                int drawWidth = Math.Max(1, (int)Math.Round(source.Width * scale));
                int drawHeight = Math.Max(1, (int)Math.Round(source.Height * scale));
                int x = (width - drawWidth) / 2;
                int y = (height - drawHeight) / 2;
                graphics.DrawImage(source, new Rectangle(x, y, drawWidth, drawHeight));
            }

            return result;
        }

        private static bool TryStageIconsetEntry(
            CacheSave database,
            AssetManager assetManager,
            string stagingRoot,
            string iconFileName,
            Bitmap icon,
            out string error)
        {
            error = string.Empty;

            byte[] textPayload;
            if (!assetManager.TryReadPackageEntry("surfaces", IconsetTextPath, out textPayload, out error)
                || textPayload == null
                || textPayload.Length == 0)
            {
                error = string.IsNullOrWhiteSpace(error)
                    ? "Failed to read iconset\\iconlist_ivtr0.txt."
                    : error;
                return false;
            }

            Encoding encoding = Encoding.GetEncoding("GBK");
            string text = encoding.GetString(textPayload).Replace("\r\n", "\n").Replace('\r', '\n');
            List<string> lines = new List<string>(text.Split('\n'));
            while (lines.Count > 0 && string.IsNullOrEmpty(lines[lines.Count - 1]))
            {
                lines.RemoveAt(lines.Count - 1);
            }

            if (lines.Count < 4
                || !int.TryParse(lines[0], out int iconWidth)
                || !int.TryParse(lines[1], out int iconHeight)
                || !int.TryParse(lines[2], out int rows)
                || !int.TryParse(lines[3], out int cols)
                || iconWidth <= 0
                || iconHeight <= 0
                || rows <= 0
                || cols <= 0)
            {
                error = "Invalid iconset\\iconlist_ivtr0.txt header.";
                return false;
            }

            byte[] imagePayload;
            if (!assetManager.TryReadPackageEntry("surfaces", IconsetImagePath, out imagePayload, out error)
                || imagePayload == null
                || imagePayload.Length == 0)
            {
                error = string.IsNullOrWhiteSpace(error)
                    ? "Failed to read iconset\\iconlist_ivtr0.dds."
                    : error;
                return false;
            }

            if (!TryResolveAtlasCapacity(imagePayload, iconWidth, iconHeight, cols, out int maxRows, out error))
            {
                return false;
            }
            if (rows > maxRows)
            {
                error = "iconlist_ivtr0.txt declares more rows than the DDS can hold.";
                return false;
            }
            if (rows < maxRows)
            {
                rows = maxRows;
                lines[2] = rows.ToString(CultureInfo.InvariantCulture);
            }

            int iconIndex = lines.Count - 4;
            if (iconIndex >= rows * cols)
            {
                error = "iconlist_ivtr0 atlas is full.";
                return false;
            }

            if (!lines.Exists(line => string.Equals((line ?? string.Empty).Trim(), iconFileName, StringComparison.OrdinalIgnoreCase)))
            {
                lines.Add(iconFileName);
            }

            int x = (iconIndex % cols) * iconWidth;
            int y = (iconIndex / cols) * iconHeight;
            if (!TryPatchDdsDxt3IconSlot(imagePayload, x, y, iconWidth, iconHeight, icon, out byte[] patchedAtlas, out error))
            {
                return false;
            }
            PatchDatabaseAtlas(database, x, y, iconWidth, iconHeight, icon);

            string stagedText = Path.Combine(stagingRoot, IconsetTextPath);
            string stagedAtlas = Path.Combine(stagingRoot, IconsetImagePath);
            Directory.CreateDirectory(Path.GetDirectoryName(stagedText));
            File.WriteAllText(stagedText, string.Join("\r\n", lines) + "\r\n", encoding);
            File.WriteAllBytes(stagedAtlas, patchedAtlas);
            return true;
        }

        private static int GetAtlasIconWidth(CacheSave database)
        {
            return database != null && database.iconWidth > 0 ? database.iconWidth : 40;
        }

        private static int GetAtlasIconHeight(CacheSave database)
        {
            return database != null && database.iconHeight > 0 ? database.iconHeight : 40;
        }

        private static void RegisterImportedIconInDatabase(CacheSave database, string iconFileName)
        {
            if (database == null || string.IsNullOrWhiteSpace(iconFileName))
            {
                return;
            }

            int iconWidth = GetAtlasIconWidth(database);
            int iconHeight = GetAtlasIconHeight(database);
            int cols = database.cols > 0 ? database.cols : 102;
            if (database.imagesx == null)
            {
                database.imagesx = new SortedList<int, string>();
            }
            if (database.imageposition == null)
            {
                database.imageposition = new SortedList<string, Point>();
            }

            string key = Path.GetFileName(iconFileName);
            int index = database.imagesx.Count;
            if (!database.imagesx.ContainsValue(key))
            {
                database.imagesx[index] = key;
                int y = index / cols;
                int x = index - y * cols;
                if (!database.imageposition.ContainsKey(key))
                {
                    database.imageposition[key] = new Point(x * iconWidth, y * iconHeight);
                }
            }
        }

        private static bool TryPatchDdsDxt3IconSlot(
            byte[] dds,
            int slotX,
            int slotY,
            int slotWidth,
            int slotHeight,
            Bitmap icon,
            out byte[] patched,
            out string error)
        {
            patched = null;
            error = string.Empty;

            if (dds == null || dds.Length < 128 || icon == null)
            {
                error = "Invalid iconlist_ivtr0.dds payload.";
                return false;
            }
            if (dds[0] != (byte)'D' || dds[1] != (byte)'D' || dds[2] != (byte)'S' || dds[3] != (byte)' ')
            {
                error = "iconlist_ivtr0.dds is not a DDS file.";
                return false;
            }

            int atlasHeight = BitConverter.ToInt32(dds, 12);
            int atlasWidth = BitConverter.ToInt32(dds, 16);
            string fourCc = Encoding.ASCII.GetString(dds, 84, 4);
            if (!string.Equals(fourCc, "DXT3", StringComparison.OrdinalIgnoreCase))
            {
                error = "iconlist_ivtr0.dds is not DXT3.";
                return false;
            }
            if (slotWidth <= 0 || slotHeight <= 0 || slotWidth % 4 != 0 || slotHeight % 4 != 0)
            {
                error = "Icon slot size must be a positive multiple of 4 for DXT3.";
                return false;
            }
            if (slotX < 0 || slotY < 0 || slotX + slotWidth > atlasWidth || slotY + slotHeight > atlasHeight)
            {
                error = "Next icon slot is outside iconlist_ivtr0.dds bounds.";
                return false;
            }

            int atlasBlocksX = (atlasWidth + 3) / 4;
            int dataOffset = 128;
            patched = new byte[dds.Length];
            Buffer.BlockCopy(dds, 0, patched, 0, dds.Length);

            using (Bitmap atlasIcon = BuildIconBitmap(icon, slotWidth, slotHeight))
            {
                for (int localBlockY = 0; localBlockY < slotHeight / 4; localBlockY++)
                {
                    for (int localBlockX = 0; localBlockX < slotWidth / 4; localBlockX++)
                    {
                        int atlasBlockX = (slotX / 4) + localBlockX;
                        int atlasBlockY = (slotY / 4) + localBlockY;
                        int writeOffset = dataOffset + ((atlasBlockY * atlasBlocksX + atlasBlockX) * 16);
                        if (writeOffset < dataOffset || writeOffset + 16 > patched.Length)
                        {
                            error = "Computed icon slot block is outside iconlist_ivtr0.dds payload.";
                            patched = null;
                            return false;
                        }

                        byte[] block = EncodeDxt3Block(atlasIcon, localBlockX * 4, localBlockY * 4);
                        Buffer.BlockCopy(block, 0, patched, writeOffset, block.Length);
                    }
                }
            }

            return true;
        }

        private static byte[] EncodeDxt3Block(Bitmap bitmap, int startX, int startY)
        {
            using (MemoryStream stream = new MemoryStream(16))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                WriteDxt3Block(writer, bitmap, startX, startY);
                return stream.ToArray();
            }
        }

        private static void PatchDatabaseAtlas(CacheSave database, int x, int y, int width, int height, Bitmap icon)
        {
            if (database == null
                || database.sourceBitmap == null
                || icon == null
                || x < 0
                || y < 0
                || x + width > database.sourceBitmap.Width
                || y + height > database.sourceBitmap.Height)
            {
                return;
            }

            using (Graphics graphics = Graphics.FromImage(database.sourceBitmap))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.DrawImage(icon, new Rectangle(x, y, width, height));
            }
        }

        private static bool TryResolveAtlasCapacity(
            byte[] dds,
            int iconWidth,
            int iconHeight,
            int cols,
            out int maxRows,
            out string error)
        {
            maxRows = 0;
            error = string.Empty;

            if (dds == null || dds.Length < 128)
            {
                error = "Invalid iconlist_ivtr0.dds payload.";
                return false;
            }
            if (dds[0] != (byte)'D' || dds[1] != (byte)'D' || dds[2] != (byte)'S' || dds[3] != (byte)' ')
            {
                error = "iconlist_ivtr0.dds is not a DDS file.";
                return false;
            }

            int atlasHeight = BitConverter.ToInt32(dds, 12);
            int atlasWidth = BitConverter.ToInt32(dds, 16);
            string fourCc = Encoding.ASCII.GetString(dds, 84, 4);
            if (!string.Equals(fourCc, "DXT3", StringComparison.OrdinalIgnoreCase))
            {
                error = "iconlist_ivtr0.dds is not DXT3.";
                return false;
            }
            if (iconWidth <= 0 || iconHeight <= 0 || cols <= 0)
            {
                error = "Invalid iconlist_ivtr0 dimensions.";
                return false;
            }
            if (cols * iconWidth > atlasWidth)
            {
                error = "iconlist_ivtr0.txt declares more columns than the DDS can hold.";
                return false;
            }

            maxRows = atlasHeight / iconHeight;
            if (maxRows <= 0)
            {
                error = "iconlist_ivtr0.dds cannot hold any icon rows.";
                return false;
            }
            return true;
        }

        private static string BuildImportedIconPath(string sourcePath)
        {
            string safeName = MakeSafeAssetName(Path.GetFileNameWithoutExtension(sourcePath));
            string stamp = DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            return Path.Combine("icon", "custom", "fweledit_" + stamp + "_" + safeName + ".dds");
        }

        private static string MakeSafeAssetName(string value)
        {
            string safe = Regex.Replace(value ?? string.Empty, "[^A-Za-z0-9_-]+", "_").Trim('_');
            if (string.IsNullOrWhiteSpace(safe))
            {
                safe = "icon";
            }
            if (safe.Length > 40)
            {
                safe = safe.Substring(0, 40).Trim('_');
            }
            return safe.ToLowerInvariant();
        }

        private static string NormalizeMappedPath(string mappedPath)
        {
            string normalized = (mappedPath ?? string.Empty).Replace('/', '\\').Trim().TrimStart('\\');
            while (normalized.Contains("\\\\"))
            {
                normalized = normalized.Replace("\\\\", "\\");
            }
            return normalized;
        }

        private static bool IsSupportedImageExtension(string path)
        {
            string extension = Path.GetExtension(path);
            return string.Equals(extension, ".tga", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".dds", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".bmp", StringComparison.OrdinalIgnoreCase);
        }

        private static void WriteDdsDxt3(string path, Bitmap bitmap)
        {
            using (FileStream stream = File.Create(path))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                int blockCountX = (bitmap.Width + 3) / 4;
                int blockCountY = (bitmap.Height + 3) / 4;
                int linearSize = blockCountX * blockCountY * 16;

                writer.Write(new[] { (byte)'D', (byte)'D', (byte)'S', (byte)' ' });
                writer.Write(124);
                writer.Write(0x00081007);
                writer.Write(bitmap.Height);
                writer.Write(bitmap.Width);
                writer.Write(linearSize);
                writer.Write(0);
                writer.Write(0);
                for (int i = 0; i < 11; i++)
                {
                    writer.Write(0);
                }
                writer.Write(32);
                writer.Write(0x00000004);
                writer.Write(new[] { (byte)'D', (byte)'X', (byte)'T', (byte)'3' });
                writer.Write(0);
                writer.Write(0);
                writer.Write(0);
                writer.Write(0);
                writer.Write(0);
                writer.Write(0x00001000);
                writer.Write(0);
                writer.Write(0);
                writer.Write(0);
                writer.Write(0);

                for (int by = 0; by < blockCountY; by++)
                {
                    for (int bx = 0; bx < blockCountX; bx++)
                    {
                        WriteDxt3Block(writer, bitmap, bx * 4, by * 4);
                    }
                }
            }
        }

        private static void WriteDxt3Block(BinaryWriter writer, Bitmap bitmap, int startX, int startY)
        {
            Color[] pixels = new Color[16];
            for (int y = 0; y < 4; y++)
            {
                int sourceY = Math.Min(bitmap.Height - 1, startY + y);
                for (int x = 0; x < 4; x++)
                {
                    int sourceX = Math.Min(bitmap.Width - 1, startX + x);
                    pixels[y * 4 + x] = bitmap.GetPixel(sourceX, sourceY);
                }
            }

            ulong alphaBits = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                ulong alpha4 = (ulong)((pixels[i].A * 15 + 127) / 255);
                alphaBits |= alpha4 << (i * 4);
            }
            writer.Write(alphaBits);

            Color min;
            Color max;
            FindColorEndpoints(pixels, out min, out max);
            ushort c0 = ToRgb565(max);
            ushort c1 = ToRgb565(min);
            if (c0 < c1)
            {
                ushort swap = c0;
                c0 = c1;
                c1 = swap;
            }
            if (c0 == c1)
            {
                c1 = c0 > 0 ? (ushort)(c0 - 1) : (ushort)0;
            }

            Color[] palette = BuildDxtPalette(c0, c1);
            uint indices = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                int best = FindNearestPaletteIndex(pixels[i], palette);
                indices |= (uint)(best & 3) << (i * 2);
            }

            writer.Write(c0);
            writer.Write(c1);
            writer.Write(indices);
        }

        private static void FindColorEndpoints(Color[] pixels, out Color min, out Color max)
        {
            int minScore = int.MaxValue;
            int maxScore = int.MinValue;
            min = Color.Black;
            max = Color.White;

            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i];
                int score = (c.R * 77) + (c.G * 150) + (c.B * 29);
                if (score < minScore)
                {
                    minScore = score;
                    min = c;
                }
                if (score > maxScore)
                {
                    maxScore = score;
                    max = c;
                }
            }
        }

        private static ushort ToRgb565(Color color)
        {
            int r = (color.R * 31 + 127) / 255;
            int g = (color.G * 63 + 127) / 255;
            int b = (color.B * 31 + 127) / 255;
            return (ushort)((r << 11) | (g << 5) | b);
        }

        private static Color FromRgb565(ushort value)
        {
            int r5 = (value >> 11) & 0x1F;
            int g6 = (value >> 5) & 0x3F;
            int b5 = value & 0x1F;
            int r = (r5 << 3) | (r5 >> 2);
            int g = (g6 << 2) | (g6 >> 4);
            int b = (b5 << 3) | (b5 >> 2);
            return Color.FromArgb(255, r, g, b);
        }

        private static Color[] BuildDxtPalette(ushort c0, ushort c1)
        {
            Color color0 = FromRgb565(c0);
            Color color1 = FromRgb565(c1);
            return new[]
            {
                color0,
                color1,
                Color.FromArgb(255, (2 * color0.R + color1.R) / 3, (2 * color0.G + color1.G) / 3, (2 * color0.B + color1.B) / 3),
                Color.FromArgb(255, (color0.R + 2 * color1.R) / 3, (color0.G + 2 * color1.G) / 3, (color0.B + 2 * color1.B) / 3)
            };
        }

        private static int FindNearestPaletteIndex(Color color, Color[] palette)
        {
            int best = 0;
            int bestDistance = int.MaxValue;
            for (int i = 0; i < palette.Length; i++)
            {
                int dr = color.R - palette[i].R;
                int dg = color.G - palette[i].G;
                int db = color.B - palette[i].B;
                int distance = dr * dr + dg * dg + db * db;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }

            return best;
        }
    }
}
