using SmallHax.MessageSystem;
using SmallHax.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmallHax.CatPoop
{
    public enum ApplicationState
    {
        Menu,
        Playing
    }

    public class ApplicationController
    {
        private GameController GameController { get; set; }
        private StateMachine<ApplicationController, ApplicationState> StateMachine { get; set; }
        private SfmlExtensions.Console Menu { get; set; }

        public ApplicationController(MessageBus<TopicKey> messageBus)
        {
            Menu = new SfmlExtensions.Console(40, 24); 
        }
    }
}
