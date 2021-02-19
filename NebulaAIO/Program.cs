using EnsoulSharp;
using EnsoulSharp.SDK;
using NebulaAio.Champions;
using System;
using System.Net;

namespace NebulaAio
{

    public class Program
    {

        public static void Main(string[] args)
        {
            GameEvent.OnGameLoad += OnLoadingComplete;
        }

        private static void OnLoadingComplete()
        {
            if (ObjectManager.Player == null)
                return;
            try
            {
                switch (GameObjects.Player.CharacterName)
                {

                    case "Viego":
                        Viego.OnGameLoad();
                        Game.Print("<font color='#ff0000' size='25'> [NebulaAIO]:  </font>" + ObjectManager.Player.CharacterName + " Loaded");
                        Game.Print("<font color='#ff0000' size='25'> [NebulaAIO]:  </font>" + "<font color='#F7FF00' size='25'>Devloped By Akane </font>");
                        break;
                    
                    case "Kindred":
                        Kindred.OnGameLoad();
                        Game.Print("<font color='#ff0000' size='25'> [NebulaAIO]:  </font>" + ObjectManager.Player.CharacterName + " Loaded");
                        Game.Print("<font color='#ff0000' size='25'> [NebulaAIO]:  </font>" + "<font color='#F7FF00' size='25'>Devloped By Akane </font>");
                        break;
                    
                    case "Khazix":
                        Khazix.OnGameLoad();
                        Game.Print("<font color='#ff0000' size='25'> [NebulaAIO]:  </font>" + ObjectManager.Player.CharacterName + " Loaded");
                        Game.Print("<font color='#ff0000' size='25'> [NebulaAIO]:  </font>" + "<font color='#F7FF00' size='25'>Devloped By Akane </font>");
                        break;
                    
                    case "Lux":
                        Lux.OnGameLoad();
                        Game.Print("<font color='#ff0000' size='25'> [NebulaAIO]:  </font>" + ObjectManager.Player.CharacterName + " Loaded");
                        Game.Print("<font color='#ff0000' size='25'> [NebulaAIO]:  </font>" + "<font color='#F7FF00' size='25'>Devloped By Akane </font>");
                        break;
                    
                    case "Diana":
                        Diana.OnGameLoad();
                        Game.Print("<font color='#ff0000' size='25'> [NebulaAIO]:  </font>" + ObjectManager.Player.CharacterName + " Loaded");
                        Game.Print("<font color='#ff0000' size='25'> [NebulaAIO]:  </font>" + "<font color='#F7FF00' size='25'>Devloped By Akane </font>");
                        break;
                    
                    case "Cassiopeia":
                        Cassiopeia.OnGameLoad();
                        Game.Print("<font color='#ff0000' size='25'> [NebulaAIO]:  </font>" + ObjectManager.Player.CharacterName + " Loaded");
                        Game.Print("<font color='#ff0000' size='25'> [NebulaAIO]:  </font>" + "<font color='#F7FF00' size='25'>Devloped By Akane </font>");
                        break;
                    
                    case "Xerath":
                        Xerath.OnGameLoad();
                        Game.Print("<font color='#ff0000' size='25'> [NebulaAIO]:  </font>" + ObjectManager.Player.CharacterName + " Loaded");
                        Game.Print("<font color='#ff0000' size='25'> [NebulaAIO]:  </font>" + "<font color='#F7FF00' size='25'>Devloped By Akane </font>");
                        break;
                    
                    case "Ashe":
                        Ashe.OnGameLoad();
                        Game.Print("<font color='#ff0000' size='25'> [NebulaAIO]:  </font>" + ObjectManager.Player.CharacterName + " Loaded");
                        Game.Print("<font color='#ff0000' size='25'> [NebulaAIO]:  </font>" + "<font color='#F7FF00' size='25'>Devloped By Akane </font>");
                        break;
                    
                    case "Annie":
                        Annie.OnGameLoad();
                        Game.Print("<font color='#ff0000' size='25'> [NebulaAIO]:  </font>" + ObjectManager.Player.CharacterName + " Loaded");
                        Game.Print("<font color='#ff0000' size='25'> [NebulaAIO]:  </font>" + "<font color='#F7FF00' size='25'>Devloped By Akane </font>");
                        break;

                    case "Blitzcrank":
                        Blitzcrank.OnGameLoad();
                        Game.Print("<font color='#ff0000' size='25'> [NebulaAIO]:  </font>" + ObjectManager.Player.CharacterName + " Loaded");
                        Game.Print("<font color='#ff0000' size='25'> [NebulaAIO]:  </font>" + "<font color='#F7FF00' size='25'>Devloped By Akane </font>");
                        break;
                    
                    case "Malphite":
                        Malphite.OnGameLoad();
                        Game.Print("<font color='#ff0000' size='25'> [NebulaAIO]:  </font>" + ObjectManager.Player.CharacterName + " Loaded");
                        Game.Print("<font color='#ff0000' size='25'> [NebulaAIO]:  </font>" + "<font color='#F7FF00' size='25'>Devloped By Akane </font>");
                        break;
                    
                    case "Ezreal":
                        Ezreal.OnGameLoad();
                        Game.Print("<font color='#ff0000' size='25'> [NebulaAIO]:  </font>" + ObjectManager.Player.CharacterName + " Loaded");
                        Game.Print("<font color='#ff0000' size='25'> [NebulaAIO]:  </font>" + "<font color='#F7FF00' size='25'>Devloped By Akane </font>");
                        break;
                    
                    case "Corki":
                        Corki.OnGameLoad();
                        Game.Print("<font color='#ff0000' size='25'> [NebulaAIO]:  </font>" + ObjectManager.Player.CharacterName + " Loaded");
                        Game.Print("<font color='#ff0000' size='25'> [NebulaAIO]:  </font>" + "<font color='#F7FF00' size='25'>Devloped By Akane </font>");
                        break;
                    
                    case "Twitch":
                        Twitch.OnGameLoad();
                        Game.Print("<font color='#ff0000' size='25'> [NebulaAIO]:  </font>" + ObjectManager.Player.CharacterName + " Loaded");
                        Game.Print("<font color='#ff0000' size='25'> [NebulaAIO]:  </font>" + "<font color='#F7FF00' size='25'>Devloped By Akane </font>");
                        break;
                    
                    default:
                        Game.Print("<font color='#ff0000' size='25'>[NebulaAIO] Does Not Support :" + ObjectManager.Player.CharacterName + "</font>");
                        Console.WriteLine("[NebulaAIO] Does Not Support " + ObjectManager.Player.CharacterName);
                        break;

                }
                string stringg;
                string uri = "https://raw.githubusercontent.com/Senthixx/NebulaAioV2/main/version.txt";
                using (WebClient client = new WebClient())
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    stringg = client.DownloadString(uri);
                }
                string versionas = "2.0.0.10\n";
                if (versionas != stringg)
                {
                    Game.Print("<font color='#ff0000'> [NebulaAIO]: </font> <font color='#ffe6ff' size='25'>You don't have the current version, please UPDATE !</font>");
                }
                else if (versionas == stringg)
                {
                    Game.Print("<font color='#ff0000' size='25'> [NebulaAIO]: </font> <font color='#ffe6ff' size='25'>Is updated to the latest version!</font>");
                }
            }
            catch (Exception ex)
            {
                Game.Print("Error in loading");
            }
        }
    }
}
