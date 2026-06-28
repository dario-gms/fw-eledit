using FWEledit.DDSReader;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace FWEledit
{
    public sealed class CreaturePortraitIconService
    {
        private readonly TgaImageService tgaImageService = new TgaImageService();
        private readonly PckEntryReaderService pckEntryReaderService = new PckEntryReaderService();
        private readonly Dictionary<string, Bitmap> cache = new Dictionary<string, Bitmap>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, string> portraitFileIndex;

        public bool TryResolvePortrait(
            CacheSave database,
            eListCollection listCollection,
            int listIndex,
            string rawValue,
            out Bitmap icon)
        {
            icon = null;
            if (!IsCreaturePortraitList(listCollection, listIndex))
            {
                return false;
            }

            string mappedPath = ResolveMappedPath(database, rawValue);
            if (string.IsNullOrWhiteSpace(mappedPath))
            {
                return false;
            }

            string cacheKey = NormalizePath(mappedPath);
            if (cache.TryGetValue(cacheKey, out icon) && icon != null)
            {
                return true;
            }

            Bitmap loaded = LoadPortrait(mappedPath);
            if (loaded == null)
            {
                return false;
            }

            using (loaded)
            {
                icon = CreateSquareThumbnail(loaded, 32);
            }

            if (icon != null)
            {
                cache[cacheKey] = icon;
                return true;
            }

            return false;
        }

        public bool IsCreaturePortraitField(eListCollection listCollection, int listIndex, string fieldName)
        {
            if (!IsCreaturePortraitList(listCollection, listIndex))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            string normalizedFieldName = fieldName.Trim();
            string listName = listCollection != null
                && listIndex >= 0
                && listCollection.Lists != null
                && listIndex < listCollection.Lists.Length
                ? (listCollection.Lists[listIndex].listName ?? string.Empty)
                : string.Empty;

            if (listName.IndexOf("PET_BEDGE_ESSENCE", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return string.Equals(normalizedFieldName, "file_head_icon", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(normalizedFieldName, "file_self_head_icon", StringComparison.OrdinalIgnoreCase);
            }

            return string.Equals(normalizedFieldName, "file_icon", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalizedFieldName, "file_icon1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalizedFieldName, "file_head_icon", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalizedFieldName, "file_self_head_icon", StringComparison.OrdinalIgnoreCase);
        }

        public string FormatPortraitPathIdDisplay(CacheSave database, string rawValue)
        {
            int pathId;
            string mappedPath;
            if (!TryResolvePortraitPath(database, rawValue, out pathId, out mappedPath))
            {
                return rawValue ?? string.Empty;
            }

            string name = Path.GetFileName(mappedPath);
            if (string.IsNullOrWhiteSpace(name))
            {
                name = mappedPath;
            }

            return name + " | " + mappedPath;
        }

        public bool TryResolvePortraitPath(CacheSave database, string rawValue, out int pathId, out string mappedPath)
        {
            pathId = 0;
            mappedPath = string.Empty;
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return false;
            }

            string value = rawValue.Trim();
            if (!int.TryParse(value, out pathId))
            {
                mappedPath = value;
                return !string.IsNullOrWhiteSpace(mappedPath);
            }

            if (pathId <= 0 || database == null || database.pathById == null)
            {
                return false;
            }

            int[] candidates = new int[] { pathId, pathId + 1, pathId - 1 };
            for (int i = 0; i < candidates.Length; i++)
            {
                string candidatePath;
                if (database.pathById.TryGetValue(candidates[i], out candidatePath) && !string.IsNullOrWhiteSpace(candidatePath))
                {
                    pathId = candidates[i];
                    mappedPath = candidatePath;
                    return true;
                }
            }

            return false;
        }

        public Bitmap TryLoadPortraitThumbnail(string mappedPath, int size)
        {
            Bitmap loaded = LoadPortrait(mappedPath);
            if (loaded == null)
            {
                return null;
            }

            using (loaded)
            {
                return CreateSquareThumbnail(loaded, size);
            }
        }

        public List<TgaPortraitEntry> BuildPortraitEntries(CacheSave database, int thumbnailSize)
        {
            List<TgaPortraitEntry> entries = new List<TgaPortraitEntry>();
            if (database == null || database.pathById == null)
            {
                return entries;
            }

            for (int i = 0; i < database.pathById.Count; i++)
            {
                int pathId = database.pathById.Keys[i];
                string mappedPath = database.pathById.Values[i];
                if (!LooksLikePortraitPath(mappedPath))
                {
                    continue;
                }

                Bitmap thumbnail = TryLoadPortraitThumbnail(mappedPath, thumbnailSize);
                if (thumbnail == null)
                {
                    continue;
                }

                string name = Path.GetFileName(mappedPath);
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = mappedPath;
                }

                entries.Add(new TgaPortraitEntry
                {
                    PathId = pathId,
                    Path = mappedPath,
                    Name = name,
                    Thumbnail = thumbnail
                });
            }

            return entries;
        }

        public static bool IsCreaturePortraitList(eListCollection listCollection, int listIndex)
        {
            if (listCollection == null || listIndex < 0 || listIndex >= listCollection.Lists.Length)
            {
                return false;
            }

            string listName = listCollection.Lists[listIndex].listName ?? string.Empty;
            return listName.IndexOf("MONSTER_ESSENCE", StringComparison.OrdinalIgnoreCase) >= 0
                || listName.IndexOf("NPC_ESSENCE", StringComparison.OrdinalIgnoreCase) >= 0
                || listName.IndexOf("PET_BEDGE_ESSENCE", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string ResolveMappedPath(CacheSave database, string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return string.Empty;
            }

            string value = rawValue.Trim();
            int pathId;
            if (!int.TryParse(value, out pathId))
            {
                return value;
            }

            if (pathId <= 0 || database == null || database.pathById == null)
            {
                return string.Empty;
            }

            int[] candidates = new int[] { pathId, pathId + 1, pathId - 1 };
            for (int i = 0; i < candidates.Length; i++)
            {
                string mapped;
                if (database.pathById.TryGetValue(candidates[i], out mapped) && !string.IsNullOrWhiteSpace(mapped))
                {
                    return mapped;
                }
            }

            return string.Empty;
        }

        private static bool LooksLikePortraitPath(string mappedPath)
        {
            string normalized = StripSurfacesPrefix(NormalizePath(mappedPath));
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return false;
            }

            if (normalized.IndexOf("head\\", StringComparison.OrdinalIgnoreCase) < 0
                && normalized.IndexOf("\\head\\", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }

            string extension = Path.GetExtension(normalized);
            return string.IsNullOrWhiteSpace(extension) || IsSupportedImageExtension(extension);
        }

        private Bitmap LoadPortrait(string mappedPath)
        {
            List<string> candidates = BuildPathCandidates(mappedPath);
            for (int i = 0; i < candidates.Count; i++)
            {
                string filePath = ResolveExtractedFile(candidates[i]);
                if (!string.IsNullOrWhiteSpace(filePath))
                {
                    Bitmap bitmap = LoadImageFromFile(filePath);
                    if (bitmap != null)
                    {
                        return bitmap;
                    }
                }
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                Bitmap bitmap = LoadImageFromPackage(candidates[i]);
                if (bitmap != null)
                {
                    return bitmap;
                }
            }

            return null;
        }

        private Bitmap LoadImageFromFile(string filePath)
        {
            string extension = Path.GetExtension(filePath) ?? string.Empty;
            if (string.Equals(extension, ".tga", StringComparison.OrdinalIgnoreCase))
            {
                return tgaImageService.TryLoad(filePath);
            }
            if (string.Equals(extension, ".dds", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    return DDS.LoadImage(filePath, true);
                }
                catch
                {
                    return null;
                }
            }

            try
            {
                using (Bitmap source = new Bitmap(filePath))
                {
                    return new Bitmap(source);
                }
            }
            catch
            {
                return null;
            }
        }

        private Bitmap LoadImageFromPackage(string relativePath)
        {
            string normalized = StripSurfacesPrefix(NormalizePath(relativePath));
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return null;
            }

            byte[] payload;
            string error;
            if (!pckEntryReaderService.TryReadFile("surfaces", normalized, out payload, out error))
            {
                return null;
            }

            string extension = Path.GetExtension(normalized) ?? string.Empty;
            if (string.Equals(extension, ".tga", StringComparison.OrdinalIgnoreCase))
            {
                return tgaImageService.TryLoad(payload);
            }
            if (string.Equals(extension, ".dds", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    return DDS.LoadImage(payload, true);
                }
                catch
                {
                    return null;
                }
            }

            try
            {
                using (MemoryStream stream = new MemoryStream(payload, false))
                using (Bitmap source = new Bitmap(stream))
                {
                    return new Bitmap(source);
                }
            }
            catch
            {
                return null;
            }
        }

        private string ResolveExtractedFile(string relativePath)
        {
            string normalized = StripSurfacesPrefix(NormalizePath(relativePath));
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return string.Empty;
            }

            if (Path.IsPathRooted(normalized) && File.Exists(normalized))
            {
                return normalized;
            }

            string[] roots = new string[]
            {
                Path.Combine(AssetManager.WorkspaceRootPath ?? string.Empty, "resources", "surfaces.pck.files"),
                Path.Combine(AssetManager.GameRootPath ?? string.Empty, "resources", "surfaces.pck.files")
            };

            for (int i = 0; i < roots.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(roots[i]))
                {
                    continue;
                }

                string candidate = Path.Combine(roots[i], normalized);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            EnsurePortraitFileIndex(roots);
            string fileName = Path.GetFileName(normalized);
            if (!string.IsNullOrWhiteSpace(fileName) && portraitFileIndex != null)
            {
                string indexedPath;
                if (portraitFileIndex.TryGetValue(fileName, out indexedPath) && File.Exists(indexedPath))
                {
                    return indexedPath;
                }
            }

            return string.Empty;
        }

        private void EnsurePortraitFileIndex(string[] roots)
        {
            if (portraitFileIndex != null)
            {
                return;
            }

            portraitFileIndex = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (roots == null)
            {
                return;
            }

            for (int i = 0; i < roots.Length; i++)
            {
                string root = roots[i];
                if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                {
                    continue;
                }

                string[] searchRoots = new string[]
                {
                    Path.Combine(root, "head"),
                    Path.Combine(root, "iconset")
                };

                for (int r = 0; r < searchRoots.Length; r++)
                {
                    if (!Directory.Exists(searchRoots[r]))
                    {
                        continue;
                    }

                    try
                    {
                        string[] files = Directory.GetFiles(searchRoots[r], "*.*", SearchOption.AllDirectories);
                        for (int f = 0; f < files.Length; f++)
                        {
                            string extension = Path.GetExtension(files[f]);
                            if (!IsSupportedImageExtension(extension))
                            {
                                continue;
                            }

                            string fileName = Path.GetFileName(files[f]);
                            if (!string.IsNullOrWhiteSpace(fileName) && !portraitFileIndex.ContainsKey(fileName))
                            {
                                portraitFileIndex.Add(fileName, files[f]);
                            }
                        }
                    }
                    catch
                    { }
                }
            }
        }

        private static List<string> BuildPathCandidates(string mappedPath)
        {
            List<string> result = new List<string>();
            string normalized = StripSurfacesPrefix(NormalizePath(mappedPath));
            AddCandidate(result, normalized);

            string extension = Path.GetExtension(normalized);
            if (string.IsNullOrWhiteSpace(extension))
            {
                AddCandidate(result, normalized + ".tga");
                AddCandidate(result, normalized + ".dds");
                AddCandidate(result, normalized + ".png");
                AddCandidate(result, normalized + ".jpg");
                AddCandidate(result, normalized + ".bmp");
            }

            return result;
        }

        private static void AddCandidate(List<string> result, string candidate)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                return;
            }
            if (!result.Contains(candidate))
            {
                result.Add(candidate);
            }
        }

        private static string NormalizePath(string path)
        {
            return (path ?? string.Empty).Trim().TrimStart('\\', '/').Replace('/', '\\');
        }

        private static string StripSurfacesPrefix(string path)
        {
            string normalized = NormalizePath(path);
            string marker = "surfaces.pck.files\\";
            int markerIndex = normalized.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex >= 0)
            {
                normalized = normalized.Substring(markerIndex + marker.Length);
            }

            if (normalized.StartsWith("resources\\surfaces.pck.files\\", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring("resources\\surfaces.pck.files\\".Length);
            }
            if (normalized.StartsWith("surfaces\\", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring("surfaces\\".Length);
            }

            return normalized;
        }

        private static bool IsSupportedImageExtension(string extension)
        {
            return string.Equals(extension, ".tga", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".dds", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".bmp", StringComparison.OrdinalIgnoreCase);
        }

        private static Bitmap CreateSquareThumbnail(Bitmap source, int size)
        {
            if (source == null || source.Width <= 0 || source.Height <= 0)
            {
                return null;
            }

            Bitmap thumbnail = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(thumbnail))
            {
                graphics.Clear(Color.Transparent);
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

            return thumbnail;
        }
    }
}
