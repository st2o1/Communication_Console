using System.Text;
using System.Net.Sockets;
using System;
using System.Threading;

namespace User
{
    class Program
    {
        static TcpClient tcpClient;
        static NetworkStream clientStream;

        static void Main()
        {
            Console.Write("Enter server IP address : ");
            string serverIp = Console.ReadLine();

            Console.Write("Enter server Port : ");
            int port = int.Parse(Console.ReadLine());

            if (!string.IsNullOrEmpty(serverIp) )
            {
                ConnectToServer(serverIp, port);
            }
           
            Console.WriteLine($"Connected to the server.\n Type (exit) to leave\n {DateTime.Now:HH:mm:ss} \n!===========!\n");

            Thread recieveThread = new Thread(ReceiveMessages);
            recieveThread.Start();

            while(true)
            {
                string message = Console.ReadLine();
                if(message != null )
                {
                    if(message.ToLower() == "exit")
                        break;
                    SendMessage(message);
                }
            }
            if(tcpClient != null)
            {
                tcpClient.Close();
            }
        }

        static void ConnectToServer(string serverIp, int port)
        {
            tcpClient = new TcpClient();
            tcpClient.Connect(serverIp, port);
            clientStream = tcpClient.GetStream();
        }

        static void SendMessage(string message)
        {
            if(clientStream != null)
            {
                byte[] data = Encoding.ASCII.GetBytes(message);
                clientStream.Write(data, 0, data.Length);
            }
            else
            { }
        }
        static void ReceiveMessages()
        {
            byte[] message = new byte[4096];
            int bytesRead;

            while(true)
            {
                bytesRead = 0;

                try
                {
                    if(clientStream != null)
                    {
                        bytesRead = clientStream.Read(message, 0, 4096);
                    }
                }
                catch
                {
                    break;
                }

                if(bytesRead == 0)
                    break;

                string serverMessage = Encoding.ASCII.GetString(message, 0, bytesRead);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n |#|  {serverMessage} \n");
                Console.ResetColor();
            }
        }
    }
}
