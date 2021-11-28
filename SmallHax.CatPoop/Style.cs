using SFML.Graphics;
using SmallHax.SfmlExtensions;
using SmallHax.SfmlExtensions.TileMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmallHax.CatPoop
{
    public class Style
    {
        public Texture TileTexture { get; }
        public Texture FrameTexture { get; }
        public Texture Font { get; }
        public string TileSetName { get; }
        public Dictionary<string, Tileset> Tilesets { get; private set; }
        public ConsoleCharacter SidebarBrush { get; }
        public ConsoleCharacter SidebarFrameBrush { get; }
        public Color[] Colors { get; } = new Color[] { Color.Red, Color.Green, Color.Blue, new Color(255, 165, 0), Color.Magenta };

        public Style()
        {
            TileTexture = new Texture("Data/Images/poop.png");
            FrameTexture = new Texture("Data/Images/frame.png");
            Font = new Texture("Data/Fonts/fat-font-sheet-1.png");
            var tileset = new Tileset(Font, 8, 10);
            var utf8TileMapper = new Utf8TileMapper();
            tileset.SetTileMapper(utf8TileMapper);
            TileSetName = "fat-font";
            Tilesets = new Dictionary<string, Tileset>() { { TileSetName, tileset } };
            SidebarBrush = new ConsoleCharacter()
            {
                TilesetName = TileSetName,
                ForegroundColor = Color.Black
            };
            SidebarFrameBrush = SidebarBrush with
            {
                BackgroundColor = Color.Black,
                ForegroundColor = new Color(255, 165, 0)
            };

        }
    }
}
