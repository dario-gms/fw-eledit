//using PWDE.Element_Editor_Classes;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using FWEledit.DDSReader;
using FWEledit.Properties;
using eELedit;

namespace FWEledit
{

    public class CacheSave
    {
        private readonly object imagesLock = new object();
        [JsonIgnore]
        public Bitmap sourceBitmap = null;

        [JsonIgnore]
        public bool started = false;

        [JsonIgnore]
        public SortedDictionary<string, Bitmap> imagesChache = new SortedDictionary<string, Bitmap>();

        [JsonIgnore]
        public Func<string, bool> ContainsDirectImage { get; set; }

        [JsonIgnore]
        public Func<string, string> ResolveDirectImageFile { get; set; }

        [JsonIgnore]
        public Func<string, byte[]> ResolveDirectImageBytes { get; set; }

        private string NormalizeIconKey(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            string fileName = path.Trim().Replace('/', '\\');
            fileName = System.IO.Path.GetFileName(fileName);
            return fileName.ToLowerInvariant();
        }

        public bool ContainsKey(string path)
        {
            string key = NormalizeIconKey(path);
            lock (imagesLock)
            {
                if (imageposition != null && !string.IsNullOrEmpty(key) && imageposition.ContainsKey(key))
                {
                    return true;
                }
            }

            return ContainsDirectImage != null && ContainsDirectImage(path);
        }

        public Bitmap images(string name)
        {
            string key = NormalizeIconKey(name);
            lock (imagesLock)
            {
                if (sourceBitmap != null)
                {
                    if (!string.IsNullOrEmpty(key) && imageposition.ContainsKey(key))
                    {
                        if (imagesChache != null && imagesChache.ContainsKey(key))
                        {
                            Bitmap cachedAtlas = imagesChache[key];
                            if (LooksLikeDirectImageReference(name) && IsProbablyUnusableIcon(cachedAtlas))
                            {
                                Bitmap directFromCachedAtlas = TryLoadDirectImage(name);
                                if (directFromCachedAtlas != null)
                                {
                                    return directFromCachedAtlas;
                                }
                            }
                            return cachedAtlas;
                        }
                        int w = iconWidth > 0 ? iconWidth : 32;
                        int h = iconHeight > 0 ? iconHeight : 32;
                        Point d = imageposition[key];
                        Bitmap pageBitmap = new Bitmap(w, h, PixelFormat.Format32bppArgb);
                        using (Graphics graphics = Graphics.FromImage(pageBitmap))
                        {
                            graphics.DrawImage(sourceBitmap, new Rectangle(0, 0, w, h), new Rectangle(d.X, d.Y, w, h), GraphicsUnit.Pixel);
                        }
                        if(imagesChache == null || imagesChache != null && !imagesChache.ContainsKey(key))
                        {
                            imagesChache[key] = pageBitmap;
                        }
                        if (LooksLikeDirectImageReference(name) && IsProbablyUnusableIcon(pageBitmap))
                        {
                            Bitmap directImage = TryLoadDirectImage(name);
                            if (directImage != null)
                            {
                                return directImage;
                            }
                        }
                        return pageBitmap;
                    }
                }
            }

            Bitmap directImageFallback = TryLoadDirectImage(name);
            if (directImageFallback != null)
            {
                return directImageFallback;
            }
            return new Bitmap(new Bitmap(Resources.NoIcon));
        }

        private static bool IsProbablyUnusableIcon(Bitmap bitmap)
        {
            if (bitmap == null || bitmap.Width <= 0 || bitmap.Height <= 0)
            {
                return true;
            }

            int visible = 0;
            int total = 0;
            long brightness = 0;
            int maxBrightness = 0;
            for (int y = 0; y < bitmap.Height; y += Math.Max(1, bitmap.Height / 8))
            {
                for (int x = 0; x < bitmap.Width; x += Math.Max(1, bitmap.Width / 8))
                {
                    Color c = bitmap.GetPixel(x, y);
                    total++;
                    if (c.A <= 8)
                    {
                        continue;
                    }

                    visible++;
                    int b = (c.R + c.G + c.B) / 3;
                    brightness += b;
                    if (b > maxBrightness)
                    {
                        maxBrightness = b;
                    }
                }
            }

            if (total <= 0 || visible == 0)
            {
                return true;
            }

            int visiblePercent = (visible * 100) / total;
            int averageBrightness = (int)(brightness / visible);
            return visiblePercent < 8 || (averageBrightness < 14 && maxBrightness < 38);
        }

        private static bool LooksLikeDirectImageReference(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            string normalized = name.Trim().Replace('/', '\\');
            return normalized.IndexOf('\\') >= 0;
        }

        private Bitmap TryLoadDirectImage(string name)
        {
            if ((ResolveDirectImageBytes == null && ResolveDirectImageFile == null) || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            string cacheKey = "direct:" + name.Trim().Replace('/', '\\').ToLowerInvariant();
            lock (imagesLock)
            {
                if (imagesChache != null && imagesChache.ContainsKey(cacheKey))
                {
                    return imagesChache[cacheKey];
                }
            }

            byte[] payload = ResolveDirectImageBytes != null ? ResolveDirectImageBytes(name) : null;
            if (payload == null && ResolveDirectImageFile != null)
            {
                string filePath = ResolveDirectImageFile(name);
                if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
                {
                    try
                    {
                        payload = File.ReadAllBytes(filePath);
                    }
                    catch
                    {
                        payload = null;
                    }
                }
            }

            if (payload == null || payload.Length == 0)
            {
                return null;
            }

            Bitmap bitmap = null;
            string extension = Path.GetExtension(name);
            try
            {
                if (string.Equals(extension, ".dds", StringComparison.OrdinalIgnoreCase))
                {
                    bitmap = DDS.LoadImage(payload, true, DDSReader.Utils.PixelFormat.UNKNOWN)
                        ?? DDS.LoadImage(payload, true, DDSReader.Utils.PixelFormat.DXT3)
                        ?? DDS.LoadImage(payload, true, DDSReader.Utils.PixelFormat.DXT5)
                        ?? DDS.LoadImage(payload, true, DDSReader.Utils.PixelFormat.DXT1);
                    if (bitmap == null)
                    {
                        bitmap = TryLoadDirectImageFromTempFile(payload, extension);
                    }
                }
                else if (string.Equals(extension, ".tga", StringComparison.OrdinalIgnoreCase))
                {
                    bitmap = new TgaImageService().TryLoad(payload);
                    if (bitmap == null)
                    {
                        bitmap = TryLoadDirectImageFromTempFile(payload, extension);
                    }
                }
                else
                {
                    using (MemoryStream stream = new MemoryStream(payload, false))
                    using (Image image = Image.FromStream(stream))
                    {
                        bitmap = new Bitmap(image);
                    }
                }
            }
            catch
            {
                bitmap = null;
            }

            if (bitmap == null)
            {
                return null;
            }

            bitmap = NormalizeDirectIconBitmap(bitmap);

            lock (imagesLock)
            {
                if (imagesChache != null && !imagesChache.ContainsKey(cacheKey))
                {
                    imagesChache[cacheKey] = bitmap;
                }
            }

            return bitmap;
        }

        private Bitmap TryLoadDirectImageFromTempFile(byte[] payload, string extension)
        {
            if (payload == null || payload.Length == 0)
            {
                return null;
            }

            string tempPath = string.Empty;
            try
            {
                string safeExtension = string.IsNullOrWhiteSpace(extension) ? ".bin" : extension;
                tempPath = Path.Combine(Path.GetTempPath(), "FWEledit", "pck-image-cache", Guid.NewGuid().ToString("N") + safeExtension);
                Directory.CreateDirectory(Path.GetDirectoryName(tempPath));
                File.WriteAllBytes(tempPath, payload);

                if (string.Equals(safeExtension, ".dds", StringComparison.OrdinalIgnoreCase))
                {
                    return DDS.LoadImage(tempPath, true, DDSReader.Utils.PixelFormat.UNKNOWN)
                        ?? DDS.LoadImage(tempPath, true, DDSReader.Utils.PixelFormat.DXT3)
                        ?? DDS.LoadImage(tempPath, true, DDSReader.Utils.PixelFormat.DXT5)
                        ?? DDS.LoadImage(tempPath, true, DDSReader.Utils.PixelFormat.DXT1)
                        ?? DDS.LoadImage(tempPath);
                }
                if (string.Equals(safeExtension, ".tga", StringComparison.OrdinalIgnoreCase))
                {
                    return new TgaImageService().TryLoad(tempPath);
                }

                using (Image image = Image.FromFile(tempPath))
                {
                    return new Bitmap(image);
                }
            }
            catch
            {
                return null;
            }
            finally
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(tempPath) && File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
                catch
                {
                }
            }
        }

        private Bitmap NormalizeDirectIconBitmap(Bitmap source)
        {
            if (source == null)
            {
                return null;
            }

            int targetWidth = iconWidth > 0 ? iconWidth : 32;
            int targetHeight = iconHeight > 0 ? iconHeight : 32;
            if (source.Width == targetWidth && source.Height == targetHeight)
            {
                return source;
            }

            Bitmap scaled = new Bitmap(targetWidth, targetHeight, PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(scaled))
            {
                graphics.Clear(Color.Transparent);
                graphics.CompositingMode = CompositingMode.SourceOver;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = source.Width < targetWidth || source.Height < targetHeight
                    ? InterpolationMode.HighQualityBicubic
                    : InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.SmoothingMode = SmoothingMode.HighQuality;

                Rectangle destination = FitImageIntoBounds(source.Width, source.Height, targetWidth, targetHeight);
                using (ImageAttributes attributes = new ImageAttributes())
                {
                    attributes.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(
                        source,
                        destination,
                        0,
                        0,
                        source.Width,
                        source.Height,
                        GraphicsUnit.Pixel,
                        attributes);
                }
            }

            source.Dispose();
            return scaled;
        }

        private static Rectangle FitImageIntoBounds(int sourceWidth, int sourceHeight, int targetWidth, int targetHeight)
        {
            if (sourceWidth <= 0 || sourceHeight <= 0 || targetWidth <= 0 || targetHeight <= 0)
            {
                return new Rectangle(0, 0, Math.Max(1, targetWidth), Math.Max(1, targetHeight));
            }

            float scale = Math.Min((float)targetWidth / sourceWidth, (float)targetHeight / sourceHeight);
            int width = Math.Max(1, (int)Math.Round(sourceWidth * scale));
            int height = Math.Max(1, (int)Math.Round(sourceHeight * scale));
            int x = (targetWidth - width) / 2;
            int y = (targetHeight - height) / 2;
            return new Rectangle(x, y, width, height);
        }
        
        public int rows = 0;
        public int cols = 0;
        public int iconWidth = 32;
        public int iconHeight = 32;
        public SortedList<int, String> imagesx = null;
        public SortedList<int, String> imagesById = null;
        public SortedList<int, string> pathById = null;
        public SortedList<string, Point> imageposition = null;      
        public SortedList<int, int> item_color = null;
        //public SortedList<int, string> item_desc = null;
        //public SortedList<string, string> arrTheme = null;
        public List<string> arrTheme = null;
        //public SortedList<int, ItemDupe> task_recipes = null;
        public SortedList<int, ItemDupe> task_items = null;
        [JsonIgnore]
        public int task_items_revision = 0;
        [JsonIgnore]
        public string task_items_signature = string.Empty;
        [JsonIgnore]
        public int path_data_revision = 0;
        [JsonIgnore]
        public int model_picker_revision = 0;
        public string[] task_items_list = null;
        public SortedList monsters_npcs_mines = null;
        public SortedList titles = null;
        public SortedList homeitems = null;
        public SortedList InstanceList = null;
        public string[] buff_str = null;
        public string[] item_ext_desc = null;
        public string[] skillstr = null;
        public string[] world_targets = null;
        public SortedList addonslist = null;
        public SortedList LocalizationText = null;
    }
}

