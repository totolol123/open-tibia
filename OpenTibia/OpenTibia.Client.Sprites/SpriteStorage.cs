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
using System.IO;
using System.Collections.Generic;
using OpenTibia.Client.Things;
#endregion

namespace OpenTibia.Client.Sprites
{
    public delegate void SpriteListChanged(uint[] changedIds);

    public class SpriteStorage
    {
        #region Constants

        private const byte HeaderU16 = 6;
        private const byte HeaderU32 = 8;

        #endregion

        #region Private Properties

        private FileStream stream;
        private BinaryReader reader;
        private Dictionary<uint, Sprite> sprites;
        private uint rawSpriteCount;
        private byte headSize;
        private Sprite blankSprite;

        #endregion

        #region Constructor
        
        public SpriteStorage()
        {
            this.sprites = new Dictionary<uint, Sprite>();
        }

        #endregion

        #region Events

        public event EventHandler StorageLoaded;

        public event SpriteListChanged StorageChanged;

        public event EventHandler StorageCompiled;

        public event EventHandler StorageUnloaded;

        #endregion

        #region Public Properties

        public string FilePath { get; private set; }

        public OpenTibia.Core.Version Version { get; private set; }

        public uint Count { get; private set; }

        public bool Extended { get; private set; }

        public bool IsTemporary
        {
            get
            {
                return this.Loaded && this.FilePath == null;
            }
        }

        public bool Transparency { get; private set; }

        public bool Changed { get; private set; }

        public bool Loaded { get; private set; }

        #endregion

        #region Public Methods

        public bool Create(OpenTibia.Core.Version version, ClientFeature features)
        {
            if (this.Loaded && !this.Unload())
            {
                return false;
            }

            this.Version = version;
            this.Extended = (features & ClientFeature.Extended) == ClientFeature.Extended || version.Value >= (ushort)DatFormat.Format_960;
            this.Transparency = (features & ClientFeature.Transparency) == ClientFeature.Transparency;
            this.headSize = this.Extended ? HeaderU32 : HeaderU16;
            this.blankSprite = new Sprite(0, this.Transparency);
            this.sprites.Add(1, new Sprite(1, this.Transparency));
            this.rawSpriteCount = 0;
            this.Count = 1;
            this.Changed = false;
            this.Loaded = true;

            if (this.StorageLoaded != null)
            {
                this.StorageLoaded(this, new EventArgs());
            }

            return true;
        }

        public bool Create(OpenTibia.Core.Version version)
        {
            return this.Create(version, ClientFeature.None);
        }

        public bool Load(string path, OpenTibia.Core.Version version, ClientFeature features)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            if (version == null)
            {
                throw new ArgumentNullException("version");
            }

            if (!File.Exists(path))
            {
                string message = "File not found: {0}"; // TODO: ResourceManager.GetString("Exception.FileNotFound");
                throw new FileNotFoundException(string.Format(message, path), "path");
            }

            if (this.Loaded && !this.Unload())
            {
                return false;
            }

            this.stream = new FileStream(path, FileMode.Open);
            this.reader = new BinaryReader(this.stream);

            uint signature = this.reader.ReadUInt32();
            if (signature != version.SprSignature)
            {
                string message = "Invalid SPR signature. Expected signature is {0:X} and loaded signature is {1:X}.";
                throw new Exception(string.Format(message, version.SprSignature, signature));
            }

            bool extended = (features & ClientFeature.Extended) == ClientFeature.Extended || version.Value >= (ushort)DatFormat.Format_960;
            if (extended)
            {
                this.headSize = HeaderU32;
                this.rawSpriteCount = this.reader.ReadUInt32();
            }
            else
            {
                this.headSize = HeaderU16;
                this.rawSpriteCount = this.reader.ReadUInt16();
            }
            
            this.FilePath = path;
            this.Version = version;
            this.Extended = extended;
            this.Transparency = (features & ClientFeature.Transparency) == ClientFeature.Transparency;
            this.Count = this.rawSpriteCount;
            this.blankSprite = new Sprite(0, this.Transparency);
            this.Changed = false;
            this.Loaded = true;

            if (this.StorageLoaded != null)
            {
                this.StorageLoaded(this, new EventArgs());
            }

            return true;
        }

        public bool Load(string path, OpenTibia.Core.Version version)
        {
            return this.Load(path, version, ClientFeature.None);
        }

        public Sprite AddSprite(Sprite sprite)
        {
            if (sprite == null)
            {
                return null;
            }

            uint id = ++this.Count;

            sprite.ID = id;
            sprite.Transparent = this.Transparency;

            this.sprites.Add(id, sprite);
            this.Changed = true;

            if (this.StorageChanged != null)
            {
                this.StorageChanged(new uint[] { id });
            }

            return sprite;
        }

        public Sprite AddSprite(Bitmap bitmap)
        {
            if (bitmap == null || bitmap.Width != Sprite.DefaultSize || bitmap.Height != Sprite.DefaultSize)
            {
                return null;
            }

            uint id = ++this.Count;
            Sprite sprite = new Sprite(id, this.Transparency);
            sprite.SetBitmap(bitmap);
            this.sprites.Add(id, sprite);
            this.Changed = true;

            if (this.StorageChanged != null)
            {
                this.StorageChanged(new uint[] { id });
            }

            return sprite;
        }

        public Sprite ReplaceSprite(Sprite newSprite, uint replaceId)
        {
            if (newSprite == null || replaceId == 0 || replaceId > this.Count)
            {
                return null;
            }

            newSprite.ID = replaceId;
            newSprite.Transparent = this.Transparency;

            Sprite replacedSprite = null;

            if (this.sprites.ContainsKey(replaceId))
            {
                replacedSprite = this.sprites[replaceId];
                this.sprites[replaceId] = newSprite;
            }
            else
            {
                replacedSprite = this.ReadSprite(replaceId);
                this.sprites.Add(replaceId, newSprite);
            }

            this.Changed = true;

            if (this.StorageChanged != null)
            {
                this.StorageChanged(new uint[] { replaceId });
            }

            return replacedSprite;
        }

        public Sprite ReplaceSprite(Bitmap newBitmap, uint replaceId)
        {
            if (newBitmap == null || newBitmap.Width != Sprite.DefaultSize || newBitmap.Height != Sprite.DefaultSize || replaceId == 0 || replaceId > this.Count)
            {
                return null;
            }

            Sprite newSprite = new Sprite(replaceId, this.Transparency);
            Sprite replacedSprite = null;

            if (this.sprites.ContainsKey(replaceId))
            {
                replacedSprite = this.sprites[replaceId];
                this.sprites[replaceId] = newSprite;
            }
            else
            {
                replacedSprite = this.ReadSprite(replaceId);
                this.sprites.Add(replaceId, newSprite);
            }

            this.Changed = true;

            if (this.StorageChanged != null)
            {
                this.StorageChanged(new uint[] { replaceId });
            }

            return replacedSprite;
        }

        public Sprite RemoveSprite(uint id)
        {
            if (id == 0 || id > this.Count)
            {
                return null;
            }

            Sprite removedSprite = this.GetSprite(id);

            if (id == this.Count && id != 1)
            {
                if (this.sprites.ContainsKey(id))
                {
                    this.sprites.Remove(id);
                }

                this.Count--;
            }
            else
            {
                if (this.sprites.ContainsKey(id))
                {
                    this.sprites[id] = new Sprite(id, this.Transparency);
                }
                else
                {
                    this.sprites.Add(id, new Sprite(id, this.Transparency));
                }
            }

            return removedSprite;
        }

        public bool HasSpriteID(uint id)
        {
            return id <= this.Count;
        }

        public Sprite GetSprite(uint id)
        {
            if (id <= this.Count)
            {
                if (id == 0)
                {
                    return this.blankSprite;
                }

                if (this.sprites.ContainsKey(id))
                {
                    return this.sprites[id];
                }

                return this.ReadSprite(id);
            }

            return null;
        }

        public Bitmap GetSpriteBitmap(uint id)
        {
            if (id <= this.Count)
            {
                return this.ReadSprite(id).GetBitmap();
            }

            return null;
        }

        public bool Save()
        {
            if (!this.IsTemporary && this.Changed)
            {
                return this.SaveAs(this.FilePath, this.Version);
            }

            return false;
        }

        public bool SaveAs(string path, OpenTibia.Core.Version version, ClientFeature features)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            string tmpPath = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + ".tmp");
            bool extended = (features & ClientFeature.Extended) == ClientFeature.Extended || version.Value >= (ushort)DatFormat.Format_960;
            byte headSize = 0;
            uint count = 0;

            try
            {
                using (BinaryWriter writer = new BinaryWriter(new FileStream(tmpPath, FileMode.Create)))
                {
                    // write the signature
                    writer.Write((uint)version.SprSignature);

                    // write the sprite count
                    if (extended)
                    {
                        count = this.Count;
                        headSize = HeaderU32;
                        writer.Write((uint)count);
                    }
                    else
                    {
                        count = this.Count >= 0xFFFE ? 0xFFFE : this.Count;
                        headSize = HeaderU16;
                        writer.Write((ushort)count);
                    }

                    int addressPosition = headSize;
                    int address = (int)((count * 4) + headSize);
                    byte[] bytes = null;

                    for (uint id = 1; id <= count; id++)
                    {
                        writer.Seek(addressPosition, SeekOrigin.Begin);

                        if (this.sprites.ContainsKey(id))
                        {
                            Sprite sprite = this.sprites[id];

                            if (sprite.Length == 0)
                            {
                                // write address 0
                                writer.Write((uint)0);
                                writer.Seek(address, SeekOrigin.Begin);
                            }
                            else
                            {
                                bytes = sprite.CompressedPixels;

                                // write address
                                writer.Write((uint)address);
                                writer.Seek(address, SeekOrigin.Begin);

                                // write colorkey
                                writer.Write((byte)0xFF); // red
                                writer.Write((byte)0x00); // blue
                                writer.Write((byte)0xFF); // green

                                // write sprite data size
                                writer.Write((short)bytes.Length);

                                if (bytes.Length > 0)
                                {
                                    writer.Write(bytes);
                                }
                            }
                        }
                        else if (id <= this.rawSpriteCount)
                        {
                            this.stream.Seek(((id - 1) * 4) + this.headSize, SeekOrigin.Begin);

                            uint spriteAddress = this.reader.ReadUInt32();

                            if (spriteAddress == 0)
                            {
                                // write address 0
                                writer.Write((uint)0);
                                writer.Seek(address, SeekOrigin.Begin);
                            }
                            else
                            {
                                // write address
                                writer.Write((uint)address);
                                writer.Seek(address, SeekOrigin.Begin);

                                // write colorkey
                                writer.Write((byte)0xFF); // red
                                writer.Write((byte)0x00); // blue
                                writer.Write((byte)0xFF); // green

                                // sets the position to the pixel data size.
                                this.stream.Seek(spriteAddress + 3, SeekOrigin.Begin);

                                // read the data size from the current stream
                                ushort pixelDataSize = this.reader.ReadUInt16();

                                // write sprite data size
                                writer.Write(pixelDataSize);

                                // write sprite compressed pixels
                                if (pixelDataSize != 0)
                                {
                                    bytes = this.reader.ReadBytes(pixelDataSize);
                                    writer.Write(bytes);
                                }
                            }
                        }

                        address = (int)writer.BaseStream.Position;
                        addressPosition += 4;
                    }

                    writer.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return false;
            }

            if (this.StorageCompiled != null)
            {
                this.StorageCompiled(this, new EventArgs());
            }

            return true;
        }

        public bool SaveAs(string path, OpenTibia.Core.Version version)
        {
            return this.SaveAs(path, version, ClientFeature.None);
        }

        public bool Unload()
        {
            if (this.Loaded)
            {
                if (this.stream != null)
                {
                    this.stream.Dispose();
                    this.stream = null;
                    this.reader = null;
                }

                this.FilePath = null;
                this.sprites.Clear();
                this.rawSpriteCount = 0;
                this.Count = 0;
                this.Extended = false;
                this.Transparency = false;
                this.Changed = false;
                this.Loaded = false;

                if (this.StorageUnloaded != null)
                {
                    this.StorageUnloaded(this, new EventArgs());
                }
            }
            
            return true;
        }

        #endregion

        #region Private Methods

        private Sprite ReadSprite(uint id)
        {
            try
            {
                if (id > this.rawSpriteCount)
                {
                    return null;
                }

                // O id 1 no arquivo spr é o endereço 0, então subtraímos
                // o id fornecido e mutiplicamos pela quantidade de bytes
                // de cada endereço.
                this.stream.Position = ((id - 1) * 4) + this.headSize;

                // Lê o endereço do sprite.
                uint spriteAddress = this.reader.ReadUInt32();

                // O endereço 0 representa um sprite em branco,
                // então retornamos um sprite sem a leitura dos dados.
                if (spriteAddress == 0)
                {
                    return new Sprite(id, this.Transparency);
                }

                // Posiciona o stream para o endereço do sprite.
                this.stream.Position = spriteAddress;

                // Leitura da cor magenta usada como referência
                // para remover o fundo do sprite.
                this.reader.ReadByte(); // red key color
                this.reader.ReadByte(); // green key color
                this.reader.ReadByte(); // blue key color

                Sprite sprite = new Sprite(id, this.Transparency);

                // O tamanho dos pixels compressados.
                ushort pixelDataSize = this.reader.ReadUInt16();
                if (pixelDataSize != 0)
                {
                    sprite.CompressedPixels = this.reader.ReadBytes(pixelDataSize);
                }

                return sprite;
            }
            catch (Exception ex)
            {
                // TODO ErrorManager.ShowError(ex);
            }

            return null;
        }

        #endregion
    }
}
