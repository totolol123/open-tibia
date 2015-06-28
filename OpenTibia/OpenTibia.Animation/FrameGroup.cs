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
using System.Collections.Generic;
#endregion

namespace OpenTibia.Animation
{
    public enum FrameGroupType : byte
    {
        Default = 0,
        Walking = 1
    }

    public class FrameGroup
    {
        #region Public Properties

        public byte Width { get; set; }

        public byte Height { get; set; }

        public byte ExactSize { get; set; }

        public byte Layers { get; set; }

        public byte PatternX { get; set; }

        public byte PatternY { get; set; }

        public byte PatternZ { get; set; }

        public byte Frames { get; set; }

        public uint[] SpriteIDs { get; set; }

        public bool IsAnimation { get; set; }

        public AnimationMode AnimationMode { get; set; }

        public int LoopCount { get; set; }

        public sbyte StartFrame { get; set; }

        public FrameDuration[] FrameDurations { get; set; }

        #endregion

        #region Public Methods
        
        public int GetTotalSprites()
        {
            return this.Width * this.Height * this.PatternX * this.PatternY * this.PatternZ * this.Frames * this.Layers;
        }

        public int GetSpriteIndex(int width, int height, int layers, int patternX, int patternY, int patternZ, int frames)
        {
            return ((((((frames % this.Frames) * this.PatternZ + patternZ) * this.PatternY + patternY) * this.PatternX + patternX) * this.Layers + layers) * this.Height + height) * this.Width + width;
        }

        public int GetTextureIndex(int layer, int patternX, int patternY, int patternZ, int frame)
        {
            return (((frame % this.Frames * this.PatternZ + patternZ) * this.PatternY + patternY) * this.PatternX + patternX) * this.Layers + layer;
        }

        #endregion
    }
}
