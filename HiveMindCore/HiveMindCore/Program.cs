// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using HiveMindCore;

static async Task<IPAddress?> GetExternalIpAddress()
{
    var externalIpString = (await new HttpClient().GetStringAsync("http://icanhazip.com"))
        .Replace("\\r\\n", "").Replace("\\n", "").Trim();
    if(!IPAddress.TryParse(externalIpString, out var ipAddress)) return null;
    return ipAddress;
}

Console.WriteLine("Good meowning.");

IPAddress address = GetExternalIpAddress().Result;
int port = 3621;

Console.WriteLine("Starting server on " + address + ":" + port + ".");

ServerDataHolder holder = new ServerDataHolder();

Thread maintainer = new Thread(() => MaintainList());
maintainer.Start();

TcpListener server = new TcpListener(IPAddress.Any, port);

server.Start();  // this will start the server

while (true)   //we wait for a connection
{
    TcpClient client;
    if (server.Pending())
    {
        client = server.AcceptTcpClient();  //if a connection exists, the server will accept it

        if (client.Connected)  //while the client is connected, we look for incoming messages
        {

            Thread thread = new Thread(() => IncomingConnectionHandler(client, holder));
            thread.Start();
        }
    }
}
return;

static void IncomingConnectionHandler(TcpClient client, ServerDataHolder holder)
{

    Console.WriteLine("HIT");

    while(client.Available < 6 && client.Connected){}
    
    byte[] buffer = new byte[6];

    client.GetStream().Read(buffer, 0, 6);

    Console.WriteLine(System.Text.Encoding.UTF8.GetString(buffer));

    if (System.Text.Encoding.UTF8.GetString(buffer).Equals("server"))
    {
        Console.WriteLine("Server Hit");
        ServerRequestHandler tmp = new ServerRequestHandler(client, holder);
    }
    else if (System.Text.Encoding.UTF8.GetString(buffer).Equals("client"))
    {
        Console.WriteLine("Client Hit");
        ClientRequestHandler tmp = new ClientRequestHandler(client, holder);
    }
}

void MaintainList()
{
    while (true)
    {
        Thread.Sleep(15000); //Run every 15 seconds.
        foreach (var serverData in holder.servers)
        {
            using TcpClient tcpClient = new TcpClient(serverData.Ip, serverData.Port);
            
            if (!tcpClient.Connected)
            {
                Console.WriteLine("Failed to connect to core!");
                holder.DeleteServer(serverData);
            }
            byte[] buffer = new byte[6];
            buffer[0] = (byte)'s';
            buffer[1] = (byte)'e';
            buffer[2] = (byte)'r';
            buffer[3] = (byte)'v';
            buffer[4] = (byte)'e';
            buffer[5] = (byte)'r';
            tcpClient.GetStream().Write(buffer, 0, buffer.Length);
            
        }
    }
}