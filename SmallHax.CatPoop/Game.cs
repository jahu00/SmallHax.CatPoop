using SFML.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmallHax.CatPoop
{
    public class Game
    {
        public Tile[,] Tiles { get; set; }

        public int Score { get; set; }

        public Vector2i BoardSize { get; set; }
    }
}
