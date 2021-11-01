using SFML.Graphics;
using SFML.System;
using SFML.Window;
using SmallHax.MessageSystem;
using SmallHax.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoopCat
{
    public enum GameState
    {
        WaitForMove,
        Poop,
        Push,
        Drop,
        CheckOver,
        Over
    }

    public class GameController
    {
        private Settings Settings { get; set; } = new Settings();
        private Color[] Colors { get; set; } = new Color[] { Color.Red, Color.Green, Color.Blue, new Color(255, 165, 0), Color.Magenta };

        private Vector2u TileSize { get; set; } = new Vector2u(24, 24);
        private Vector2i BoardSize { get; set; } = new Vector2i(10, 10);

        private Texture TileTexture { get; set; }

        private Texture FrameTexture { get; set; }

        private Game Model { get; set; }

        private Vector2i CursorPosition { get; set; }

        private Sprite CursorSprite { get; set; }
        private StateMachine<GameController, GameState> StateMachine { get; set; }
        private MessageBus<TopicKey> MessageBus { get; set; }
        public GameController(MessageBus<TopicKey> messageBus)
        {
            MessageBus = messageBus;
            TileTexture = new Texture("Data/Images/poop.png");
            FrameTexture = new Texture("Data/Images/frame.png");

            Model = new Game();
            Model.Tiles = new Tile[BoardSize.X, BoardSize.Y];

            var random = new Random();
            var maxColorId = Colors.Count();

            for (var y = 0; y < BoardSize.Y; y++)
            {
                for (var x = 0; x < BoardSize.X; x++)
                {
                    var colorId = random.Next(0, maxColorId);
                    var tile = new Tile() { ColorId = colorId };
                    Model.Tiles[x, y] = tile;
                }
            }

            CursorPosition = new Vector2i(BoardSize.X / 2, BoardSize.Y / 2);
            CursorSprite = new Sprite(FrameTexture);

            
            InitializeStates(GameState.WaitForMove);
        }

        private void InitializeStates(GameState state)
        {
            StateMachine = new StateMachine<GameController, GameState>(this);
            StateMachine.AddState<WaitForMoveStateScript>(GameState.WaitForMove);
            StateMachine.AddState<PoopStateScript>(GameState.Poop);
            StateMachine.AddState<PushStateScript>(GameState.Push);
            StateMachine.AddState<DropStateScript>(GameState.Drop);
            StateMachine.SetState(state);
        }

        public void Process()
        {
            StateMachine.Process();
        }

        public void Draw(RenderTarget renderTarget)
        {
            for (var y = 0; y < BoardSize.Y; y++)
            {
                for (var x = 0; x < BoardSize.X; x++)
                {
                    var tile = Model.Tiles[x, y];
                    if (tile == null)
                    {
                        continue;
                    }
                    var color = Colors[tile.ColorId];
                    var tileSprite = new Sprite(TileTexture)
                    {
                        Color = color,
                        Position = new Vector2f(TileSize.X * x, TileSize.Y * y)
                    };
                    renderTarget.Draw(tileSprite);
                }
            }

            CursorSprite.Position = new Vector2f(TileSize.X * CursorPosition.X, TileSize.Y * CursorPosition.Y);

            renderTarget.Draw(CursorSprite);
        }

        public class WaitForMoveStateScript : StateScript<GameController, GameState>
        {
            private Topic InputTopic { get; set; }
            private Subscription InputSubscription { get; set; }
            public override void Initialize(GameController owner, StateMachine<GameController, GameState> stateMachine, object stateArgs = null)
            {
                base.Initialize(owner, stateMachine, stateArgs);
                InputTopic = Owner.MessageBus[TopicKey.Input];
                InputSubscription = InputTopic.Subscribe(this);
            }

            public override void Deinitialize()
            {
                base.Deinitialize();
                InputTopic.Unsubscribe(this);
            }

            public override void Process()
            {
                var inputEvent = InputSubscription.DequeueMessage();

                if (inputEvent == null)
                {
                    return;
                }

                var keyInputEvent = (KeyEventArgs)inputEvent;

                if (keyInputEvent.Code == Owner.Settings.Up && Owner.CursorPosition.Y > 0)
                {
                    Owner.CursorPosition += MoveVectors.Up;
                }

                if (keyInputEvent.Code == Owner.Settings.Down && Owner.CursorPosition.Y < Owner.BoardSize.Y - 1)
                {
                    Owner.CursorPosition += MoveVectors.Down;
                }

                if (keyInputEvent.Code == Owner.Settings.Left && Owner.CursorPosition.X > 0)
                {
                    Owner.CursorPosition += MoveVectors.Left;
                }

                if (keyInputEvent.Code == Owner.Settings.Right && Owner.CursorPosition.X < Owner.BoardSize.X - 1)
                {
                    Owner.CursorPosition += MoveVectors.Right;
                }

                if (keyInputEvent.Code == Owner.Settings.Fire)
                {
                    Owner.StateMachine.SetState(GameState.Poop);
                }

                InputSubscription.Clear();
            }
        }

        public class PoopStateScript : StateScript<GameController, GameState>
        {
            public override void Process()
            {
                var positionsOfTilesToRemove = FindMatchingTiles();
                if (positionsOfTilesToRemove.Count < 2)
                {
                    Owner.StateMachine.SetState(GameState.WaitForMove);
                    return;
                }
                foreach(var position in positionsOfTilesToRemove)
                {
                    Owner.Model.Tiles[position.X, position.Y] = null;
                }
                Owner.StateMachine.SetState(GameState.Drop);
            }

            public List<Vector2i> FindMatchingTiles()
            {
                var checkedTileMap = new bool[Owner.BoardSize.X,Owner.BoardSize.Y];
                var results = new List<Vector2i>();
                var totalNumberOfTiles = Owner.BoardSize.X * Owner.BoardSize.Y;
                FindMatchingTiles(Owner.CursorPosition, checkedTileMap, results, totalNumberOfTiles);
                return results;
            }

            private void FindMatchingTiles(Vector2i position, bool[,] checkedTileMap, List<Vector2i> results, int watchdog)
            {
                watchdog--;
                if (watchdog < 0)
                {
                    throw new Exception("Watchdog triggered");
                }
                if (checkedTileMap[position.X, position.Y])
                {
                    return;
                }
                checkedTileMap[position.X, position.Y] = true;
                var tile = Owner.Model.Tiles[position.X, position.Y];
                if (tile == null)
                {
                    return;
                }

                results.Add(position);

                var abovePosition = position + MoveVectors.Up;
                if (abovePosition.Y > -1)
                {
                    var otherTile = Owner.Model.Tiles[abovePosition.X, abovePosition.Y];
                    if (otherTile?.ColorId == tile.ColorId)
                    {
                        FindMatchingTiles(abovePosition, checkedTileMap, results, watchdog);
                    }
                }
                var belowPosition = position + MoveVectors.Down;
                if (belowPosition.Y < Owner.BoardSize.Y)
                {
                    var otherTile = Owner.Model.Tiles[belowPosition.X, belowPosition.Y];
                    if (otherTile?.ColorId == tile.ColorId)
                    {
                        FindMatchingTiles(belowPosition, checkedTileMap, results, watchdog);
                    }
                }
                var leftPosition = position + MoveVectors.Left;
                if (leftPosition.X > -1)
                {
                    var otherTile = Owner.Model.Tiles[leftPosition.X, leftPosition.Y];
                    if (otherTile?.ColorId == tile.ColorId)
                    {
                        FindMatchingTiles(leftPosition, checkedTileMap, results, watchdog);
                    }
                }
                var rightPosition = position + MoveVectors.Right;
                if (rightPosition.X < Owner.BoardSize.X)
                {
                    var otherTile = Owner.Model.Tiles[rightPosition.X, rightPosition.Y];
                    if (otherTile?.ColorId == tile.ColorId)
                    {
                        FindMatchingTiles(rightPosition, checkedTileMap, results, watchdog);
                    }
                }
            }
        }

        public class DropStateScript : StateScript<GameController, GameState>
        {
            public override void Process()
            {
                var tilesMoved = false;
                
                for (var y = Owner.BoardSize.Y - 1; y > 0; y--)
                {
                    for (var x = 0; x < Owner.BoardSize.X; x++)
                    {
                        var tile = Owner.Model.Tiles[x, y];
                        if (tile != null)
                        {
                            continue;
                        }
                        var otherTile = Owner.Model.Tiles[x, y - 1];
                        if (otherTile == null)
                        {
                            continue;
                        }
                        Owner.Model.Tiles[x, y - 1] = null;
                        Owner.Model.Tiles[x, y] = otherTile;
                        tilesMoved = true;
                    }
                }
                if (!tilesMoved)
                {
                    Owner.StateMachine.SetState(GameState.Push);
                }
            }
        }

        public class PushStateScript : StateScript<GameController, GameState>
        {
            public override void Process()
            {
                var tilesMoved = false;

                for (var x = 0; x < Owner.BoardSize.X - 1; x++)
                {
                    for (var y = 0; y < Owner.BoardSize.Y; y++)
                    {
                        var tile = Owner.Model.Tiles[x, y];
                        if (tile != null)
                        {
                            goto doubleBreak;
                        }
                    }
                    for (var y = 0; y < Owner.BoardSize.Y; y++)
                    {
                        var tile = Owner.Model.Tiles[x + 1, y];
                        if (tile == null)
                        {
                            continue;
                        }
                        Owner.Model.Tiles[x, y] = tile;
                        Owner.Model.Tiles[x + 1, y] = null;
                        tilesMoved = true;
                    }
                    doubleBreak: continue;
                }
                if (!tilesMoved)
                {
                    Owner.StateMachine.SetState(GameState.WaitForMove);
                }
            }
        }
    }
}
