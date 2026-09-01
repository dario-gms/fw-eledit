using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace FWEledit
{
    public sealed class CustomPortraitImportService
    {
        private const int PortraitSize = 64;

        public bool TryImportFromImage(
            string sourcePath,
            CacheSave database,
            AssetManager assetManager,
            out TgaPortraitEntry entry,
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
                string relativePath = BuildImportedPortraitPath(sourcePath);
                string mappedPath = "surfaces\\" + relativePath;
                string stagingRoot = Path.Combine(Path.GetTempPath(), "FWEledit", "pck-stage", "portrait-surfaces", Guid.NewGuid().ToString("N"));
                string stagedFile = Path.Combine(stagingRoot, relativePath);

                Directory.CreateDirectory(Path.GetDirectoryName(stagedFile));
                using (Bitmap source = LoadSourceBitmap(sourcePath))
                using (Bitmap converted = BuildPortraitBitmap(source, PortraitSize))
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

                CreaturePortraitIconService portraitIconService = new CreaturePortraitIconService();
                entry = new TgaPortraitEntry
                {
                    PathId = pathId,
                    Path = mappedPath,
                    Name = Path.GetFileName(mappedPath),
                    Thumbnail = portraitIconService.TryLoadPortraitThumbnail(mappedPath, 96)
                };
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
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
                database.pathById = new System.Collections.Generic.SortedList<int, string>();
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

        private static Bitmap BuildPortraitBitmap(Bitmap source, int size)
        {
            if (source == null || source.Width <= 0 || source.Height <= 0)
            {
                throw new InvalidOperationException("Invalid source image.");
            }

            Bitmap result = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(result))
            {
                graphics.Clear(Color.Transparent);
                graphics.CompositingMode = CompositingMode.SourceOver;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.SmoothingMode = SmoothingMode.HighQuality;

                float scale = Math.Max((float)size / source.Width, (float)size / source.Height);
                int width = Math.Max(1, (int)Math.Round(source.Width * scale));
                int height = Math.Max(1, (int)Math.Round(source.Height * scale));
                int x = (size - width) / 2;
                int y = (size - height) / 2;
                graphics.DrawImage(source, new Rectangle(x, y, width, height));
            }

            return result;
        }

        private static string BuildImportedPortraitPath(string sourcePath)
        {
            string safeName = MakeSafeAssetName(Path.GetFileNameWithoutExtension(sourcePath));
            string stamp = DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            return Path.Combine("head", "custom", "fweledit_" + stamp + "_" + safeName + ".tga");
        }

        private static string MakeSafeAssetName(string value)
        {
            string safe = Regex.Replace(value ?? string.Empty, "[^A-Za-z0-9_-]+", "_").Trim('_');
            if (string.IsNullOrWhiteSpace(safe))
            {
                safe = "portrait";
            }
            if (safe.Length > 40)
            {
                safe = safe.Substring(0, 40).Trim('_');
            }
            return safe.ToLowerInvariant();
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
