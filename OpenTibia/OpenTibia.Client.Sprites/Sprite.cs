#region Licence
/**
* Copyright (C) 2015 Open Tibia Tools <https://github.com/ottools/open-tibia>
*
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
*
* The above copyright notice and this permission notice shall be included in all
* copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
*/
#endregion

#region Using Statements
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
#endregion

namespace OpenTibia.Client.Sprites
{
    public class Sprite
    {
        #region Constants

        public const byte DefaultSize = 32;
        public const ushort PixelsDataSize = 4096; // 32*32*4

        #endregion

        #region Private Properties

        private bool transparent = false;
        private Bitmap bitmap = null;

        #endregion

        #region Contructor

        public Sprite(uint id, bool transparent)
        {
            this.ID = id;
            this.Transparent = transparent;
        }

        public Sprite()
        {
            this.ID = 0;
            this.Transparent = false;
        }

        #endregion

        #region Public Properties

        public uint ID { get; set; }

        public byte[] CompressedPixels { get; internal set; }

        public int Length
        {
            get { return this.CompressedPixels == null ? 0 : this.CompressedPixels.Length; }
        }

        public bool Transparent
        {
            get
            { 
                return this.transparent;
            }

            set
            {
                if (this.transparent != value)
                {
                    if (this.Length != 0)
                    {
                        byte[] pixels = UncompressPixels(this.CompressedPixels, this.transparent);

                        this.transparent = value;
                        this.CompressedPixels = CompressPixels(pixels, this.transparent);
                        this.bitmap = null;
                    }
                    else
                    {
                        this.transparent = value;
                    }
                }
            }
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return this.ID.ToString();
        }

        public byte[] GetPixels()
        {
            byte[] bytes = this.CompressedPixels != null ? this.CompressedPixels : EmptyArray;
            return UncompressPixels(bytes, this.transparent);
        }

        public void SetPixels(byte[] pixels)
        {
            if (pixels != null && pixels.Length != PixelsDataSize)
            {
                throw new Exception("Invalid sprite pixels length");
            }

            this.CompressedPixels = CompressPixels(pixels, this.transparent);
            this.bitmap = null;
        }

        public Bitmap GetBitmap()
        {
            if (this.bitmap != null)
            {
                return this.bitmap;
            }

            this.bitmap = new Bitmap(DefaultSize, DefaultSize, PixelFormat.Format32bppArgb);

            byte[] pixels = this.GetPixels();
            if (pixels != null)
            {
                BitmapData bitmapData = this.bitmap.LockBits(Rectangle, ImageLockMode.ReadWrite, this.bitmap.PixelFormat);
                Marshal.Copy(pixels, 0, bitmapData.Scan0, pixels.Length);
                this.bitmap.UnlockBits(bitmapData);
            }

            return this.bitmap;
        }

        public void SetBitmap(Bitmap bitmap)
        {
            if (bitmap == null)
            {
                throw new ArgumentNullException("bitmap");
            }

            if (bitmap.Width != DefaultSize || bitmap.Height != DefaultSize)
            {
                throw new ArgumentException("bitmap", "Invalid bitmap size");
            }

            this.CompressedPixels = CompressBitmap(bitmap, this.transparent);
            this.bitmap = bitmap;
        }

        #endregion

        #region Class Properties

        private static readonly byte[] EmptyArray = new byte[0];

        #endregion

        #region Class Methods

        public static byte[] CompressBitmap(Bitmap bitmap)
        {
            if (bitmap == null)
            {
                throw new ArgumentNullException("pixels");
            }

            BitmapData bitmapData = bitmap.LockBits(Rectangle, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            byte[] pixels = new byte[PixelsDataSize];
            Marshal.Copy(bitmapData.Scan0, pixels, 0, PixelsDataSize);
            bitmap.UnlockBits(bitmapData);

            return CompressPixels(pixels, false);
        }

        public static byte[] CompressBitmap(Bitmap bitmap, bool transparent)
        {
            if (bitmap == null)
            {
                throw new ArgumentNullException("pixels");
            }

            BitmapData bitmapData = bitmap.LockBits(Rectangle, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            byte[] pixels = new byte[PixelsDataSize];
            Marshal.Copy(bitmapData.Scan0, pixels, 0, PixelsDataSize);
            bitmap.UnlockBits(bitmapData);

            return CompressPixels(pixels, transparent);
        }

        public static byte[] CompressPixels(byte[] pixels)
        {
            return CompressPixels(pixels, false);
        }

        public static byte[] CompressPixels(byte[] pixels, bool transparent)
        {
            if (pixels == null)
            {
                throw new ArgumentNullException("pixels");
            }

            if (pixels.Length != PixelsDataSize)
            {
                throw new Exception("Invalid pixels data size");
            }

            byte[] compressedPixels;

            using (BinaryWriter writer = new BinaryWriter(new MemoryStream()))
            {
                int read = 0;
                int alphaCount = 0;
                ushort chunkSize = 0;
                long coloredPos = 0;
                long finishOffset = 0;
                int length = pixels.Length / 4;
                int index = 0;

                while (index < length)
                {
                    chunkSize = 0;

                    // Read transparent pixels
                    while (index < length)
                    {
                        read = (index * 4) + 3;

                        // alpha
                        if (pixels[read++] != 0)
                        {
                            break;
                        }

                        alphaCount++;
                        chunkSize++;
                        index++;
                    }

                    // Read colored pixels
                    if (alphaCount < length && index < length)
                    {
                        writer.Write(chunkSize); // Writes the length of the transparent pixels
                        coloredPos = writer.BaseStream.Position; // Save colored position 
                        writer.BaseStream.Seek(2, SeekOrigin.Current); // Skip colored position
                        chunkSize = 0;

                        while (index < length)
                        {
                            read = index * 4;

                            byte blue = pixels[read++];
                            byte green = pixels[read++];
                            byte red = pixels[read++];
                            byte alpha = pixels[read++];

                            if (alpha == 0)
                            {
                                break;
                            }

                            writer.Write(red);
                            writer.Write(green);
                            writer.Write(blue);

                            if (transparent)
                            {
                                writer.Write(alpha);
                            }

                            chunkSize++;
                            index++;
                        }

                        finishOffset = writer.BaseStream.Position;
                        writer.BaseStream.Seek(coloredPos, SeekOrigin.Begin); // Go back to chunksize indicator
                        writer.Write(chunkSize); // Writes the length of he colored pixels
                        writer.BaseStream.Seek(finishOffset, SeekOrigin.Begin);
                    }
                }

                compressedPixels = ((MemoryStream)writer.BaseStream).ToArray();
            }

            return compressedPixels;
        }

        public static Bitmap UncompressBitmap(byte[] compressedPixels)
        {
            return UncompressBitmap(compressedPixels, false);
        }

        public static Bitmap UncompressBitmap(byte[] compressedPixels, bool transparent)
        {
            if (compressedPixels == null)
            {
                throw new ArgumentNullException("compressedPixels");
            }

            Bitmap bitmap = new Bitmap(DefaultSize, DefaultSize, PixelFormat.Format32bppArgb);
            byte[] pixels = UncompressPixels(compressedPixels, transparent);
            BitmapData bitmapData = bitmap.LockBits(Rectangle, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            Marshal.Copy(pixels, 0, bitmapData.Scan0, PixelsDataSize);
            bitmap.UnlockBits(bitmapData);
            return bitmap;
        }

        public static byte[] UncompressPixels(byte[] compressedPixels)
        {
            return UncompressPixels(compressedPixels, false);
        }

        public static byte[] UncompressPixels(byte[] compressedPixels, bool transparent)
        {
            if (compressedPixels == null)
            {
                throw new ArgumentNullException("compressedPixels");
            }

            int read = 0;
            int write = 0;
            int pos = 0;
            int transparentPixels = 0;
            int coloredPixels = 0;
            int length = compressedPixels.Length;
            byte bitPerPixel = (byte)(transparent ? 4 : 3);
            byte[] pixels = new byte[PixelsDataSize];

            for (read = 0; read < length; read += 4 + (bitPerPixel * coloredPixels))
            {
                transparentPixels = compressedPixels[pos++] | compressedPixels[pos++] << 8;
                coloredPixels = compressedPixels[pos++] | compressedPixels[pos++] << 8;

                for (int i = 0; i < transparentPixels; i++)
                {
                    pixels[write++] = 0x00; // Blue
                    pixels[write++] = 0x00; // Green
                    pixels[write++] = 0x00; // Red
                    pixels[write++] = 0x00; // Alpha
                }

                for (int i = 0; i < coloredPixels; i++)
                {
                    byte red = compressedPixels[pos++];
                    byte green = compressedPixels[pos++];
                    byte blue = compressedPixels[pos++];
                    byte alpha = transparent ? compressedPixels[pos++] : (byte)0xFF;

                    pixels[write++] = blue;
                    pixels[write++] = green;
                    pixels[write++] = red;
                    pixels[write++] = alpha;
                }
            }

            // Fills the remaining pixels
            while (write < PixelsDataSize)
            {
                pixels[write++] = 0x00; // Blue
                pixels[write++] = 0x00; // Green
                pixels[write++] = 0x00; // Red
                pixels[write++] = 0x00; // Alpha
            }

            return pixels;
        }

        #endregion

        #region Class Properties

        public static readonly Rectangle Rectangle = new Rectangle(0, 0, DefaultSize, DefaultSize);

        #endregion
    }
}
