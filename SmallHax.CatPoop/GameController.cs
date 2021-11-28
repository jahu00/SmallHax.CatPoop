using SFML.Graphics;
using SFML.System;
using SFML.Window;
using SmallHax.MessageSystem;
using SmallHax.SfmlExtensions;
using SmallHax.SfmlExtensions.TileMapper;
using SmallHax.State;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmallHax.CatPoop
{
    public enum GameState
    {
        WaitForMove,
        Poop,
        Push,
        Drop,
        CheckOver,
        GrantBonus,
        Over
    }

    public class GameController
    {
        private Settings Settings { get; set; }
        private  Style Style { get; set; }
        private Vector2u TileSize { get; set; } = new Vector2u(24, 24);
        private SfmlExtensions.Console Sidebar { get; set; }
        private SfmlExtensions.Console Overlay { get; set; }
        private Game Model { get; set; }
        private Vector2i CursorPosition { get; set; }
        private Sprite CursorSprite { get; set; }
        private StateMachine<GameController, GameState> StateMachine { get; set; }
        private MessageBus<TopicKey> MessageBus { get; set; }
        public GameController(MessageBus<TopicKey> messageBus, Settings settings, Style style)
        {
            Settings = settings;
            Style = style;
            MessageBus = messageBus;
            Sidebar = new SfmlExtensions.Console(Style.Tilesets, 8, 10, 10, 24)
            {
                BackgroundColor = new Color(255, 165, 0),
                Position = new Vector2f(240, 0)
            };
            
            Sidebar.SetText(0, 0, "╔", Style.SidebarFrameBrush);
            Sidebar.SetText(9, 0, "╗", Style.SidebarFrameBrush);
            Sidebar.SetText(0, 23, "╚", Style.SidebarFrameBrush);
            Sidebar.SetText(9, 23, "╝", Style.SidebarFrameBrush);
            Sidebar.SetText(1, 1, "Score:", Style.SidebarBrush);

            CursorSprite = new Sprite(Style.FrameTexture);

            Overlay = new SfmlExtensions.Console(Style.Tilesets, 8, 10, 24, 4)
            {
                BackgroundColor = new Color(255, 165, 0),
                Position = new Vector2f(3 * 8, 10 * 10)
            };

            Overlay.SetText(0, 0, "╔", Style.SidebarFrameBrush);
            Overlay.SetText(23, 0, "╗", Style.SidebarFrameBrush);
            Overlay.SetText(0, 3, "╚", Style.SidebarFrameBrush);
            Overlay.SetText(23, 3, "╝", Style.SidebarFrameBrush);
            Overlay.SetText(1, 1, "GAME OVER", Style.SidebarBrush);

            InitializeStates();
            NewGame();
            StateMachine.SetState(GameState.WaitForMove);
        }

        private void InitializeStates()
        {
            StateMachine = new StateMachine<GameController, GameState>(this);
            StateMachine.AddState<WaitForMoveStateScript>(GameState.WaitForMove);
            StateMachine.AddState<PoopStateScript>(GameState.Poop);
            StateMachine.AddState<PushStateScript>(GameState.Push);
            StateMachine.AddState<DropStateScript>(GameState.Drop);
            StateMachine.AddState<CheckOverStateScript>(GameState.CheckOver);
            StateMachine.AddState<GrantBonusStateScript>(GameState.GrantBonus);
            StateMachine.AddState<OverStateScript>(GameState.Over);
        }

        public void NewGame()
        {
            Model = new Game()
            {
                BoardSize = new Vector2i(10, 10)
            };
            Model.Tiles = new Tile[Model.BoardSize.X, Model.BoardSize.Y];

            var random = new Random();
            var maxColorId = Style.Colors.Count();

            for (var y = 0; y < Model.BoardSize.Y; y++)
            {
                for (var x = 0; x < Model.BoardSize.X; x++)
                {
                    var colorId = random.Next(0, maxColorId);
                    var tile = new Tile() { ColorId = colorId };
                    Model.Tiles[x, y] = tile;
                }
            }

            CursorPosition = new Vector2i(Model.BoardSize.X / 2, Model.BoardSize.Y / 2);
        }

        public void Process()
        {
            StateMachine.Process();
        }

        public void Draw(RenderTarget renderTarget)
        {
            DrawTiles(renderTarget);
            DrawCursor(renderTarget);
            DrawScore(renderTarget);
            if (StateMachine.CurrentState == GameState.Over)
            {
                DrawOverlay(renderTarget);
            }
        }

        private void DrawOverlay(RenderTarget renderTarget)
        {
            renderTarget.Draw(Overlay);
        }

        private void DrawScore(RenderTarget renderTarget)
        {
            var scoreStr = Model.Score.ToString().PadLeft(8, '0');

            Sidebar.SetText(1, 2, scoreStr, Style.SidebarBrush);

            renderTarget.Draw(Sidebar);
        }

        private void DrawCursor(RenderTarget renderTarget)
        {
            CursorSprite.Position = new Vector2f(TileSize.X * CursorPosition.X, TileSize.Y * CursorPosition.Y);
            renderTarget.Draw(CursorSprite);
        }

        private void DrawTiles(RenderTarget renderTarget)
        {
            for (var y = 0; y < Model.BoardSize.Y; y++)
            {
                for (var x = 0; x < Model.BoardSize.X; x++)
                {
                    var tile = Model.Tiles[x, y];
                    if (tile == null)
                    {
                        continue;
                    }
                    var color = Style.Colors[tile.ColorId];
                    var tileSprite = new Sprite(Style.TileTexture)
                    {
                        Color = color,
                        Position = new Vector2f(TileSize.X * x, TileSize.Y * y)
                    };
                    renderTarget.Draw(tileSprite);
                }
            }
        }

        private void ApplyBonus(decimal multiplier)
        {
            Model.Score = (int)(Model.Score * multiplier);
        }

        private void DrawBonus(decimal multiplier)
        {
            var multiplierText = multiplier.ToString("#.##", CultureInfo.InvariantCulture);
            Overlay.SetText(1, 2, $"Bonus: x{multiplierText}", Style.SidebarBrush);
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

                if (keyInputEvent.Code == Owner.Settings.Down && Owner.CursorPosition.Y < Owner.Model.BoardSize.Y - 1)
                {
                    Owner.CursorPosition += MoveVectors.Down;
                }

                if (keyInputEvent.Code == Owner.Settings.Left && Owner.CursorPosition.X > 0)
                {
                    Owner.CursorPosition += MoveVectors.Left;
                }

                if (keyInputEvent.Code == Owner.Settings.Right && Owner.CursorPosition.X < Owner.Model.BoardSize.X - 1)
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
                GivePoints(positionsOfTilesToRemove.Count);
                Owner.StateMachine.SetState(GameState.Drop);
            }

            private void GivePoints(int removedTileCount)
            {
                var basePoints = removedTileCount < 5 ? removedTileCount : (1 + removedTileCount % 5);
                Owner.Model.Score += basePoints * (int)Math.Pow(10, 1 + removedTileCount / 5);
            }

            public List<Vector2i> FindMatchingTiles()
            {
                var checkedTileMap = new bool[Owner.Model.BoardSize.X,Owner.Model.BoardSize.Y];
                var results = new List<Vector2i>();
                var totalNumberOfTiles = Owner.Model.BoardSize.X * Owner.Model.BoardSize.Y;
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
                if (belowPosition.Y < Owner.Model.BoardSize.Y)
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
                if (rightPosition.X < Owner.Model.BoardSize.X)
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
                var tilesMoved = DropTiles();
                if (!tilesMoved)
                {
                    Owner.StateMachine.SetState(GameState.Push);
                }
            }

            public bool DropTiles()
            {
                var tilesMoved = false;
                for (var y = Owner.Model.BoardSize.Y - 1; y > 0; y--)
                {
                    for (var x = 0; x < Owner.Model.BoardSize.X; x++)
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
                return tilesMoved;
            }
        }

        public class PushStateScript : StateScript<GameController, GameState>
        {
            public override void Process()
            {
                var tilesMoved = PushColumns();

                if (!tilesMoved)
                {
                    Owner.StateMachine.SetState(GameState.CheckOver);
                }
            }

            public bool PushColumns()
            {
                var tilesMoved = false;

                for (var x = 0; x < Owner.Model.BoardSize.X - 1; x++)
                {
                    for (var y = 0; y < Owner.Model.BoardSize.Y; y++)
                    {
                        var tile = Owner.Model.Tiles[x, y];
                        if (tile != null)
                        {
                            goto nextColumn;
                        }
                    }
                    for (var y = 0; y < Owner.Model.BoardSize.Y; y++)
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
                    nextColumn: continue;
                }

                return tilesMoved;
            }
        }

        public class CheckOverStateScript : StateScript<GameController, GameState>
        {
            public override void Process()
            {
                for (var y = 1; y < Owner.Model.BoardSize.Y; y++)
                {
                    for (var x = 0; x < Owner.Model.BoardSize.X - 1; x++)
                    {
                        var tile = Owner.Model.Tiles[x, y];
                        if (tile == null)
                        {
                            continue;
                        }
                        var verticalyAdjecentTile = Owner.Model.Tiles[x, y - 1];
                        var horizontallyAdjecentTile = Owner.Model.Tiles[x + 1, y];
                        if (horizontallyAdjecentTile?.ColorId == tile.ColorId || verticalyAdjecentTile?.ColorId == tile.ColorId)
                        {
                            Owner.StateMachine.SetState(GameState.WaitForMove);
                            return;
                        }
                    }
                }
                Owner.StateMachine.SetState(GameState.GrantBonus);
            }
        }

        public class OverStateScript : StateScript<GameController, GameState>
        {
            public override void Process()
            {
            }
        }

        public class GrantBonusStateScript : StateScript<GameController, GameState>
        {
            public override void Process()
            {
                var leftOverTileCount = CountLeftOverTiles();
                GrantPoints(leftOverTileCount);
                Owner.StateMachine.SetState(GameState.Over);
            }

            private int CountLeftOverTiles()
            {
                var result = 0;
                for (var y = 0; y < Owner.Model.BoardSize.Y; y++)
                {
                    for (var x = 0; x < Owner.Model.BoardSize.X; x++)
                    {
                        var tile = Owner.Model.Tiles[x, y];
                        if (tile == null)
                        {
                            continue;
                        }
                        result++;
                    }
                }
                return result;
            }

            private void GrantPoints(int leftOverTileCount)
            {
                if (leftOverTileCount >= 10)
                {
                    return;
                }
                var multiplier = (20 - leftOverTileCount) / 10m;
                Owner.DrawBonus(multiplier);
                Owner.ApplyBonus(multiplier);
            }

        }
    }
}
