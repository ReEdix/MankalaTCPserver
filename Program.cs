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
        public static MankalaDBDataContext db = new MankalaDBDataContext($@"Data Source = (LocalDB)\MSSQLLocalDB; AttachDbFilename = {Environment.CurrentDirectory}\databaseM.mdf; Integrated Security = True");
        static void Main(string[] args)
        {         
            server.Events.ClientConnected += Events_ClientConnected;
            server.Events.ClientDisconnected += Events_ClientDisconnected;
            server.Events.DataReceived += Events_DataReceived;
            server.Start();
            Console.WriteLine("Server wystartował ....");
            Console.WriteLine(Environment.CurrentDirectory);

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
                    matches.Add(new Match(connectedClients.Find(x => x.ip == e.IpPort)));
                    Console.WriteLine($"Gracz: {e.IpPort} stworzył nową gre");
                    SendMatchesToAll();
                    break;

                case Messages.Client.Join:
                    string hostAdress = messageData[1];
                    foreach (Match match in matches)
                    {
                        if (match.playerWhite.name.Equals(hostAdress) && match.playerBlack == null)
                        {
                            match.playerBlack = new Player(e.IpPort, connectedClients.Find(x => x.ip == e.IpPort).name);
                            server.Send(match.playerWhite.ip, Messages.Server.Start + ":WHITE");
                            server.Send(match.playerBlack.ip, Messages.Server.Start + ":BLACK");
                            Console.WriteLine("Mecz wystartował");
                            SendMatchesToAll();
                            break;
                        }
                    }

                    break;

                case Messages.Client.Move:
                    Match ourMatch = matches.Find(x => x.playerBlack.ip == e.IpPort);
                    if(ourMatch == null)
                    {
                        ourMatch = matches.Find(x => x.playerWhite.ip == e.IpPort);
                    }

                    if(e.IpPort == ourMatch.playerWhite.ip)
                    {
                        server.Send(ourMatch.playerBlack.ip, Encoding.UTF8.GetString(e.Data));
                    }
                    else
                    {
                        server.Send(ourMatch.playerWhite.ip, Encoding.UTF8.GetString(e.Data));
                    }                  
                    break;

                case Messages.Server.Matches:
                    Console.WriteLine(listOfMatches());
                    server.Send(e.IpPort, listOfMatches());
                    break;

                case Messages.Client.Cancel:
                    matches.RemoveAll(x => x.playerWhite.ip == e.IpPort);
                    SendMatchesToAll();
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
                    server.Send(e.IpPort,$"{Messages.Server.User}:{db.gameCount(messageData[1])}");
                    break;
                case Messages.Client.Register:
                    addUser(Encoding.UTF8.GetString(e.Data));
                    server.Send(e.IpPort, Messages.Server.Registered);
                    break;

                case Messages.Client.EndGame:  // endgame message + playercolor
                    Match thisMatch = matches.Find(x => x.playerWhite.ip == e.IpPort);
                    if (thisMatch == null)
                    {
                        thisMatch = matches.Find(x => x.playerBlack.ip == e.IpPort);
                    }
                    Player thisPlayerWhite = connectedClients.Find(x => x.ip == thisMatch.playerWhite.ip);
                    Player thisPlayerBlack = connectedClients.Find(x => x.ip == thisMatch.playerBlack.ip);
                    if (messageData[1] == "BLACK")
                    {
                        server.Send(e.IpPort, Messages.Server.Winner + ":" + thisPlayerWhite.name + ":" +
                            thisPlayerBlack.name + ":" + thisPlayerBlack.name);
                        server.Send(thisMatch.playerWhite.ip, Messages.Server.Lost + ":" + thisPlayerBlack.name);
                    }
                    else
                    {
                        server.Send(e.IpPort, Messages.Server.Winner + ":" + thisPlayerWhite.name + ":" +
                            thisPlayerBlack.name + ":" + thisPlayerWhite.name);
                        // do kogo, imie wygranego
                        server.Send(thisMatch.playerBlack.ip, Messages.Server.Lost + ":" + thisPlayerWhite.name);
                    }                 
                    break;

                case Messages.Client.SaveGame:
                    Player thisPlayer1 = connectedClients.Find(x => x.name == messageData[1]);
                    Player thisPlayer2 = connectedClients.Find(x => x.name == messageData[2]);
                    Player thisPlayer3 = connectedClients.Find(x => x.name == messageData[3]);
                    addGame(thisPlayer1.name + ":"+ thisPlayer2.name + ":"+ thisPlayer3.name);
                   
                    break;
                case Messages.Client.Surrender:
                    Match surrenderMatch = matches.Find(x => x.playerWhite.ip == e.IpPort);
                    if (surrenderMatch == null)
                    {
                        surrenderMatch = matches.Find(x => x.playerBlack.ip == e.IpPort);
                    }
                    Console.WriteLine($"{surrenderMatch.playerBlack.ip}:{surrenderMatch.playerWhite.ip}");
                    if (e.IpPort.Equals(surrenderMatch.playerBlack.ip))
                    {
                        server.Send(surrenderMatch.playerBlack.ip, $"{Messages.Server.Winner}:{surrenderMatch.playerBlack.name}:" +
                            $"{surrenderMatch.playerWhite.name}:{surrenderMatch.playerBlack.name}");
                        
                        server.Send(surrenderMatch.playerWhite.ip, $"{Messages.Server.Lost}:{surrenderMatch.playerBlack.name}");
                    }
                    else
                    {
                        server.Send(surrenderMatch.playerWhite.ip, $"{Messages.Server.Winner}:{surrenderMatch.playerWhite.name}" +
                            $":{surrenderMatch.playerBlack.name}:{surrenderMatch.playerWhite.name}");
                        server.Send(surrenderMatch.playerBlack.ip, $"{Messages.Server.Lost}:{surrenderMatch.playerWhite.name}");
                    }
                    break;

                default:
                    break;
            }
        }

        private static void Events_ClientDisconnected(object sender, ClientDisconnectedEventArgs e)
        {
            Console.WriteLine($"Client disconnected {e.IpPort}");
            matches.RemoveAll(x => x.playerWhite.ip == e.IpPort);
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

            if(listOfMatches.Length == 0)
            {
                return null;
            }

            foreach(Match match in matches)
            {
                if (match.playerBlack == null)
                {
                    listOfMatches.Append($":{match.playerWhite.name}");
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

            
            foreach (User u in db.Users)
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

        private static void SendMatchesToAll()
        {
            foreach (Player player in connectedClients)
            {
                if (!matches.Any(x => x.playerWhite.ip.Equals(player.ip) && x.playerBlack != null))
                {
                    server.Send(player.ip, listOfMatches());
                    continue;
                }
                if (matches.Where(x => x.playerBlack != null).Any(x => x.playerBlack.ip.Equals(player.ip)))
                {
                    server.Send(player.ip, listOfMatches());
                }
            }
        }


    }
}
