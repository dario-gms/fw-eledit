//using PWDE.Element_Editor_Classes;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using sELedit.Properties;
using eELedit;

namespace sELedit
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
                return imageposition != null && !string.IsNullOrEmpty(key) && imageposition.ContainsKey(key);
            }
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
                            return imagesChache[key];
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
                        return pageBitmap;
                    }
                }
            }
            return new Bitmap(new Bitmap(Resources.NoIcon));
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

