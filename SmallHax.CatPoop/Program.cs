using SFML.Graphics;
using SFML.System;
using SFML.Window;
using SmallHax.MessageSystem;
using System;
using System.Linq;

namespace SmallHax.CatPoop
{
    public enum TopicKey { Application, Game, Input }
    class Program
    {
        static void Main(string[] args)
        {
            var messageBus = new MessageBus<TopicKey>();
            var inputTopic = messageBus[TopicKey.Input];
            
            var viewportSize = new Vector2u(320, 240);
            var window = new RenderWindow(new VideoMode(viewportSize.X, viewportSize.Y, 32), "Cat Poop", Styles.Default, new ContextSettings() { AntialiasingLevel = 0 });
            window.Size = new Vector2u(viewportSize.X * 3, viewportSize.Y * 3);
            window.Closed += (sender, e) => { window.Close(); };
            window.KeyPressed += (sender, e) => { inputTopic.PublishMessage(e); };
            window.SetFramerateLimit(60);

            var gameController = new GameController(messageBus);

            double processTimer = 0;
            double processInterval = 100;

            while (window.IsOpen)
            {
                var startTime = DateTime.UtcNow;
                window.DispatchEvents();
                window.Clear();
                //window.Draw(root);

                gameController.Draw(window);

                if (processTimer == 0)
                {
                    gameController.Process();
                }

                window.Display();
                var endTime = DateTime.UtcNow;
                processTimer += (endTime - startTime).TotalMilliseconds;
                if (processTimer >= processInterval)
                {
                    processTimer = 0;
                }
            }
        }
    }
}
