using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Text;

namespace Server
{
    class Program
    {
        static TcpListener tcpListener;
        static List<ClientInfo> connectedClients = new List<ClientInfo>();
        static List<string> messageHistory = new List<string>();
        static int maxUsers;
        static readonly object lockObj = new object();

        class ClientInfo
        {
            public TcpClient TcpClient { get; set; }
            public string Nickname { get; set; }

            static void Main()
            {

                Console.Write("Enter maximum users: ");
                maxUsers = int.Parse(Console.ReadLine());

                Console.Write("Enter Port : ");
                int port = int.Parse(Console.ReadLine());

                Console.Clear();
                Console.WriteLine($"Server started. on port {port}\n [#] CTRL + C to close the Server");

                tcpListener = new TcpListener(IPAddress.Any, port);
                tcpListener.Start();

                while(true)
                {
                    TcpClient client = tcpListener.AcceptTcpClient();
                    string nickname = GetNickname(client);

                    lock(lockObj)
                    {
                        if(connectedClients.Count >= maxUsers)
                        {
                            SendRoomFullMessage(client);
                            continue;
                        }

                        connectedClients.Add(new ClientInfo { TcpClient = client, Nickname = nickname });

                        SendAuthorizationMessage(client, nickname);
                        SendHistoryToClient(client);
                        BroadcastParticipantCount();
                        BroadcastNewMessage($"{nickname} has joined the room.");
                        SendConnectedClientsList(client);

                        Thread clientThread = new Thread(() => HandleClientComm(client, nickname));
                        clientThread.Start();
                    }
                }
            }
            private static void SendConnectedClientsList(TcpClient client)
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine("Current participants:");
                for(int i = 0; i < connectedClients.Count; i++)
                {
                    builder.AppendLine($"{i + 1}. {connectedClients[i].Nickname}");
                }

                NetworkStream stream = client.GetStream();
                byte[] data = Encoding.ASCII.GetBytes(builder.ToString() + "\n");
                stream.Write(data, 0, data.Length);
            }
            private static void BroadcastNewMessage(string message)
            {
                foreach(var clientInfo in connectedClients)
                {
                    NetworkStream stream = clientInfo.TcpClient.GetStream();
                    byte[] data = Encoding.ASCII.GetBytes(message + "\n");
                    stream.Write(data, 0, data.Length);
                }
            }
            private static void BroadcastParticipantCount()
            {
                string participantCountMessage = $"Current participants: {connectedClients.Count}/{maxUsers}";

                foreach(var clientInfo in connectedClients)
                {
                    NetworkStream stream = clientInfo.TcpClient.GetStream();
                    byte[] data = Encoding.ASCII.GetBytes(participantCountMessage + "\n");
                    stream.Write(data, 0, data.Length);
                }
            }
            private static void SendRoomFullMessage(TcpClient client)
            {
                NetworkStream stream = client.GetStream();
                byte[] message = Encoding.ASCII.GetBytes("Room Full. Please wait until a slot becomes available.\n");
                stream.Write(message, 0, message.Length);
            }

            private static void HandleClientComm(TcpClient tcpClient, string nickname)
            {
                IPEndPoint clientEndPoint = tcpClient.Client.RemoteEndPoint as IPEndPoint;
                if(clientEndPoint != null)
                {
                    string clientIpAddress = clientEndPoint.Address.ToString();

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"{clientIpAddress} has connected at {DateTime.Now:HH:mm:ss} by the Alias :: {nickname}");
                    Console.ResetColor();
                    NetworkStream clientStream = tcpClient.GetStream();

                    byte[] message = new byte[4096];
                    int bytesRead;
                    while(true)
                    {
                        bytesRead = 0;

                        try
                        {
                            bytesRead = clientStream.Read(message, 0, 4096);
                        }
                        catch
                        {
                            break;
                        }

                        if(bytesRead == 0)
                            break;

                        string clientMessage = $"{nickname} @ [{DateTime.Now:HH:mm:ss}]:\n {Encoding.ASCII.GetString(message, 0, bytesRead)}\n";
                        Console.WriteLine($"msg from {nickname} :: ({clientIpAddress}) || [{DateTime.Now:HH:mm:ss}] : {clientMessage}\n");

                        AddToHistory($"{nickname} has joined @ {DateTime.Now:HH:mm:ss}\n");
                        AddToHistory($"{clientMessage} @ {DateTime.Now:HH:mm:ss}\n");
                        BroadcastMessage(clientMessage, tcpClient);
                    }
                }

                connectedClients.RemoveAll(c => c.TcpClient == tcpClient);
                tcpClient.Close();
            }

            private static void BroadcastMessage(string message, TcpClient senderClient)
            {
                foreach(ClientInfo clientInfo in connectedClients)
                {
                    if(clientInfo.TcpClient != senderClient)
                    {
                        if(clientInfo.TcpClient != null)
                        {
                            NetworkStream stream = clientInfo.TcpClient.GetStream();
                            byte[] data = Encoding.ASCII.GetBytes(message);
                            stream.Write(data, 0, data.Length);
                        }
                    }
                }
            }
            private static void SendHistoryToClient(TcpClient client)
            {
                foreach(string historyMessage in messageHistory)
                {
                    NetworkStream stream = client.GetStream();
                    byte[] data = Encoding.ASCII.GetBytes(historyMessage);
                    stream.Write(data, 0, data.Length);
                }
            }

            private static void AddToHistory(string message)
            {
                messageHistory.Add(message);
            }

            private static void SendAuthorizationMessage(TcpClient client, string nickname)
            {
                NetworkStream stream = client.GetStream();
                byte[] authMessage = Encoding.ASCII.GetBytes($"Authorised as {nickname} , Welcome\n");
                stream.Write(authMessage, 0, authMessage.Length);
            }

            private static string GetNickname(TcpClient client)
            {
                NetworkStream stream = client.GetStream();
                byte[] nicknamePrompt = Encoding.ASCII.GetBytes("Type Your Alias : ");
                stream.Write(nicknamePrompt, 0, nicknamePrompt.Length);

                byte[] nicknameBuffer = new byte[4096];
                int bytesRead = stream.Read(nicknameBuffer, 0, 4096);
                return Encoding.ASCII.GetString(nicknameBuffer, 0, bytesRead).Trim();
            }
        }
    }
}