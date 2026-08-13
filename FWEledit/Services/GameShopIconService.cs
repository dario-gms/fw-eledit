using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using FWEledit.DDSReader;

namespace FWEledit
{
    public sealed class GameShopIconService
    {
        private readonly Dictionary<int, Image> imageCacheByPathId = new Dictionary<int, Image>();

        public List<GameShopIconOption> LoadOptions(CacheSave database, string elementsPath)
        {
            List<GameShopIconOption> options = new List<GameShopIconOption>();
            if (database == null || database.pathById == null)
            {
                return options;
            }

            foreach (KeyValuePair<int, string> item in database.pathById.OrderBy(pair => pair.Key))
            {
                string path = NormalizePath(item.Value);
                if (!IsGameShopIconPath(path))
                {
                    continue;
                }

                options.Add(new GameShopIconOption
                {
                    PathId = item.Key,
                    Path = path,
                    FileName = Path.GetFileName(path),
                    Image = TryLoadIcon(database, elementsPath, item.Key, path)
                });
            }

            return options;
        }

        public Image TryLoadIcon(CacheSave database, string elementsPath, int pathId, string mappedPath)
        {
            Image cached;
            if (imageCacheByPathId.TryGetValue(pathId, out cached) && cached != null)
            {
                return cached;
            }

            string normalized = NormalizePath(mappedPath);
            Image image = database != null ? TryLoadFromDatabase(database, normalized) : null;

            if (image != null)
            {
                imageCacheByPathId[pathId] = image;
            }

            return image;
        }

        public string ResolveMappedPath(CacheSave database, int pathId)
        {
            if (database == null || database.pathById == null)
            {
                return string.Empty;
            }

            string mapped;
            return database.pathById.TryGetValue(pathId, out mapped) ? NormalizePath(mapped) : string.Empty;
        }

        public static bool IsGameShopIconPath(string path)
        {
            string normalized = NormalizePath(path).ToLowerInvariant();
            if (normalized.Length == 0 || !normalized.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string withoutSurfaces = StripSurfacesPrefix(normalized);
            return withoutSurfaces.StartsWith("qshop\\", StringComparison.OrdinalIgnoreCase)
                && !withoutSurfaces.StartsWith("qshop\\background\\", StringComparison.OrdinalIgnoreCase);
        }

        public static string NormalizePath(string path)
        {
            return (path ?? string.Empty).Trim().TrimStart('\\', '/').Replace('/', '\\');
        }

        public Image TryLoadFromFile(string elementsPath, string mappedPath)
        {
            string fullPath = ResolveGameResourcePath(elementsPath, mappedPath);
            if (string.IsNullOrWhiteSpace(fullPath) || !File.Exists(fullPath))
            {
                return null;
            }

            try
            {
                Bitmap pfimBitmap = TryLoadDdsWithPfim(fullPath);
                if (pfimBitmap != null)
                {
                    return pfimBitmap;
                }

                return DDS.LoadImage(fullPath);
            }
            catch
            {
                try
                {
                    using (Bitmap source = new Bitmap(fullPath))
                    {
                        return new Bitmap(source);
                    }
                }
                catch
                {
                    return null;
                }
            }
        }

        private static Bitmap TryLoadDdsWithPfim(string ddsPath)
        {
            if (string.IsNullOrWhiteSpace(ddsPath) || !File.Exists(ddsPath))
            {
                return null;
            }

            try
            {
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory ?? string.Empty;
                string[] candidates = new string[]
                {
                    Path.Combine(baseDirectory, "Pfim.dll"),
                    Path.Combine(baseDirectory, "lib", "Pfim.dll"),
                    Path.Combine(Environment.CurrentDirectory ?? string.Empty, "Pfim.dll"),
                    Path.Combine(Environment.CurrentDirectory ?? string.Empty, "tools", "Pfim.dll")
                };

                string pfimPath = string.Empty;
                for (int i = 0; i < candidates.Length; i++)
                {
                    if (File.Exists(candidates[i]))
                    {
                        pfimPath = candidates[i];
                        break;
                    }
                }

                if (string.IsNullOrWhiteSpace(pfimPath))
                {
                    return null;
                }

                Assembly assembly = Assembly.LoadFrom(pfimPath);
                Type pfimageType = assembly.GetType("Pfim.Pfimage");
                Type iimageType = assembly.GetType("Pfim.IImage");
                if (pfimageType == null || iimageType == null)
                {
                    return null;
                }

                MethodInfo fromFile = pfimageType.GetMethod("FromFile", new Type[] { typeof(string) });
                if (fromFile == null)
                {
                    return null;
                }

                object image = fromFile.Invoke(null, new object[] { ddsPath });
                if (image == null)
                {
                    return null;
                }

                try
                {
                    PropertyInfo compressedProperty = iimageType.GetProperty("Compressed");
                    MethodInfo decompressMethod = iimageType.GetMethod("Decompress", Type.EmptyTypes);
                    if (compressedProperty != null && decompressMethod != null)
                    {
                        bool compressed = (bool)compressedProperty.GetValue(image, null);
                        if (compressed)
                        {
                            decompressMethod.Invoke(image, null);
                        }
                    }

                    int width = (int)iimageType.GetProperty("Width").GetValue(image, null);
                    int height = (int)iimageType.GetProperty("Height").GetValue(image, null);
                    int stride = (int)iimageType.GetProperty("Stride").GetValue(image, null);
                    int bpp = (int)iimageType.GetProperty("BitsPerPixel").GetValue(image, null);
                    byte[] data = (byte[])iimageType.GetProperty("Data").GetValue(image, null);
                    if (data == null || width <= 0 || height <= 0 || stride <= 0)
                    {
                        return null;
                    }

                    PixelFormat pixelFormat = bpp == 24 ? PixelFormat.Format24bppRgb : PixelFormat.Format32bppArgb;
                    Bitmap bitmap = new Bitmap(width, height, pixelFormat);
                    BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, pixelFormat);
                    try
                    {
                        int rowBytes = Math.Min(Math.Abs(bitmapData.Stride), Math.Abs(stride));
                        for (int y = 0; y < height; y++)
                        {
                            IntPtr destination = IntPtr.Add(bitmapData.Scan0, y * bitmapData.Stride);
                            Marshal.Copy(data, y * stride, destination, rowBytes);
                        }
                    }
                    finally
                    {
                        bitmap.UnlockBits(bitmapData);
                    }

                    return bitmap;
                }
                finally
                {
                    MethodInfo dispose = iimageType.GetMethod("Dispose", Type.EmptyTypes);
                    if (dispose != null)
                    {
                        dispose.Invoke(image, null);
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        private Image TryLoadFromDatabase(CacheSave database, string mappedPath)
        {
            string normalized = NormalizePath(mappedPath);
            string withoutSurfaces = StripSurfacesPrefix(normalized);
            string[] candidates = new string[]
            {
                normalized,
                withoutSurfaces,
                "surfaces\\" + withoutSurfaces,
                normalized.Replace('\\', '/'),
                withoutSurfaces.Replace('\\', '/'),
                ("surfaces\\" + withoutSurfaces).Replace('\\', '/')
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                string candidate = candidates[i];
                if (string.IsNullOrWhiteSpace(candidate) || !database.ContainsKey(candidate))
                {
                    continue;
                }

                try
                {
                    return database.images(candidate);
                }
                catch
                {
                }
            }

            return null;
        }

        private static string ResolveGameResourcePath(string elementsPath, string mappedPath)
        {
            string relative = NormalizePath(mappedPath);
            if (string.IsNullOrWhiteSpace(relative))
            {
                return string.Empty;
            }

            string withoutSurfaces = StripSurfacesPrefix(relative);
            string dataPath = GameShopDataService.ResolvePath(elementsPath);
            string dataDirectory = !string.IsNullOrWhiteSpace(dataPath) ? Path.GetDirectoryName(dataPath) : string.Empty;
            string gameRoot = string.Empty;
            if (!string.IsNullOrWhiteSpace(dataDirectory))
            {
                DirectoryInfo parent = Directory.GetParent(dataDirectory);
                gameRoot = parent != null ? parent.FullName : string.Empty;
            }

            List<string> candidates = new List<string>();
            for (int i = 0; i < candidates.Count; i++)
            {
                string candidate = candidates[i];
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return string.Empty;
        }

        private static void AddCandidate(List<string> candidates, params string[] parts)
        {
            if (parts == null || parts.Length == 0 || string.IsNullOrWhiteSpace(parts[0]))
            {
                return;
            }

            try
            {
                string candidate = Path.GetFullPath(Path.Combine(parts));
                if (!candidates.Contains(candidate, StringComparer.OrdinalIgnoreCase))
                {
                    candidates.Add(candidate);
                }
            }
            catch
            {
            }
        }

        private static string StripSurfacesPrefix(string path)
        {
            string normalized = NormalizePath(path);
            return normalized.StartsWith("surfaces\\", StringComparison.OrdinalIgnoreCase)
                ? normalized.Substring("surfaces\\".Length)
                : normalized;
        }
    }
}
