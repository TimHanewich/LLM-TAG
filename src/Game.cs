using System;
using System.Collections.Generic;

namespace TextAdventureAI
{
    public class Game
    {
        public GameDesign Design {get; set;}
        public List<GameDecision> History {get; set;}

        public Game()
        {
            Design = new GameDesign();
            History = new List<GameDecision>();
        }
    }
}