using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SFML.Window.Keyboard;

namespace PoopCat
{
    public class Settings
    {
        public Key Up { get; set; } = Key.Up;
        public Key Down { get; set; } = Key.Down;
        public Key Left { get; set; } = Key.Left;
        public Key Right { get; set; } = Key.Right;
        public Key Fire { get; set; } = Key.Space;
        public Key Back { get; set; } = Key.Escape;
    }
}
