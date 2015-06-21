### Open Tibia Framework


Loading and Compiling a SPR file
---

``` C#
// creates a Version 10.79.
OpenTibia.Core.Version version = new OpenTibia.Core.Version(1079, "Client 10.79", 0x3A71, 0x557A5E34, 0);

// the path to the spr file.
string path = @"C:\Clients\10.79\Tibia.spr";

// creates a SpriteStorage instance.
OpenTibia.Client.Sprites.SpriteStorage sprites = new OpenTibia.Client.Sprites.SpriteStorage();

// loads the spr file.
sprites.Load(path, version);

// gets a sprite from the storage
OpenTibia.Client.Sprites.Sprite sprite = sprites.GetSprite(100);

// adding a sprite.
sprites.AddSprite(new OpenTibia.Client.Sprites.Sprite());

 // replacing a sprite.
sprites.ReplaceSprite(new OpenTibia.Client.Sprites.Sprite(), 12);

// removing a sprite.
sprites.RemoveSprite(10);

// compiles the spr file.
sprites.Save();
```