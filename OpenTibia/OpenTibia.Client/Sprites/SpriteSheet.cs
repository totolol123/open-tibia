﻿#region Licence
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
using OpenTibia.Geom;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
#endregion

namespace OpenTibia.Client.Sprites
{
    public class SpriteSheet
    {
        #region Constructor

        public SpriteSheet(Bitmap bitmap, Dictionary<int, Rect> rectList)
        {
            if (bitmap == null)
            {
                throw new ArgumentNullException("bitmap");
            }

            if (rectList == null || rectList.Count == 0)
            {
                throw new ArgumentNullException("rectList");
            }

            this.Bitmap = bitmap;
            this.RectList = rectList;
        }

        #endregion

        #region Public Properties

        public Bitmap Bitmap { get; private set; }

        public Dictionary<int, Rect> RectList { get; private set; }

        #endregion

        #region Public Methods
        
        public bool Save(string path, ImageFormat format)
        {
            this.Bitmap.Save(path, format);
            return true;
        }

        public bool Save(string path)
        {
            this.Bitmap.Save(path, ImageFormat.Png);
            return true;
        }

        #endregion
    }
}
