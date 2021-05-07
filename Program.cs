﻿using SimpleTcp;
using System;
using System.Collections.Generic;
using System.Text;

namespace TCPserver
{
    class Program
    {
        public static List<String> connectedClients = new List<string>();
        public static List<Match> matches = new List<Match>();
        static SimpleTcpServer server = new SimpleTcpServer("127.0.0.1:8001");
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
                    foreach(string client in connectedClients)
                    {
                        Console.WriteLine(client);
                    }
                }
            }
        }

        private static void Events_DataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine($"Dane od clienta: {e.IpPort} -> {Encoding.UTF8.GetString(e.Data)}");

            switch(Encoding.UTF8.GetString(e.Data).Split(':')[0])
            {
                case Messages.Client.Host:
                    matches.Add(new Match(e.IpPort));
                    Console.WriteLine($"Gracz: {e.IpPort} stworzył nową gre");
                    break;
                case Messages.Client.Join:
                    matches[0].playerBlack = e.IpPort;
                    server.Send(matches[0].playerWhite, Messages.Server.Start + ":WHITE");
                    server.Send(matches[0].playerBlack, Messages.Server.Start + ":BLACK");
                    break;
                case Messages.Client.Move:
                    if(e.IpPort == matches[0].playerWhite)
                    {
                        server.Send(matches[0].playerBlack, Encoding.UTF8.GetString(e.Data));
                    }
                    else
                    {
                        server.Send(matches[0].playerWhite, Encoding.UTF8.GetString(e.Data));
                    }                  
                    break;
                default:
                    break;
            }
        }

        private static void Events_ClientDisconnected(object sender, ClientDisconnectedEventArgs e)
        {
            Console.WriteLine($"Client disconnected {e.IpPort}");
            connectedClients.Remove(e.IpPort);       
        }

        private static void Events_ClientConnected(object sender, ClientConnectedEventArgs e)
        {
            Console.WriteLine($"New client connected {e.IpPort}");
            server.Send(e.IpPort, "Witaj");
            connectedClients.Add(e.IpPort);           
        }
    }
}
