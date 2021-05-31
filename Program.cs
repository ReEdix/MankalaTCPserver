using SimpleTcp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace TCPserver
{
    class Program
    {
        public static List<Player> connectedClients = new List<Player>();
        public static List<Match> matches = new List<Match>();
        static SimpleTcpServer server = new SimpleTcpServer("127.0.0.1:8001");
        public static MankalaDBDataContext db = new MankalaDBDataContext(@"Data Source = (LocalDB)\MSSQLLocalDB; AttachDbFilename = C:\Users\user\source\repos\MankalaTCPserver\databaseM.mdf; Integrated Security = True");
        static void Main(string[] args)
        {         
            server.Events.ClientConnected += Events_ClientConnected;
            server.Events.ClientDisconnected += Events_ClientDisconnected;
            server.Events.DataReceived += Events_DataReceived;
            server.Start();
            Console.WriteLine("Server wystartował ....");

            string commandLine;
            while((commandLine = Console.ReadLine()) != "EXIT")
            {
                if(commandLine.Equals("CLIENTS"))
                {
                    foreach(Player client in connectedClients)
                    {
                        Console.WriteLine(client.name);
                    }
                }
            }
        }

        private static void Events_DataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine($"Dane od clienta: {e.IpPort} -> {Encoding.UTF8.GetString(e.Data)}");

            String[] messageData = Encoding.UTF8.GetString(e.Data).Split(':');

            switch (messageData[0])
            {
                case Messages.Client.Host:
                    matches.Add(new Match(e.IpPort));
                    Console.WriteLine($"Gracz: {e.IpPort} stworzył nową gre");
                    break;
                case Messages.Client.Join:

                    string hostAdress = $"{messageData[1]}:{messageData[2]}";

                    foreach (Match match in matches)
                    {
                        if (match.playerWhite.Equals(hostAdress))
                        {
                            match.playerBlack = e.IpPort;
                            server.Send(match.playerWhite, Messages.Server.Start + ":WHITE");
                            server.Send(match.playerBlack, Messages.Server.Start + ":BLACK");
                            Console.WriteLine("Mecz wystartował");
                            break;
                        }
                    }
                    break;
                case Messages.Client.Move:
                    Match ourMatch = matches.Find(x => x.playerBlack == e.IpPort);
                    if(ourMatch == null)
                    {
                        ourMatch = matches.Find(x => x.playerWhite == e.IpPort);
                    }

                    if(e.IpPort == ourMatch.playerWhite)
                    {
                        server.Send(ourMatch.playerBlack, Encoding.UTF8.GetString(e.Data));
                    }
                    else
                    {
                        server.Send(ourMatch.playerWhite, Encoding.UTF8.GetString(e.Data));
                    }                  
                    break;
                case Messages.Server.Matches:
                    Console.WriteLine(listOfMatches());
                    server.Send(e.IpPort, listOfMatches());
                    break;
                case Messages.Client.Cancel:
                    matches.RemoveAll(x => x.playerWhite == e.IpPort);
                    break;
                case Messages.Client.Login:
                    if(!db.userLogin(messageData[1], messageData[2]))
                    {
                        server.Send(e.IpPort, Messages.Server.Disconnect);
                        break;
                    }

                    if(connectedClients.Exists(x => x.name == messageData[1]))
                    {
                        server.Send(e.IpPort, Messages.Server.Logged);
                    }
                    connectedClients.Add(new Player(e.IpPort, messageData[1]));

                    break;
                case Messages.Client.Register:
                    addUser(Encoding.UTF8.GetString(e.Data));
                    server.Send(e.IpPort, Messages.Server.Disconnect);
                    break;

                case Messages.Client.EndGame:  // endgame message + playercolor
                    Match thisMatch = matches.Find(x => x.playerWhite == e.IpPort);
                    if (thisMatch == null)
                    {
                        thisMatch = matches.Find(x => x.playerBlack == e.IpPort);
                    }
                    Player thisPlayerWhite = connectedClients.Find(x => x.ip == thisMatch.playerWhite);
                    Player thisPlayerBlack = connectedClients.Find(x => x.ip == thisMatch.playerBlack);
                    if (messageData[1] == "BLACK")
                    {
                        server.Send(e.IpPort, Messages.Server.Winner + ":" + thisPlayerWhite.name + ":" +
                            thisPlayerBlack.name + ":" + thisPlayerBlack.name);
                        server.Send(thisMatch.playerWhite, Messages.Server.Lost + ":" + thisPlayerBlack.name);

                    }
                    else
                    {
                        server.Send(e.IpPort, Messages.Server.Winner + ":" + thisPlayerWhite.name + ":" +
                            thisPlayerBlack.name + ":" + thisPlayerWhite.name);
                        // do kogo, imie wygranego
                        server.Send(thisMatch.playerBlack, Messages.Server.Lost + ":" + thisPlayerWhite.name);
                    }
                    break;

                case Messages.Client.SaveGame:
                    Player thisPlayer1 = connectedClients.Find(x => x.name == messageData[1]);
                    Player thisPlayer2 = connectedClients.Find(x => x.name == messageData[2]);
                    Player thisPlayer3 = connectedClients.Find(x => x.name == messageData[3]);
                    addGame(thisPlayer1.name + ":"+ thisPlayer2.name + ":"+ thisPlayer3.name);
                    break;

                default:
                    break;
            }
        }

        private static void Events_ClientDisconnected(object sender, ClientDisconnectedEventArgs e)
        {
            Console.WriteLine($"Client disconnected {e.IpPort}");
            matches.RemoveAll(x => x.playerWhite == e.IpPort);
            connectedClients.RemoveAll(x => x.ip == e.IpPort);       
        }

        private static void Events_ClientConnected(object sender, ClientConnectedEventArgs e)
        {
            Console.WriteLine($"New client connected {e.IpPort}");
                
        }

        private static string listOfMatches()
        {
            StringBuilder listOfMatches = new StringBuilder();
            listOfMatches.Append(Messages.Server.Matches);

            foreach(Match match in matches)
            {
                if (match.playerBlack == null)
                {
                    listOfMatches.Append($":{match.playerWhite}");
                }
            }

            return listOfMatches.ToString();
        }

        private static void addUser(String playerData)
        {

            //name;password
            User newUser = new User();

            String[] substring = playerData.Split(':');
            newUser.name = substring[1];
            newUser.password = substring[2];



            db.Users.InsertOnSubmit(newUser);
            try
            {
                db.SubmitChanges();
            }
            catch (Exception e)
            {
                Console.WriteLine("Can't submit changes in database - user");
                Console.WriteLine(e.Message);
            }
        }
        private static void addGame(String gameData)
        {
            //player1_name:player2_name:winner_name
            Game newGame = new Game();

            String[] substring = gameData.Split(':');
            int p1 = 0, p2 = 0, w = 0;

            foreach(User u in db.Users)
            {
                if (u.name == substring[0])
                {
                    p1 = u.Id;
                }
                if (u.name == substring[1])
                {
                    p2 = u.Id;
                }
                if (u.name == substring[2])
                {
                    w = u.Id;
                }

            }

            newGame.player1 = p1;
            newGame.player2 = p2;
            newGame.winner = w;

            Console.WriteLine("Zapisane osoby to ->" + p1 + ":" + p2 + ":" + w);

            db.Games.InsertOnSubmit(newGame);
            try
            {
                db.SubmitChanges();
            }
            catch (Exception e)
            {
                Console.WriteLine("Can't submit changes in database - game");
                Console.WriteLine(e.Message);
                Console.ReadLine();
            }
        }
    }
}
