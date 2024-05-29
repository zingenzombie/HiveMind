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

            return;
        }
    }
}

holder.CreateServer(0, 0, "testName", address, 3622, "Zin");

Console.WriteLine(holder.GetServerJsonData("testName"));

return;

static void IncomingConnectionHandler(TcpClient client, ServerDataHolder holder)
{

    Console.WriteLine("HIT");

    while(client.Available < 6){}
    
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