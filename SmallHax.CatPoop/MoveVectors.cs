using SFML.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmallHax.CatPoop
{
    public static class MoveVectors
    {
        public static Vector2i Up { get; private set; } = new Vector2i(0, -1);
        public static Vector2i Down { get; private set; } = new Vector2i(0, 1);
        public static Vector2i Left { get; private set; } = new Vector2i(-1, 0);
        public static Vector2i Right { get; private set; } = new Vector2i(1, 0);
    }
}
