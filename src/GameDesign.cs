using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TextAdventureAI
{
    public class GameDesign
    {
        public string setting {get; set;}
        public string atmosphere {get; set;}
        public string protagonist {get; set;}
        public string goal {get; set;}
        public string conflict {get; set;}

        public GameDesign()
        {
            setting = "";
            atmosphere = "";
            protagonist = "";
            goal = "";
            conflict = "";
        }
    }
}