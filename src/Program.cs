using System;
using TimHanewich;
using TimHanewich.AgentFramework;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Spectre.Console;

namespace TextAdventureAI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            RunAsync().Wait();
        }

        public static async Task RunAsync()
        {
            //Branding
            Markup title1 = new Markup(":joystick: [blue][bold]LLM TAG[/][/] :joystick:");
            title1.Centered();
            AnsiConsole.Write(title1);

            Markup title2 = new Markup("An [bold]LLM[/]-Driven [bold]T[/]ext [bold]A[/]dventure [bold]G[/]ame");
            title2.Centered();
            AnsiConsole.Write(title2);

            Markup title3 = new Markup("[gray][italic]For more information, visit [underline]https://github.com/TimHanewich/LLM-TAG[/][/][/]");
            title3.Centered();
            AnsiConsole.Write(title3);

            Console.WriteLine();
            Console.WriteLine();

            //Azure openai credentials
            IModelConnection model;
            AzureOpenAICredentials? creds = JsonConvert.DeserializeObject<AzureOpenAICredentials>(System.IO.File.ReadAllText("azure-openai-credentials.json"));
            if (creds == null)
            {
                throw new Exception("Unable to parse AzureOpenAICredentials from azure-openai-credentials.json!");
            }
            model = creds;

            //Ask the user if they want to have input into the game they are playing.
            SelectionPrompt<string> WantsToDesignGame = new SelectionPrompt<string>();
            WantsToDesignGame.Title("Do you want to have creative influence over the text-based adventure you are going to play?");
            WantsToDesignGame.AddChoice("Yes, I have something in mind.");
            WantsToDesignGame.AddChoice("No - random is fine!");
            string WantsToDesignGameChoice = AnsiConsole.Prompt<string>(WantsToDesignGame);
            string? UserGameRequest = null;
            if (WantsToDesignGameChoice == "Yes, I have something in mind.")
            {
                AnsiConsole.MarkupLine("[bold]Great![/] What type of game do you want to play? Describe as much or as little as you want!");
                AnsiConsole.MarkupLine("Consider describing things like the [underline]setting[/], [underline]atmosphere[/], [underline]protagonist[/], [underline]goal[/], and [underline]conflict[/].");
                UserGameRequest = AnsiConsole.Ask<string>("> ");
                Console.WriteLine();
            }

            //Design the game
            Agent GameDesigner = new Agent();
            GameDesigner.Model = model;
            GameDesigner.Messages.Add(new Message(Role.system, System.IO.File.ReadAllText(@".\prompts\GameDesigner_SYSTEM.txt")));
            Message GameDesignerUserMessage = new Message();
            GameDesignerUserMessage.Role = Role.user;
            GameDesignerUserMessage.Content = "Make me a text-based game and output it in JSON format.";
            if (UserGameRequest != null)
            {
                GameDesignerUserMessage.Content = GameDesignerUserMessage.Content + "\n\n" + "Use the following description of a game as inspiration for the game you create: " + UserGameRequest;
            }
            GameDesigner.Messages.Add(GameDesignerUserMessage);
            AnsiConsole.Markup("[gray][italic]Generating game... [/][/]");
            Message msg_GameDesign = await GameDesigner.PromptAsync(json_mode: true);
            AnsiConsole.MarkupLine("[gray][italic]generated![/][/]");
            Console.WriteLine();

            //If there was no content, error
            if (msg_GameDesign.Content == null)
            {
                throw new Exception("Error! The AI did not give a valid game design. It returned nothing!");
            }

            //Parse
            GameDesign gd;
            try
            {
                GameDesign? parsed_gd = JsonConvert.DeserializeObject<GameDesign>(msg_GameDesign.Content);
                if (parsed_gd == null)
                {
                    throw new Exception("The GameDesign parsed from JSON but the object was not returned!");
                }
                else
                {
                    gd = parsed_gd;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Parsing the GameDesign agent's response into a GameDesign class failed! \n\n" + "Error msg: " + ex.Message + "\n\n" + " Here is what the GameDesign agent provided: " + msg_GameDesign.Content);
            }

            //Print out the game
            AnsiConsole.MarkupLine("Below is the game that you will be playing:");
            Console.WriteLine();
            AnsiConsole.MarkupLine("[underline]SETTING[/]");
            AnsiConsole.MarkupLine("[blue]" + gd.setting + "[/]");
            Console.WriteLine();
            AnsiConsole.MarkupLine("[underline]ATMOSPHERE[/]");
            AnsiConsole.MarkupLine("[blue]" + gd.atmosphere + "[/]");
            Console.WriteLine();
            AnsiConsole.MarkupLine("[underline]PROTAGONIST[/]");
            AnsiConsole.MarkupLine("[blue]" + gd.protagonist + "[/]");
            Console.WriteLine();
            AnsiConsole.MarkupLine("[underline]YOUR GOAL[/]");
            AnsiConsole.MarkupLine("[blue]" + gd.goal + "[/]");
            Console.WriteLine();
            AnsiConsole.MarkupLine("[underline]CONFLICT[/]");
            AnsiConsole.MarkupLine("[blue]" + gd.conflict + "[/]");
            Console.WriteLine();
            Console.WriteLine();
            AnsiConsole.Markup("Ready to play? Press [bold]enter[/] to start playing!");
            Console.ReadLine();
            Console.Clear();

            //Create list of decisions
            List<GameDecision> GameHistory = new List<GameDecision>();

            //Create the game engine
            Agent GameEngine = new Agent();
            GameEngine.Model = model;
            
            
            while (true)
            {
                //Prepare game engine
                GameEngine.Messages.Clear(); //clear messages
                GameEngine.Messages.Add(new Message(Role.system, System.IO.File.ReadAllText(@".\prompts\GameEngine_SYSTEM.txt"))); //Add system instructions

                //Prepare user message
                List<string> UserMessage = new List<string>();
                UserMessage.Add("Here is the background of the game:");
                UserMessage.Add("setting: " + gd.setting);
                UserMessage.Add("atmosphere: " + gd.atmosphere);
                UserMessage.Add("protagonist: " + gd.protagonist);
                UserMessage.Add("goal: " + gd.goal);
                UserMessage.Add("conflict: " + gd.conflict);
                UserMessage.Add("");

                //Append game history
                UserMessage.Add("History of the player's gameplay so far:");
                if (GameHistory.Count == 0)
                {
                    UserMessage.Add("(no history yet - please produce the first scenario)");
                }
                else
                {
                    int tick = 1;
                    foreach (GameDecision dec in GameHistory)
                    {
                        UserMessage.Add("situation #" + tick.ToString("#,##0") + ": " + dec.situation);
                        UserMessage.Add("Do you...");
                        foreach (string option in dec.options)
                        {
                            UserMessage.Add(option);
                        }
                        UserMessage.Add("Player decided to: " + dec.decision);
                        UserMessage.Add("");
                        tick = tick + 1;
                    }
                }

                //If they finished the game, say "GAME COMPLETED"
                UserMessage.Add("If, with their last action, the game is now completed, simply respond in JSON with '{\"state\": \"completed\"}'");

                //Add user message
                string UserMessageStr = "";
                foreach (string s in UserMessage)
                {
                    UserMessageStr = UserMessageStr + s + Environment.NewLine;
                }
                if (UserMessageStr.Length > 0)
                {
                    UserMessageStr = UserMessageStr.Substring(0, UserMessageStr.Length - 1); //trim off the last new line
                }
                GameEngine.Messages.Add(new Message(Role.user, UserMessageStr));

                //Prompt the model
                AnsiConsole.Markup("[gray][italic]generating next step... [/][/]");
                Message NextDecisionAIResponse = await GameEngine.PromptAsync(json_mode: true); //prompt
                AnsiConsole.MarkupLine("[gray][italic]done![/][/]");
                Console.WriteLine();
                if (NextDecisionAIResponse.Content == null)
                {
                    throw new Exception("Prompting of GameEngine agent failed! Content returned back empty.");
                }
                 
                //Is the game over?
                if (NextDecisionAIResponse.Content.ToLower().Contains("{\"state\": \"completed\"}"))
                {
                    AnsiConsole.MarkupLine("[green][bold]Congratulations, you have completed the game![/][/]");
                    return;
                }

                //Try to parse the response
                GameDecision? NextGameDecision = JsonConvert.DeserializeObject<GameDecision>(NextDecisionAIResponse.Content);
                if (NextGameDecision == null)
                {
                    throw new Exception("Unable to parse response the GameEngine agent provided as a valid GameDecision class.");
                }

                //Present it to the user and have them choose
                Console.WriteLine(NextGameDecision.situation);
                Console.WriteLine();
                Console.WriteLine("What do you do?");
                SelectionPrompt<string> UserSelection = new SelectionPrompt<string>();
                UserSelection.AddChoices(NextGameDecision.options);
                NextGameDecision.decision = AnsiConsole.Prompt(UserSelection);
                AnsiConsole.MarkupLine("[bold][blue]**" + NextGameDecision.decision + "**[/][/]");
                Console.WriteLine();
                GameHistory.Add(NextGameDecision); //Add to history

            }


            
        }
    }
}