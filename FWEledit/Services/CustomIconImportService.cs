using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace FWEledit
{
    public sealed class CustomIconImportService
    {
        private const string CustomIconPrefix = "surfaces\\icon\\custom\\";

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
                int width = database.iconWidth > 0 ? database.iconWidth : 32;
                int height = database.iconHeight > 0 ? database.iconHeight : 32;
                string relativePath = BuildImportedIconPath(sourcePath);
                string mappedPath = "surfaces\\" + relativePath;
                string stagingRoot = Path.Combine(Path.GetTempPath(), "FWEledit", "pck-stage", "custom-icon-surfaces", Guid.NewGuid().ToString("N"));
                string stagedFile = Path.Combine(stagingRoot, relativePath);

                Directory.CreateDirectory(Path.GetDirectoryName(stagedFile));
                using (Bitmap source = LoadSourceBitmap(sourcePath))
                using (Bitmap converted = BuildIconBitmap(source, width, height))
                {
                    WriteTga32(stagedFile, converted);
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

        private static string BuildImportedIconPath(string sourcePath)
        {
            string safeName = MakeSafeAssetName(Path.GetFileNameWithoutExtension(sourcePath));
            string stamp = DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            return Path.Combine("icon", "custom", "fweledit_" + stamp + "_" + safeName + ".tga");
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

        private static void WriteTga32(string path, Bitmap bitmap)
        {
            using (FileStream stream = File.Create(path))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write((byte)2);
                writer.Write(new byte[5]);
                writer.Write((short)0);
                writer.Write((short)0);
                writer.Write((ushort)bitmap.Width);
                writer.Write((ushort)bitmap.Height);
                writer.Write((byte)32);
                writer.Write((byte)0x28);

                for (int y = 0; y < bitmap.Height; y++)
                {
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        Color color = bitmap.GetPixel(x, y);
                        writer.Write(color.B);
                        writer.Write(color.G);
                        writer.Write(color.R);
                        writer.Write(color.A);
                    }
                }
            }
        }
    }
}
