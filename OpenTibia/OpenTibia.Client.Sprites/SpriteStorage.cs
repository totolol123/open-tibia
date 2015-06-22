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
using OpenTibia.Core;
using System.ComponentModel;
using System.Threading;
#endregion

namespace OpenTibia.Client.Sprites
{
    public class SpriteListChangedArgs
    {
        #region Constructor
        
        public SpriteListChangedArgs(Sprite[] changedSprites, StorageChangeType changeType)
        {
            this.ChangedSprites = changedSprites;
            this.ChangeType = changeType;
        }

        #endregion

        #region Public Properties

        public Sprite[] ChangedSprites { get; private set; }

        public StorageChangeType ChangeType { get; private set; }

        #endregion
    }

    public delegate void SpriteListChangedHandler(object sender, SpriteListChangedArgs e);

    public delegate void ProgressHandler(object sender, int percentage);

    public class SpriteStorage : IStorage
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
        private BackgroundWorker worker;
        private string newPath;
        private string tmpPath;

        #endregion

        #region Constructor
        
        public SpriteStorage()
        {
            this.sprites = new Dictionary<uint, Sprite>();
            this.worker = new BackgroundWorker();
            this.worker.WorkerSupportsCancellation = true;
            this.worker.WorkerReportsProgress = true;
            this.worker.DoWork += new DoWorkEventHandler(this.DoWork_Handler);
            this.worker.ProgressChanged += new ProgressChangedEventHandler(this.WorkerProgressChanged_Handler);
            this.worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(this.RunWorkerCompleted_Handler);
        }

        #endregion

        #region Events

        public event EventHandler StorageLoaded;

        public event SpriteListChangedHandler StorageChanged;

        public event EventHandler StorageCompiled;

        public event EventHandler StorageCompilationCanceled;

        public event EventHandler StorageUnloaded;

        public event ProgressHandler ProgressChanged;

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

        public bool Compiling { get; private set; }

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
            this.Compiling = false;

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

        public bool AddSprite(Sprite sprite)
        {
            if (sprite == null)
            {
                return false;
            }

            uint id = ++this.Count;

            sprite.ID = id;
            sprite.Transparent = this.Transparency;

            this.sprites.Add(id, sprite);
            this.Changed = true;

            if (this.StorageChanged != null)
            {
                this.StorageChanged(this, new SpriteListChangedArgs(new Sprite[] { sprite }, StorageChangeType.Add));
            }

            return true;
        }

        public bool AddSprite(Bitmap bitmap)
        {
            if (bitmap == null || bitmap.Width != Sprite.DefaultSize || bitmap.Height != Sprite.DefaultSize)
            {
                return false;
            }

            uint id = ++this.Count;
            Sprite sprite = new Sprite(id, this.Transparency);
            sprite.SetBitmap(bitmap);
            this.sprites.Add(id, sprite);
            this.Changed = true;

            if (this.StorageChanged != null)
            {
                this.StorageChanged(this, new SpriteListChangedArgs(new Sprite[] { sprite }, StorageChangeType.Add));
            }

            return true;
        }

        public bool AddSprites(Sprite[] sprites)
        {
            if (sprites == null || sprites.Length == 0)
            {
                return false;
            }

            List<Sprite> changedSprites = new List<Sprite>();

            foreach (Sprite sprite in sprites)
            {
                if (sprite == null)
                {
                    continue;
                }

                uint id = ++this.Count;
                sprite.ID = id;
                sprite.Transparent = this.Transparency;
                this.sprites.Add(id, sprite);
                changedSprites.Add(sprite);
            }

            if (changedSprites.Count != 0)
            {
                this.Changed = true;

                if (this.StorageChanged != null)
                {
                    this.StorageChanged(this, new SpriteListChangedArgs(changedSprites.ToArray(), StorageChangeType.Add));
                }

                return true;
            }

            return false;
        }

        public bool AddSprites(Bitmap[] sprites)
        {
            if (sprites == null || sprites.Length == 0)
            {
                return false;
            }

            List<Sprite> changedSprites = new List<Sprite>();

            foreach (Bitmap bitmap in sprites)
            {
                if (bitmap == null || bitmap.Width != Sprite.DefaultSize || bitmap.Height != Sprite.DefaultSize)
                {
                    continue;
                }

                uint id = ++this.Count;
                Sprite sprite = new Sprite(id, this.Transparency);
                this.sprites.Add(id, sprite);
                changedSprites.Add(sprite);
            }

            if (changedSprites.Count != 0)
            {
                this.Changed = true;

                if (this.StorageChanged != null)
                {
                    this.StorageChanged(this, new SpriteListChangedArgs(changedSprites.ToArray(), StorageChangeType.Add));
                }

                return true;
            }

            return false;
        }

        public bool ReplaceSprite(Sprite newSprite, uint replaceId)
        {
            if (newSprite == null || replaceId == 0 || replaceId > this.Count)
            {
                return false;
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
                this.StorageChanged(this, new SpriteListChangedArgs(new Sprite[] { replacedSprite }, StorageChangeType.Replace));
            }

            return true;
        }

        public bool ReplaceSprite(Sprite newSprite)
        {
            if (newSprite != null)
            {
                return this.ReplaceSprite(newSprite, newSprite.ID);
            }

            return false;
        }

        public bool ReplaceSprite(Bitmap newBitmap, uint replaceId)
        {
            if (newBitmap == null || newBitmap.Width != Sprite.DefaultSize || newBitmap.Height != Sprite.DefaultSize || replaceId == 0 || replaceId > this.Count)
            {
                return false;
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
                this.StorageChanged(this, new SpriteListChangedArgs(new Sprite[] { replacedSprite }, StorageChangeType.Replace));
            }

            return true;
        }

        public bool ReplaceSprites(Sprite[] newSprites)
        {
            if (newSprites == null || newSprites.Length == 0)
            {
                return false;
            }

            List<Sprite> changedSprites = new List<Sprite>();

            foreach (Sprite sprite in newSprites)
            {
                if (sprite == null || sprite.ID == 0 || sprite.ID > this.Count)
                {
                    continue;
                }

                uint id = sprite.ID;
                Sprite replacedSprite = null;

                sprite.Transparent = this.Transparency;

                if (this.sprites.ContainsKey(id))
                {
                    replacedSprite = this.sprites[id];
                    this.sprites[id] = sprite;
                }
                else
                {
                    replacedSprite = this.ReadSprite(id);
                    this.sprites.Add(id, sprite);
                }

                changedSprites.Add(replacedSprite);
            }

            if (changedSprites.Count != 0)
            {
                this.Changed = true;

                if (this.StorageChanged != null)
                {
                    this.StorageChanged(this, new SpriteListChangedArgs(changedSprites.ToArray(), StorageChangeType.Replace));
                }

                return true;
            }

            return false;
        }

        public bool RemoveSprite(uint id)
        {
            if (id == 0 || id > this.Count)
            {
                return false;
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

            this.Changed = true;

            if (this.StorageChanged != null)
            {
                this.StorageChanged(this, new SpriteListChangedArgs(new Sprite[] { removedSprite }, StorageChangeType.Remove));
            }

            return true;
        }

        public bool RemoveSprites(uint[] ids)
        {
            if (ids == null || ids.Length == 0)
            {
                return false;
            }

            List<Sprite> changedSprites = new List<Sprite>();

            for (int i = 0; i < ids.Length; i++)
            {
                uint id = ids[i];

                if (id == 0 || id > this.Count)
                {
                    continue;
                }

                changedSprites.Add(this.GetSprite(id));

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
            }

            if (changedSprites.Count != 0)
            {
                this.Changed = true;

                if (this.StorageChanged != null)
                {
                    this.StorageChanged(this, new SpriteListChangedArgs(changedSprites.ToArray(), StorageChangeType.Remove));
                }
            }

            return false;
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

        public Sprite GetSprite(int id)
        {
            if (id >= 0)
            {
                return this.GetSprite((uint)id);
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
                return this.Save(this.FilePath);
            }

            return false;
        }

        public bool Save(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            if (!this.Loaded || this.Compiling)
            {
                return false;
            }

            if (!this.Changed)
            {
                //  only copy the content and reload if nothing has changed.
                if (this.FilePath != null && !path.Equals(this.FilePath))
                {
                    File.Copy(this.FilePath, path, true);

                    if (!this.Reload(this.FilePath, path))
                    {
                        return false;
                    }

                    if (this.StorageCompiled != null)
                    {
                        this.StorageCompiled(this, new EventArgs());
                    }
                }

                return true;
            }

            this.newPath = path;
            this.tmpPath = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + ".tmp");
            this.worker.RunWorkerAsync();
            this.Compiling = true;
            return true;
        }

        public bool Cancel()
        {
            if (this.Compiling)
            {
                this.worker.CancelAsync();
                this.Compiling = false;
                return true;
            }

            return false;
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
                this.Compiling = false;
                this.newPath = null;
                this.tmpPath = null;

                if (this.StorageUnloaded != null)
                {
                    this.StorageUnloaded(this, new EventArgs());
                }
            }
            
            return true;
        }

        #endregion

        #region Private Methods

        private bool Reload(string oldPath, string newPath)
        {
            if (!File.Exists(oldPath))
            {
                return false;
            }

            try
            {
                if (this.reader != null)
                {
                    this.reader.Dispose();
                    this.reader = null;
                    this.stream = null;
                }

                if (File.Exists(newPath))
                {
                    File.Delete(newPath);
                }

                File.Move(oldPath, newPath);

                this.FilePath = newPath;
                this.stream = new FileStream(newPath, FileMode.Open);
                this.reader = new BinaryReader(this.stream);
                this.sprites.Clear();
                this.rawSpriteCount = this.Extended ? this.reader.ReadUInt32() : this.reader.ReadUInt16();
                this.Count = this.rawSpriteCount;
                this.Changed = false;
                this.Loaded = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return false;
            }

            return true;
        }

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
            catch /*(Exception ex)*/
            {
                // TODO ErrorManager.ShowError(ex);
            }

            return null;
        }

        #endregion

        #region Event Handlers

        private void DoWork_Handler(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            using (BinaryWriter writer = new BinaryWriter(new FileStream(this.tmpPath, FileMode.Create)))
            {
                uint count = 0;

                // write the signature
                writer.Write((uint)this.Version.SprSignature);

                // write the sprite count
                if (this.Extended)
                {
                    count = this.Count;
                    writer.Write((uint)count);
                }
                else
                {
                    count = this.Count >= 0xFFFE ? 0xFFFE : this.Count;
                    writer.Write((ushort)count);
                }

                int addressPosition = headSize;
                int address = (int)((count * 4) + headSize);
                byte[] bytes = null;

                for (uint id = 1; id <= count; id++)
                {
                    if (worker.CancellationPending)
                    {
                        e.Cancel = true;
                        break;
                    }

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

                    if ((id % 500) == 0)
                    {
                        Thread.Sleep(10);
                        worker.ReportProgress((int)((id * 100) / count));
                    }
                }

                writer.Close();
            }
        }

        private void WorkerProgressChanged_Handler(object sender, ProgressChangedEventArgs e)
        {
            if (this.ProgressChanged != null)
            {
                this.ProgressChanged(this, e.ProgressPercentage);
            }
        }

        private void RunWorkerCompleted_Handler(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!e.Cancelled && this.Reload(this.tmpPath, this.newPath))
            {
                this.tmpPath = null;
                this.newPath = null;

                if (this.StorageCompiled != null)
                {
                    this.StorageCompiled(this, new EventArgs());
                }

                if (this.ProgressChanged != null)
                {
                    this.ProgressChanged(this, 100);
                }
            }
            else if (File.Exists(tmpPath))
            {
                File.Delete(tmpPath);
                this.tmpPath = null;
                this.newPath = null;

                if (this.ProgressChanged != null)
                {
                    this.ProgressChanged(this, 0);
                }
            }

            if (e.Cancelled && this.StorageCompilationCanceled != null)
            {
                this.StorageCompilationCanceled(this, new EventArgs());
            }
        }

        #endregion
    }
}
