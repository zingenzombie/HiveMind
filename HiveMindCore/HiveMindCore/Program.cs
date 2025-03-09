// See https://aka.ms/new-console-template for more information

using System.Collections;
using System.ComponentModel;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using HiveMindCore;

X509Certificate2 serverCertificate;

ServerDataHolder holder;

bool maintain = true;

Main(args);

void HandleArgs(string[] args)
{
    foreach (var s in args)
    {
        switch (s)
        {
            case "-disableMaintainer":
                maintain = false;
                break;
            default:
                Console.WriteLine("Invalid argument received!");
                throw new Exception("Invalid command-line argument received.");
        }
    }
}

void LiveControls()
{
    while (true)
    {
        string[] args = Console.ReadLine().Split(' ');

        switch (args[0])
        {
            //remove a server at a location
            case "rmserv":
                if (args.Length != 3)
                {
                    Console.WriteLine("Please also append the server tile you'd like to remove");
                    return;
                }
                var retrievedVals = int.TryParse(args[1], out int X) & int.TryParse(args[2], out int Y);
                if(retrievedVals)
                    holder.DeleteServer(X, Y);
                else
                    Console.WriteLine("Invalid Tile Location Passed In");
                break;
            case "showtiles":
                holder.PrintServers();
                break;
            case "help":
                Console.WriteLine("CONSOLE CONTROLS FOR HIVE SERVER");
                Console.WriteLine("'rmserv' <X> <Y> | To remove the server at a specific tile location");
                Console.WriteLine("'showtiles' | shows all of the tiles that have servers attached to them including info");
                break;
            default:
                Console.WriteLine("Invalid Argument");
                break;
        }
    }
}

void Main(string[] args)
{

    HandleArgs(args);
    
    static async Task<IPAddress?> GetExternalIpAddress()
    {
        var externalIpString = (await new HttpClient().GetStringAsync("http://icanhazip.com"))
            .Replace("\\r\\n", "").Replace("\\n", "").Trim();
        if(!IPAddress.TryParse(externalIpString, out var ipAddress)) return null;
        return ipAddress;
    }

    Console.WriteLine("Good morning.");

    IPAddress address = GetExternalIpAddress().Result;
    int port = 3621;

    //Gather certificate
    serverCertificate = GatherCertificate();

    Console.WriteLine("Starting server on " + address + ":" + port + ".");

    holder = new ServerDataHolder();

    if (maintain)
    {
        Thread maintainer = new Thread(() => MaintainList());
        maintainer.Start();
    }

    Thread liveControls = new Thread(() => LiveControls());
    liveControls.Start();

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
                Console.Write("Incoming connection request... (Verifying)... ");
                new Thread(() => PromoteToSsl(client)).Start();
            }
        }
    } 
}

static X509Certificate2 GatherCertificate()
{

    String CERT_ADDR = "/honeydragonproductions.com-ssl-bundle/certificate.pfx";
    String PWD_ADDR = "/honeydragonproductions.com-ssl-bundle/pwd";

    string pwd = File.ReadAllText(PWD_ADDR);

    pwd = pwd.Substring(0, pwd.Length - 1);

    return new X509Certificate2(CERT_ADDR, pwd);
}

void IncomingConnectionHandler(SslStream sslStream)
{
    //CoreCommunication.SendStringToStream(sslStream, "This connection has been verified by the core...");
    //CoreCommunication.SendStringToStream(sslStream, "Core is requesting your connection type...");
    // Read a message from the client.
    Console.Write("Requesting Connection Info... ");
    string connectionType = CoreCommunication.GetStringFromStream(sslStream);
    Console.WriteLine($"Info Received (connection is of type): {connectionType}");

    switch (connectionType)
    {
        case "server":
            //CoreCommunication.SendStringToStream(sslStream, $"(TYPE SUCCESS) you are recognized as a {connectionType.ToUpper()}");
            ServerRequestHandler tmpS = new ServerRequestHandler(sslStream, holder);
            break;
        case "client":
            //CoreCommunication.SendStringToStream(sslStream, $"(TYPE SUCCESS) you are recognized as a {connectionType.ToUpper()}");
            ClientRequestHandler tmpC = new ClientRequestHandler(sslStream, holder);
            break;
        default:
            CoreCommunication.SendStringToStream(sslStream, $"ERROR: type incompatibility (you must specify the whether you are a server or a client)");
            break;
    }
}

void PromoteToSsl(TcpClient client)
{
    
    Console.WriteLine(client.Client);
    
    SslStream sslStream = new SslStream(client.GetStream(), false);
     
    try
    {
        sslStream.AuthenticateAsServer(serverCertificate, clientCertificateRequired: false, checkCertificateRevocation: true);
        
        Console.Write("VERIFIED! \n");
        // Set timeouts for the read and write to 5 seconds.
        sslStream.ReadTimeout = 5000;
        sslStream.WriteTimeout = 5000;
        IncomingConnectionHandler(sslStream);
    }
    catch (AuthenticationException e)
    {
        Console.Write("FAILED \n");
        Console.WriteLine("Exception: {0}", e.Message);
        if (e.InnerException != null)
        {
            Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
        }
        Console.WriteLine ("Authentication failed - closing the connection.");
        sslStream.Close();
        client.Close();
    }
    finally
    {
        // The client stream will be closed with the sslStream
        // because we specified this behavior when creating
        // the sslStream.
        sslStream.Close();
        client.Close();
    }
}

void MaintainList()
{
    while (true)
    {
        Thread.Sleep(5000); //Run every 5 seconds.
        IEnumerator enumerator = holder.servers.GetEnumerator();
        Console.WriteLine("HeartBeat");

        while (enumerator.MoveNext())
        {

            var serverData = (KeyValuePair<ServerDataHolder.Key, ServerDataHolder.ServerData>) enumerator.Current;

            TileStream tcpClient;

            try
            {
                Console.WriteLine($"Trying connection with Tile Server {"10.20.0.59"},{serverData.Value.Port}");
                tcpClient = new TileStream(new TcpClient("10.20.0.59", serverData.Value.Port));
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
                Console.WriteLine("Goodbye " + serverData.Value.X + ", " + serverData.Value.Y + ".");
                holder.DeleteServer(serverData.Value.X, serverData.Value.Y);
                continue;
            }
            
            if (!tcpClient.Connected)
            {
                Console.WriteLine("Goodbye " + serverData.Value.X + ", " + serverData.Value.Y + ".");
                holder.DeleteServer(serverData.Value.X, serverData.Value.Y);
            }
            
            tcpClient.SendStringToStream("check");
        }
        ((IDisposable)enumerator).Dispose();
    }
}