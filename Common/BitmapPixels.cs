using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Isidore
{
    // Shared source in ImgProcess and Load, so neither assembly needs a reference
    // to the other. Only formats whose bytes exactly match GetPixel are decoded.
    internal static class BitmapPixels
    {
        internal static Color[,] ReadColors(Bitmap bitmap)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;
            PixelFormat format = bitmap.PixelFormat;
            int bitsPerPixel;
            switch (format)
            {
                case PixelFormat.Format24bppRgb: bitsPerPixel = 24; break;
                case PixelFormat.Format32bppRgb:
                case PixelFormat.Format32bppArgb: bitsPerPixel = 32; break;
                case PixelFormat.Format8bppIndexed: bitsPerPixel = 8; break;
                case PixelFormat.Format4bppIndexed: bitsPerPixel = 4; break;
                case PixelFormat.Format1bppIndexed: bitsPerPixel = 1; break;
                default:
                    // GDI+ determines rounding when expanding premultiplied,
                    // 16-bit and high-precision channels. Keep that behavior.
                    return ReadPixels(bitmap, width, height);
            }

            Color[] palette = bitsPerPixel <= 8 ? bitmap.Palette.Entries : null;
            BitmapData data;
            try
            {
                data = bitmap.LockBits(new Rectangle(0, 0, width, height),
                    ImageLockMode.ReadOnly, format);
            }
            catch (ArgumentException)
            {
                return ReadPixels(bitmap, width, height);
            }
            catch (ExternalException)
            {
                return ReadPixels(bitmap, width, height);
            }

            try
            {
                Color[,] colors = new Color[width, height];
                byte[] row = new byte[(int)(((long)width * bitsPerPixel + 7) / 8)];
                for (int y = 0; y < height; y++)
                {
                    // Scan0 is the first logical row, including negative strides.
                    Marshal.Copy(RowAddress(data, y), row, 0, row.Length);
                    for (int x = 0; x < width; x++)
                    {
                        if (palette != null)
                        {
                            int index = bitsPerPixel == 8 ? row[x] :
                                bitsPerPixel == 4 ? (row[x / 2] >> (x % 2 == 0 ? 4 : 0)) & 15 :
                                (row[x / 8] >> (7 - x % 8)) & 1;
                            colors[x, y] = Color.FromArgb(palette[index].ToArgb());
                        }
                        else
                        {
                            int offset = x * (bitsPerPixel / 8);
                            int alpha = format == PixelFormat.Format32bppArgb ? row[offset + 3] : 255;
                            colors[x, y] = Color.FromArgb(alpha,
                                row[offset + 2], row[offset + 1], row[offset]);
                        }
                    }
                }
                return colors;
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
        }

        internal static void WriteArgb(Bitmap bitmap, int[] pixels)
        {
            int width = bitmap.Width;
            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, width, bitmap.Height),
                ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try
            {
                for (int y = 0; y < bitmap.Height; y++)
                    Marshal.Copy(pixels, y * width,
                        RowAddress(data, y), width);
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
        }

        private static IntPtr RowAddress(BitmapData data, int row)
        {
            // External bitmaps can have large strides even for short rows.
            // Calculate in 64 bits, retaining native pointer width on x86.
            long address = unchecked(data.Scan0.ToInt64() + (long)row * data.Stride);
            return IntPtr.Size == 8 ? new IntPtr(address) : new IntPtr(unchecked((int)address));
        }

        private static Color[,] ReadPixels(Bitmap bitmap, int width, int height)
        {
            Color[,] colors = new Color[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    colors[x, y] = bitmap.GetPixel(x, y);
            return colors;
        }
    }
}
