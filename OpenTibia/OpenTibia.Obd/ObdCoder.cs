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
using OpenTibia.Animation;
using OpenTibia.Client.Sprites;
using OpenTibia.Client.Things;
using OpenTibia.Collections;
using OpenTibia.Utils;
using System;
using System.IO;
using System.Text;
#endregion

namespace OpenTibia.Obd
{
    public enum ObdVersion : ushort
    {
        InvalidVersion = 0,
        Version1 = 100,
        Version2 = 200,
        Version3 = 300
    }

    internal enum ObdFlags : byte
    {
        Ground = 0x00,
        GroundBorder = 0x01,
        OnBottom = 0x02,
        OnTop = 0x03,
        Container = 0x04,
        Stackable = 0x05,
        ForceUse = 0x06,
        MultiUse = 0x07,
        Writable = 0x08,
        WritableOnce = 0x09,
        FluidContainer = 0x0A,
        Fluid = 0x0B,
        IsUnpassable = 0x0C,
        IsUnmovable = 0x0D,
        BlockMissiles = 0x0E,
        BlockPathfinder = 0x0F,
        NoMoveAnimation = 0x10,
        Pickupable = 0x11,
        Hangable = 0x12,
        HookSouth = 0x13,
        HookEast = 0x14,
        Rotatable = 0x15,
        HasLight = 0x16,
        DontHide = 0x17,
        Translucent = 0x18,
        HasOffset = 0x19,
        HasElevation = 0x1A,
        LyingObject = 0x1B,
        Minimap = 0x1D,
        AnimateAlways = 0x1C,
        LensHelp = 0x1E,
        FullGround = 0x1F,
        IgnoreLook = 0x20,
        Cloth = 0x21,
        Market = 0x22,
        DefaultAction = 0x23,

        HasChanges = 0xFC,
        FloorChange = 0xFD,
        Usable = 0xFE,

        LastFlag = 0xFF
    }

    public static class ObdCoder
    {
        #region | Public Static Methods |

        public static byte[] Encode(ThingData data, ObdVersion obdVersion)
        {
            if (obdVersion == ObdVersion.Version3)
            {
                return EncodeV3(data);
            }
            else if (obdVersion == ObdVersion.Version2)
            {
                return EncodeV2(data);
            }
            else if (obdVersion == ObdVersion.Version1)
            {
                return EncodeV1(data);
            }

            return null;
        }

        public static bool Save(string path, ThingData data, ObdVersion version)
        {
            if (data == null)
            {
                return false;
            }

            byte[] bytes = Encode(data, version);
            if (bytes == null)
            {
                return false;
            }

            using (BinaryWriter writer = new BinaryWriter(new FileStream(path, FileMode.Create)))
            {
                writer.Write(bytes);
                writer.Close();
            }

            return true;
        }

        public static bool Save(string path, ThingData data)
        {
            return Save(path, data, ObdVersion.Version2);
        }

        public static ThingData Decode(byte[] bytes)
        {
            if (bytes == null)
            {
                return null;
            }

            bytes = LZMACoder.Uncompress(bytes);

            using (BinaryReader reader = new BinaryReader(new MemoryStream(bytes)))
            {
                ushort version = reader.ReadUInt16();
                
                if (version == (ushort)ObdVersion.Version3)
                {
                    return DecodeV3(reader);
                }
                else if (version == (ushort)ObdVersion.Version2)
                {
                    return DecodeV2(reader);
                }
                else if (version >= (ushort)DatFormat.Format_710)
                {
                    return DecodeV1(reader);
                }
                else
                {
                    
                }
            }

            return null;
        }

        public static ThingData Load(string path)
        {
            try
            {
                using (BinaryReader reader = new BinaryReader(new FileStream(path, FileMode.Open)))
                {
                    byte[] bytes = reader.ReadBytes((int)reader.BaseStream.Length);
                    reader.Close();
                    return Decode(bytes);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return null;
            }
        }

        #endregion

        #region | Private Static Methods |

        private static byte[] EncodeV1(ThingData data)
        {
            using (BinaryWriter writer = new BinaryWriter(new MemoryStream()))
            {
                // write client version
                writer.Write((ushort)DatFormat.Format_1010);

                // write category
                string category = string.Empty;
                switch (data.Category)
                {
                    case ThingCategory.Item:
                        category = "item";
                        break;

                    case ThingCategory.Outfit:
                        category = "outfit";
                        break;

                    case ThingCategory.Effect:
                        category = "effect";
                        break;

                    case ThingCategory.Missile:
                        category = "missile";
                        break;
                }

                writer.Write((ushort)category.Length);
                writer.Write(Encoding.UTF8.GetBytes(category));

                if (!ThingTypeSerializer.WriteProperties(data.ThingType, DatFormat.Format_1010, writer))
                {
                    return null;
                }

                FrameGroup group = data.GetFrameGroup(FrameGroupType.Default);

                writer.Write(group.Width);
                writer.Write(group.Height);

                if (group.Width > 1 || group.Height > 1)
                {
                    writer.Write(group.ExactSize);
                }

                writer.Write(group.Layers);
                writer.Write(group.PatternX);
                writer.Write(group.PatternY);
                writer.Write(group.PatternZ);
                writer.Write(group.Frames);

                Sprite[] sprites = data.Sprites[FrameGroupType.Default];
                for (int i = 0; i < sprites.Length; i++)
                {
                    Sprite sprite = sprites[i];
                    byte[] pixels = sprite.GetARGBPixels();
                    writer.Write((uint)sprite.ID);
                    writer.Write((uint)pixels.Length);
                    writer.Write(pixels);
                }

                return LZMACoder.Compress(((MemoryStream)writer.BaseStream).ToArray());
            }
        }

        private static byte[] EncodeV2(ThingData data)
        {
            using (BinaryWriter writer = new BinaryWriter(new MemoryStream()))
            {
                // write obd version
                writer.Write((ushort)ObdVersion.Version2);

                // write client version
                writer.Write((ushort)DatFormat.Format_1050);

                // write category
                writer.Write((byte)data.Category);

                // skipping the texture patterns position.
                int patternsPosition = (int)writer.BaseStream.Position;
                writer.Seek(4, SeekOrigin.Current);

                if (!WriteProperties(data.ThingType, writer))
                {
                    return null;
                }

                // write the texture patterns position.
                int position = (int)writer.BaseStream.Position;
                writer.Seek(patternsPosition, SeekOrigin.Begin);
                writer.Write((uint)writer.BaseStream.Position);
                writer.Seek(position, SeekOrigin.Begin);

                FrameGroup group = data.GetFrameGroup(FrameGroupType.Default);

                writer.Write(group.Width);
                writer.Write(group.Height);

                if (group.Width > 1 || group.Height > 1)
                {
                    writer.Write(group.ExactSize);
                }

                writer.Write(group.Layers);
                writer.Write(group.PatternX);
                writer.Write(group.PatternY);
                writer.Write(group.PatternZ);
                writer.Write(group.Frames);

                if (group.IsAnimation)
                {
                    writer.Write((byte)group.AnimationMode);
                    writer.Write(group.LoopCount);
                    writer.Write(group.StartFrame);

                    for (int i = 0; i < group.Frames; i++)
                    {
                        writer.Write((uint)group.FrameDurations[i].Minimum);
                        writer.Write((uint)group.FrameDurations[i].Maximum);
                    }
                }

                Sprite[] sprites = data.Sprites[FrameGroupType.Default];
                for (int i = 0; i < sprites.Length; i++)
                {
                    Sprite sprite = sprites[i];
                    byte[] pixels = sprite.GetARGBPixels();
                    writer.Write(sprite.ID);
                    writer.Write(pixels);
                }

                return LZMACoder.Compress(((MemoryStream)writer.BaseStream).ToArray());
            }
        }

        private static byte[] EncodeV3(ThingData data)
        {
            throw new NotImplementedException();
        }

        public static bool WriteProperties(ThingType thing, BinaryWriter output)
        {
            if (thing.StackOrder == StackOrder.Ground)
            {
                output.Write((byte)ObdFlags.Ground);
                output.Write((ushort)thing.GroundSpeed);
            }
            else if (thing.StackOrder == StackOrder.Border)
            {
                output.Write((byte)ObdFlags.GroundBorder);
            }
            else if (thing.StackOrder == StackOrder.Bottom)
            {
                output.Write((byte)ObdFlags.OnBottom);
            }
            else if (thing.StackOrder == StackOrder.Top)
            {
                output.Write((byte)ObdFlags.OnTop);
            }

            if (thing.IsContainer)
            {
                output.Write((byte)ObdFlags.Container);
            }

            if (thing.Stackable)
            {
                output.Write((byte)ObdFlags.Stackable);
            }

            if (thing.ForceUse)
            {
                output.Write((byte)ObdFlags.ForceUse);
            }

            if (thing.MultiUse)
            {
                output.Write((byte)ObdFlags.MultiUse);
            }

            if (thing.Writable)
            {
                output.Write((byte)ObdFlags.Writable);
                output.Write((ushort)thing.MaxTextLength);
            }

            if (thing.WritableOnce)
            {
                output.Write((byte)ObdFlags.WritableOnce);
                output.Write((ushort)thing.MaxTextLength);
            }

            if (thing.IsFluidContainer)
            {
                output.Write((byte)ObdFlags.FluidContainer);
            }

            if (thing.IsFluid)
            {
                output.Write((byte)ObdFlags.Fluid);
            }

            if (thing.Unpassable)
            {
                output.Write((byte)ObdFlags.IsUnpassable);
            }

            if (thing.Unmovable)
            {
                output.Write((byte)ObdFlags.IsUnmovable);
            }

            if (thing.BlockMissiles)
            {
                output.Write((byte)ObdFlags.BlockMissiles);
            }

            if (thing.BlockPathfinder)
            {
                output.Write((byte)ObdFlags.BlockPathfinder);
            }

            if (thing.NoMoveAnimation)
            {
                output.Write((byte)ObdFlags.NoMoveAnimation);
            }

            if (thing.Pickupable)
            {
                output.Write((byte)ObdFlags.Pickupable);
            }

            if (thing.Hangable)
            {
                output.Write((byte)ObdFlags.Hangable);
            }

            if (thing.HookSouth)
            {
                output.Write((byte)ObdFlags.HookSouth);
            }

            if (thing.HookEast)
            {
                output.Write((byte)ObdFlags.HookEast);
            }

            if (thing.Rotatable)
            {
                output.Write((byte)ObdFlags.Rotatable);
            }

            if (thing.HasLight)
            {
                output.Write((byte)ObdFlags.HasLight);
                output.Write((ushort)thing.LightLevel);
                output.Write((ushort)thing.LightColor);
            }

            if (thing.DontHide)
            {
                output.Write((byte)ObdFlags.DontHide);
            }

            if (thing.Translucent)
            {
                output.Write((byte)ObdFlags.Translucent);
            }

            if (thing.HasOffset)
            {
                output.Write((byte)ObdFlags.HasOffset);
                output.Write((ushort)thing.OffsetX);
                output.Write((ushort)thing.OffsetY);
            }

            if (thing.HasElevation)
            {
                output.Write((byte)ObdFlags.HasElevation);
                output.Write((ushort)thing.Elevation);
            }

            if (thing.LyingObject)
            {
                output.Write((byte)ObdFlags.LyingObject);
            }

            if (thing.AnimateAlways)
            {
                output.Write((byte)ObdFlags.AnimateAlways);
            }

            if (thing.Minimap)
            {
                output.Write((byte)ObdFlags.Minimap);
                output.Write((ushort)thing.MinimapColor);
            }

            if (thing.IsLensHelp)
            {
                output.Write((byte)ObdFlags.LensHelp);
                output.Write((ushort)thing.LensHelp);
            }

            if (thing.FullGround)
            {
                output.Write((byte)ObdFlags.FullGround);
            }

            if (thing.IgnoreLook)
            {
                output.Write((byte)ObdFlags.IgnoreLook);
            }

            if (thing.IsCloth)
            {
                output.Write((byte)ObdFlags.Cloth);
                output.Write((ushort)thing.ClothSlot);
            }

            if (thing.IsMarketItem)
            {
                output.Write((byte)ObdFlags.Market);
                output.Write((ushort)thing.MarketCategory);
                output.Write((ushort)thing.MarketTradeAs);
                output.Write((ushort)thing.MarketShowAs);
                output.Write((ushort)thing.MarketName.Length);
                output.Write((string)thing.MarketName);
                output.Write((ushort)thing.MarketRestrictVocation);
                output.Write((ushort)thing.MarketRestrictLevel);
            }

            if (thing.HasAction)
            {
                output.Write((byte)ObdFlags.DefaultAction);
                output.Write((ushort)thing.DefaultAction);
            }

            if (thing.HasCharges)
            {
                output.Write((byte)ObdFlags.HasChanges);
            }

            if (thing.FloorChange)
            {
                output.Write((byte)ObdFlags.FloorChange);
            }

            if (thing.Usable)
            {
                output.Write((byte)ObdFlags.Usable);
            }

            // close flags
            output.Write((byte)ObdFlags.LastFlag);
            return true;
        }

        private static ThingData DecodeV1(BinaryReader reader)
        {
            reader.BaseStream.Position = 0;

            Console.WriteLine(reader.ReadUInt16());

            ushort nameLength = reader.ReadUInt16();
            byte[] buffer = reader.ReadBytes(nameLength);
            string categoryStr = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
            ThingCategory category = ThingCategory.Invalid;

            switch (categoryStr)
            {
                case "item":
                    category = ThingCategory.Item;
                    break;

                case "outfit":
                    category = ThingCategory.Outfit;
                    break;

                case "effect":
                    category = ThingCategory.Effect;
                    break;

                case "missile":
                    category = ThingCategory.Missile;
                    break;
            }

            ThingType thing = new ThingType(category);

            if (!ThingTypeSerializer.ReadProperties(thing, DatFormat.Format_1010, reader))
            {
                return null;
            }

            FrameGroup group = new FrameGroup();

            group.Width = reader.ReadByte();
            group.Height = reader.ReadByte();

            if (group.Width > 1 || group.Height > 1)
            {
                group.ExactSize = reader.ReadByte();
            }
            else
            {
                group.ExactSize = Sprite.DefaultSize;
            }

            group.Layers = reader.ReadByte();
            group.PatternX = reader.ReadByte();
            group.PatternY = reader.ReadByte();
            group.PatternZ = reader.ReadByte();
            group.Frames = reader.ReadByte();

            if (group.Frames > 1)
            {
                group.IsAnimation = true;
                group.AnimationMode = AnimationMode.Asynchronous;
                group.LoopCount = 0;
                group.StartFrame = 0;
                group.FrameDurations = new FrameDuration[group.Frames];

                for (byte i = 0; i < group.Frames; i++)
                {
                    group.FrameDurations[i] = new FrameDuration(category);
                }
            }

            int totalSprites = group.GetTotalSprites();
            if (totalSprites > 4096)
            {
                throw new Exception("The ThingData has more than 4096 sprites.");
            }

            group.SpriteIDs = new uint[totalSprites];
            SpriteGroup spriteGroup = new SpriteGroup();
            Sprite[] sprites = new Sprite[totalSprites];

            for (int i = 0; i < totalSprites; i++)
            {
                uint spriteID = reader.ReadUInt32();
                group.SpriteIDs[i] = spriteID;

                uint dataSize = reader.ReadUInt32();
                if (dataSize > Sprite.PixelsDataSize)
                {
                    throw new Exception("Invalid sprite data size.");
                }

                byte[] pixels = reader.ReadBytes((int)dataSize);

                Sprite sprite = new Sprite(spriteID, true);
                sprite.SetPixelsARGB(pixels);
                sprites[i] = sprite;
            }

            thing.SetFrameGroup(FrameGroupType.Default, group);
            spriteGroup.Add(FrameGroupType.Default, sprites);
            return new ThingData(thing, spriteGroup);
        }

        private static ThingData DecodeV2(BinaryReader reader)
        {
            throw new NotImplementedException();
        }

        private static ThingData DecodeV3(BinaryReader reader)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
