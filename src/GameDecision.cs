using System;

namespace TextAdventureAI
{
    public class GameDecision
    {
        public string situation {get; set;}
        public string[] options {get; set;}
        public string decision {get; set;}

        public GameDecision()
        {
            situation = "";
            options = new string[]{};
            decision = "";
        }

    }
}