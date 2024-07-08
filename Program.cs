using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Program
{
    private static List<TcpClient> clients = new List<TcpClient>();
    private static int counter = 1;

    static void Main(string[] args)
    {
        TcpListener server = new TcpListener(IPAddress.Any, 8888);
        server.Start();
        Console.WriteLine("Server started.");

        Thread acceptClientsThread = new Thread(AcceptClients);
        acceptClientsThread.Start(server);

        while (true)
        {
            if (clients.Count > 0)
            {
                SendMessageToClients(counter.ToString());
                counter++;
            }
            Thread.Sleep(60000); // 1 minute
        }
    }

    private static void AcceptClients(object obj)
    {
        TcpListener server = (TcpListener)obj;
        while (clients.Count < 3)
        {
            TcpClient client = server.AcceptTcpClient();
            lock (clients)
            {
                clients.Add(client);
                int clientIndex = clients.IndexOf(client) + 1;
                Console.WriteLine($"Client {clientIndex} connected.");
                Thread clientThread = new Thread(() => HandleClient(client, clientIndex));
                clientThread.Start();
            }
        }
    }

    private static void HandleClient(TcpClient client, int clientIndex)
    {
        try
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[256];
            int bytesRead;
            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
            {
                string data = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"Received from client {clientIndex}: {data}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Client {clientIndex} disconnected: {ex.Message}");
        }
        finally
        {
            client.Close();
            lock (clients)
            {
                clients.Remove(client);
                Console.WriteLine($"Client {clientIndex} disconnected.");
            }
        }
    }

    private static void SendMessageToClients(string message)
    {
        byte[] data = Encoding.ASCII.GetBytes(message);
        lock (clients)
        {
            foreach (TcpClient client in clients)
            {
                try
                {
                    NetworkStream stream = client.GetStream();
                    stream.Write(data, 0, data.Length);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending message to a client: {ex.Message}");
                }
            }
        }
        Console.WriteLine($"Sent message to all clients: {message}");
    }
}
