using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace FWEledit
{
    public sealed class TgaImageService
    {
        public Bitmap TryLoad(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                {
                    return null;
                }

                using (FileStream stream = File.OpenRead(filePath))
                {
                    return Load(stream);
                }
            }
            catch
            {
                return null;
            }
        }

        public Bitmap TryLoad(byte[] payload)
        {
            try
            {
                if (payload == null || payload.Length == 0)
                {
                    return null;
                }

                using (MemoryStream stream = new MemoryStream(payload, false))
                {
                    return Load(stream);
                }
            }
            catch
            {
                return null;
            }
        }

        private Bitmap Load(Stream stream)
        {
            using (BinaryReader reader = new BinaryReader(stream))
            {
                int idLength = reader.ReadByte();
                int colorMapType = reader.ReadByte();
                int imageType = reader.ReadByte();
                reader.ReadBytes(5);
                reader.ReadInt16();
                reader.ReadInt16();
                int width = reader.ReadUInt16();
                int height = reader.ReadUInt16();
                int bitsPerPixel = reader.ReadByte();
                int descriptor = reader.ReadByte();

                if (width <= 0 || height <= 0 || width > 8192 || height > 8192)
                {
                    return null;
                }
                if (colorMapType != 0)
                {
                    return null;
                }
                if (imageType != 2 && imageType != 3 && imageType != 10 && imageType != 11)
                {
                    return null;
                }
                if (bitsPerPixel != 8 && bitsPerPixel != 24 && bitsPerPixel != 32)
                {
                    return null;
                }

                if (idLength > 0)
                {
                    reader.ReadBytes(idLength);
                }

                bool topOrigin = (descriptor & 0x20) != 0;
                bool rightOrigin = (descriptor & 0x10) != 0;
                int pixelCount = width * height;
                int[] pixels = new int[pixelCount];

                if (imageType == 2 || imageType == 3)
                {
                    for (int i = 0; i < pixelCount; i++)
                    {
                        WritePixel(pixels, width, height, i, ReadPixel(reader, bitsPerPixel), topOrigin, rightOrigin);
                    }
                }
                else
                {
                    int written = 0;
                    while (written < pixelCount)
                    {
                        int packet = reader.ReadByte();
                        int count = (packet & 0x7F) + 1;
                        if ((packet & 0x80) != 0)
                        {
                            int pixel = ReadPixel(reader, bitsPerPixel);
                            for (int i = 0; i < count && written < pixelCount; i++)
                            {
                                WritePixel(pixels, width, height, written, pixel, topOrigin, rightOrigin);
                                written++;
                            }
                        }
                        else
                        {
                            for (int i = 0; i < count && written < pixelCount; i++)
                            {
                                WritePixel(pixels, width, height, written, ReadPixel(reader, bitsPerPixel), topOrigin, rightOrigin);
                                written++;
                            }
                        }
                    }
                }

                Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                BitmapData data = bitmap.LockBits(
                    new Rectangle(0, 0, width, height),
                    ImageLockMode.WriteOnly,
                    PixelFormat.Format32bppArgb);
                try
                {
                    Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
                }
                finally
                {
                    bitmap.UnlockBits(data);
                }
                return bitmap;
            }
        }

        private static int ReadPixel(BinaryReader reader, int bitsPerPixel)
        {
            if (bitsPerPixel == 8)
            {
                int gray = reader.ReadByte();
                return unchecked((int)0xFF000000) | (gray << 16) | (gray << 8) | gray;
            }

            int blue = reader.ReadByte();
            int green = reader.ReadByte();
            int red = reader.ReadByte();
            int alpha = bitsPerPixel == 32 ? reader.ReadByte() : 255;
            return (alpha << 24) | (red << 16) | (green << 8) | blue;
        }

        private static void WritePixel(int[] pixels, int width, int height, int streamIndex, int argb, bool topOrigin, bool rightOrigin)
        {
            int x = streamIndex % width;
            int y = streamIndex / width;
            if (!topOrigin)
            {
                y = height - 1 - y;
            }
            if (rightOrigin)
            {
                x = width - 1 - x;
            }

            pixels[(y * width) + x] = argb;
        }
    }
}
