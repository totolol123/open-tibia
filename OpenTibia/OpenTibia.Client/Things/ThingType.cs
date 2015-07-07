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
using System;
using System.Collections.Generic;
using System.ComponentModel;
#endregion

namespace OpenTibia.Client.Things
{
    public enum ThingCategory : byte
    {
        Invalid = 0,
        Item = 1,
        Outfit = 2,
        Effect = 3,
        Missile = 4
    }

    public enum StackOrder : byte
    {
        Commom = 0,
        Ground = 1,
        Border = 2,
        Bottom = 3,
        Top = 4
    }

    public enum DefaultAction : byte
    {
        None = 0,
        Look = 1,
        Use = 2,
        Open = 3,
        AutowalkHighlight = 4
    }

    public class ThingType
    {
        #region Internal Properties

        internal Dictionary<FrameGroupType, FrameGroup> frameGroups;

        #endregion

        #region Constructor

        public ThingType(ushort id, ThingCategory category)
        {
            this.ID = id;
            this.Category = category;
            this.StackOrder = StackOrder.Commom;
            this.frameGroups = new Dictionary<FrameGroupType, FrameGroup>();
        }

        public ThingType(ThingCategory category) : this (0, category)
        {
            ////
        }

        #endregion

        #region Public Properties

        [Browsable(false)]
        public ushort ID { get; set; }

        [Browsable(false)]
        public ThingCategory Category { get; private set; }

        [DisplayName("Stack Order")]
        public StackOrder StackOrder { get; set; }

        [DisplayName("Ground Speed")]
        public ushort GroundSpeed { get; set; }

        [DisplayName("Is Container")]
        [Category("General Flags"), Description("...")]
        public bool IsContainer { get; set; }

        [DisplayName("Stackable")]
        [Category("General Flags"), Description("...")]
        public bool Stackable { get; set; }

        [DisplayName("ForceUse")]
        [Category("General Flags"), Description("...")]
        public bool ForceUse { get; set; }

        [DisplayName("MultiUse")]
        [Category("General Flags"), Description("...")]
        public bool MultiUse { get; set; }

        [DisplayName("Writable")]
        [Category("Write"), Description("...")]
        public bool Writable { get; set; }

        [DisplayName("Writable Once")]
        [Category("Write"), Description("...")]
        public bool WritableOnce { get; set; }

        [DisplayName("Maximum Text Length")]
        [Category("Write"), Description("...")]
        public ushort MaxTextLength { get; set; }

        [DisplayName("Is Fluid Container")]
        [Category("General Flags"), Description("...")]
        public bool IsFluidContainer { get; set; }

        [DisplayName("Is Fluid")]
        [Category("General Flags"), Description("...")]
        public bool IsFluid { get; set; }

        [Category("General Flags"), Description("...")]
        public bool Unpassable { get; set; }

        [Category("General Flags"), Description("...")]
        public bool Unmovable { get; set; }

        [DisplayName("Block Missiles")]
        [Category("General Flags"), Description("...")]
        public bool BlockMissiles { get; set; }

        [DisplayName("Block Pathfinder")]
        [Category("General Flags"), Description("...")]
        public bool BlockPathfinder { get; set; }

        [DisplayName("No Move Animation")]
        [Category("General Flags"), Description("...")]
        public bool NoMoveAnimation { get; set; }

        [DisplayName("Pickupable")]
        [Category("General Flags"), Description("...")]
        public bool Pickupable { get; set; }

        [DisplayName("Hangable")]
        [Category("General Flags"), Description("...")]
        public bool Hangable { get; set; }

        [DisplayName("HookEast")]
        [Category("General Flags"), Description("...")]
        public bool HookEast { get; set; }

        [DisplayName("HookSouth")]
        [Category("General Flags"), Description("...")]
        public bool HookSouth { get; set; }

        [DisplayName("Rotatable")]
        [Category("General Flags"), Description("...")]
        public bool Rotatable { get; set; }

        [DisplayName("Has Light")]
        [Category("Light"), Description("...")]
        public bool HasLight { get; set; }

        [DisplayName("Intensity")]
        [Category("Light"), Description("...")]
        public ushort LightLevel { get; set; }

        [DisplayName("Color")]
        [Category("Light"), Description("...")]
        public ushort LightColor { get; set; }

        [DisplayName("Don't Hide")]
        [Category("General Flags"), Description("...")]
        public bool DontHide { get; set; }

        [DisplayName("Translucent")]
        [Category("General Flags"), Description("...")]
        public bool Translucent { get; set; }

        [DisplayName("Has Offset")]
        [Category("Offset"), Description("...")]
        public bool HasOffset { get; set; }

        [DisplayName("Offset X")]
        [Category("Offset"), Description("...")]
        public ushort OffsetX { get; set; }

        [DisplayName("Offset Y")]
        [Category("Offset"), Description("...")]
        public ushort OffsetY { get; set; }

        [DisplayName("Has Elevation")]
        [Category("Elevation"), Description("...")]
        public bool HasElevation { get; set; }

        [DisplayName("Height")]
        [Category("Elevation"), Description("...")]
        public ushort Elevation { get; set; }

        [DisplayName("LyingObject")]
        [Category("General Flags"), Description("...")]
        public bool LyingObject { get; set; }

        [DisplayName("Is Minimap")]
        [Category("Minimap"), Description("...")]
        public bool Minimap { get; set; }

        [DisplayName("Minimap Color")]
        [Category("Minimap"), Description("...")]
        public ushort MinimapColor { get; set; }

        [DisplayName("AnimateAlways")]
        [Category("Minimap"), Description("...")]
        public bool AnimateAlways { get; set; }

        [DisplayName("Is Lens Help")]
        [Category("Lens Help"), Description("...")]
        public bool IsLensHelp { get; set; }

        [DisplayName("Lens Help")]
        [Category("Lens Help"), Description("...")]
        public ushort LensHelp { get; set; }

        [DisplayName("Full Ground")]
        [Category("General Flags"), Description("...")]
        public bool FullGround { get; set; }

        [DisplayName("Ignore Look")]
        [Category("General Flags"), Description("...")]
        public bool IgnoreLook { get; set; }

        [DisplayName("Is Cloth")]
        [Category("Cloth"), Description("...")]
        public bool IsCloth { get; set; }

        [DisplayName("Cloth Slot")]
        [Category("Cloth"), Description("...")]
        public ClothSlot ClothSlot { get; set; }

        [DisplayName("Is Market Item")]
        [Category("Market"), Description("...")]
        public bool IsMarketItem { get; set; }

        [DisplayName("Category")]
        [Category("Market"), Description("...")]
        public MarketCategory MarketCategory { get; set; }

        [DisplayName("Trade As")]
        [Category("Market"), Description("...")]
        public ushort MarketTradeAs { get; set; }

        [DisplayName("Show As")]
        [Category("Market"), Description("...")]
        public ushort MarketShowAs { get; set; }

        [DisplayName("Name")]
        [Category("Market"), Description("...")]
        public string MarketName { get; set; }

        [DisplayName("Vocation")]
        [Category("Market"), Description("...")]
        public ushort MarketRestrictVocation { get; set; }

        [DisplayName("Restrict Level")]
        [Category("Market"), Description("...")]
        public ushort MarketRestrictLevel { get; set; }

        [DisplayName("Has Action")]
        [Category("Action"), Description("...")]
        public bool HasAction { get; set; }

        [DisplayName("Default Action")]
        [Category("Action"), Description("...")]
        public DefaultAction DefaultAction { get; set; }

        [DisplayName("Default Action")]
        [Category("General Flags"), Description("...")]
        public bool Usable { get; set; }

        [DisplayName("Has Charges")]
        [Category("General Flags"), Description("...")]
        public bool HasCharges { get; set; }

        [DisplayName("Floor Change")]
        [Category("General Flags"), Description("...")]
        public bool FloorChange { get; set; }

        [Browsable(false)]
        public byte FrameGroupCount
        {
            get
            {
                return (byte)this.frameGroups.Count;
            }
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            if (this.MarketName != null)
            {
                return this.ID.ToString() + " - " + this.MarketName;
            }

            return this.ID.ToString();
        }

        public FrameGroup GetFrameGroup(FrameGroupType groupType)
        {
            if (this.frameGroups.ContainsKey(groupType))
            {
                return this.frameGroups[groupType];
            }

            return null;
        }

        public FrameGroup SetFrameGroup(FrameGroupType groupType, FrameGroup group)
        {
            if (groupType == FrameGroupType.Walking && (!this.frameGroups.ContainsKey(FrameGroupType.Default) || this.frameGroups.Count == 0))
            {
                this.frameGroups.Add(FrameGroupType.Default, group);
            }

            if (!this.frameGroups.ContainsKey(groupType))
            {
                this.frameGroups.Add(groupType, group);
            }

            return group;
        }

        #endregion

        #region Public Static Methods

        public static ThingType Create(ushort id, ThingCategory category)
        {
            if (category == ThingCategory.Invalid)
            {
                throw new ArgumentException("Invalid category.");
            }

            ThingType thing = new ThingType(id, category);

            if (category == ThingCategory.Outfit)
            {
                for (int i = 0; i < 2; i++)
                {
                    FrameGroup group = FrameGroup.Create();
                    group.PatternX = 4; // directions
                    group.Frames = 3;   // animations
                    group.IsAnimation = true;
                    group.SpriteIDs = new uint[group.GetTotalSprites()];
                    group.FrameDurations = new FrameDuration[group.Frames];

                    for (int f = 0; f < group.Frames; f++)
                    {
                        group.FrameDurations[f] = new FrameDuration(category);
                    }

                    thing.SetFrameGroup((FrameGroupType)i, group);
                }
            }
            else
            {
                FrameGroup group = FrameGroup.Create();

                if (category == ThingCategory.Missile)
                {
                    group.PatternX = 3;
                    group.PatternY = 3;
                    group.SpriteIDs = new uint[group.GetTotalSprites()];
                }

                thing.SetFrameGroup(FrameGroupType.Default, group);
            }

            return thing;
        }

        public static ThingType ToSingleFrameGroup(ThingType thing)
        {
            if (thing.Category != ThingCategory.Outfit || thing.FrameGroupCount != 2)
            {
                return thing;
            }

            FrameGroup walkingFrameGroup = thing.GetFrameGroup(FrameGroupType.Walking);
            FrameGroup newGroup = walkingFrameGroup.Clone();

            if (walkingFrameGroup.Frames > 1)
            {
                newGroup.Frames = (byte)(newGroup.Frames + 1);
                newGroup.SpriteIDs = new uint[newGroup.GetTotalSprites()];
                newGroup.IsAnimation = true;
                newGroup.FrameDurations = new FrameDuration[newGroup.Frames];

                for (int i = 0; i < newGroup.Frames; i++)
                {
                    if (newGroup.FrameDurations[i] != null)
                    {
                        newGroup.FrameDurations[i] = newGroup.FrameDurations[i];
                    }
                    else
                    {
                        newGroup.FrameDurations[i] = new FrameDuration(ThingCategory.Outfit);
                    }
                }
            }

            for (byte k = 0; k < thing.FrameGroupCount; k++)
            {
                FrameGroup group = thing.GetFrameGroup((FrameGroupType)k);

                for (byte f = 0; f < group.Frames; f++)
                {
                    for (byte z = 0; z < group.PatternZ; z++)
                    {
                        for (byte y = 0; y < group.PatternY; y++)
                        {
                            for (byte x = 0; x < group.PatternX; x++)
                            {
                                for (byte l = 0; l < group.Layers; l++)
                                {
                                    for (byte w = 0; w < group.Width; w++)
                                    {
                                        for (byte h = 0; h < group.Height; h++)
                                        {
                                            int i = group.GetSpriteIndex(w, h, l, x, y, z, f);
                                            int ni = 0;

                                            if (k == (byte)FrameGroupType.Default && f == 0)
                                            {
                                                ni = newGroup.GetSpriteIndex(w, h, l, x, y, z, f);
                                            }
                                            else if (k == (byte)FrameGroupType.Walking)
                                            {
                                                ni = newGroup.GetSpriteIndex(w, h, l, x, y, z, f + 1);
                                            }

                                            newGroup.SpriteIDs[ni] = group.SpriteIDs[i];
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            thing.frameGroups = new Dictionary<FrameGroupType, FrameGroup>();
            thing.frameGroups.Add(FrameGroupType.Default, newGroup);
            return thing;
        }

        #endregion
    }
}
