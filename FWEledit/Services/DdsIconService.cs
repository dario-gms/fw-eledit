using System;
using System.Drawing;
using System.IO;
using System.Text;

namespace FWEledit
{
    public sealed class DdsIconService
    {
        public Bitmap BuildIcon(Bitmap rawImg, string rawTxt, string icoName)
        {
            try
            {
                int counter = 0;
                string line;
                int width = 0;
                int height = 0;
                double columns = 0;
                double imgNum = -1;
                double x;
                double y;
                Bitmap cropped = null;

                using (StreamReader file = new StreamReader(rawTxt, Encoding.GetEncoding("GB2312")))
                {
                    while ((line = file.ReadLine()) != null)
                    {
                        if (counter == 0) { width = Int32.Parse(line); }
                        if (counter == 1) { height = Int32.Parse(line); }
                        if (counter == 3) { columns = Int32.Parse(line); }

                        if (line == icoName)
                        {
                            imgNum = counter - 3;
                            break;
                        }
                        counter++;
                    }
                }

                if (imgNum != -1)
                {
                    x = Math.Floor(((imgNum * width) - width) / (columns * width)) * width;
                    y = ((imgNum * width) - width) - (((columns * width) * x) / width);

                    Rectangle rect = new Rectangle(Convert.ToInt32(y), Convert.ToInt32(x), width, height);
                    cropped = rawImg.Clone(rect, rawImg.PixelFormat);
                }
                return cropped;
            }
            catch
            {
                return Properties.Resources.NoIcon;
            }
        }
    }
}
