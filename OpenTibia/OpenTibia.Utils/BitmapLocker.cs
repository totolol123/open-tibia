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
using System.Runtime.InteropServices;
#endregion

namespace OpenTibia.Utilities
{
    public class BitmapLocker : IDisposable
    {
        #region Private Properties

        private Bitmap bitmap;
        private BitmapData bitmapData;
        private byte[] pixels;
        private IntPtr address = IntPtr.Zero;

        #endregion

        #region Constructor

        public BitmapLocker(Bitmap bitmap)
        {
            this.bitmap = bitmap;
        }

        #endregion

        #region Public Methods

        public void LockBits()
        {
            // create rectangle to lock
            Rectangle rect = new Rectangle(0, 0, this.bitmap.Width, this.bitmap.Height);

            // lock bitmap and return bitmap data
            this.bitmapData = this.bitmap.LockBits(rect, ImageLockMode.ReadWrite, this.bitmap.PixelFormat);

            // create byte array to copy pixel values
            this.pixels = new byte[this.bitmap.Width * this.bitmap.Height * 4];
            this.address = this.bitmapData.Scan0;

            // copy data from pointer to array
            Marshal.Copy(this.address, this.pixels, 0, this.pixels.Length);
        }

        public void CopyPixels(Bitmap source, int x, int y)
        {
            for (int py = 0; py < source.Height; py++)
            {
                for (int px = 0; px < source.Width; px++)
                {
                    this.SetPixel(px + x, py + y, source.GetPixel(px, py));
                }
            }
        }

        public void CopyPixels(Bitmap source, int rx, int ry, int rw, int rh, int px, int py)
        {
            for (int y = 0; y < rh; y++)
            {
                for (int x = 0; x < rw; x++)
                {
                    this.SetPixel(px + x, py + y, source.GetPixel(rx + x, ry + y));
                }
            }
        }

        public void UnlockBits()
        {
            // copy data from byte array to pointer
            Marshal.Copy(this.pixels, 0, this.address, this.pixels.Length);

            // unlock bitmap data
            this.bitmap.UnlockBits(this.bitmapData);
        }

        public void Dispose()
        {
            bitmap = null;
            bitmapData = null;
            pixels = null;
        }

        #endregion

        #region Private Methods

        private void SetPixel(int x, int y, Color color)
        {
            // get start index of the specified pixel
            int i = ((y * this.bitmap.Width) + x) * 4;

            if (i > this.pixels.Length - 4)
            {
                return;
            }

            this.pixels[i] = color.B;
            this.pixels[i + 1] = color.G;
            this.pixels[i + 2] = color.R;
            this.pixels[i + 3] = color.A;
        }

        #endregion
    }
}
