// See https://aka.ms/new-console-template for more information

using System.Collections;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using HiveMindCore;

X509Certificate2 serverCertificate;

ServerDataHolder holder;

Main();

void Main()
{
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

    //Thread maintainer = new Thread(() => MaintainList());
    //maintainer.Start();

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

                Thread thread = new Thread(() => PromoteToSsl(client));
                thread.Start();
            }
        }
    } 
}

static X509Certificate2 GatherCertificate()
{

    String CERT_ADDR = "/root/honeydragonproductions.com-ssl-bundle/domain.pfx";
    String PWD_ADDR = "/root/honeydragonproductions.com-ssl-bundle/pwd.txt";

    string pwd = File.ReadAllText(PWD_ADDR);
    
    return new X509Certificate2(CERT_ADDR, pwd);
}

void IncomingConnectionHandler(SslStream sslStream)
{
        
    // Read a message from the client.
    Console.WriteLine("Waiting for client message...");
    string messageData = CoreCommunication.GetStringFromStream(sslStream);
    Console.WriteLine("Received: {0}", messageData);

    switch (messageData)
    {
        case "server":
            ServerRequestHandler tmpS = new ServerRequestHandler(sslStream, holder);
            break;
        case "client":
            ClientRequestHandler tmpC = new ClientRequestHandler(sslStream, holder);
            break;
        default:
            break;
    }
}

void PromoteToSsl(TcpClient client)
{
    SslStream sslStream = new SslStream(client.GetStream(), false);
    
    try
    {
        sslStream.AuthenticateAsServer(serverCertificate, clientCertificateRequired: false, checkCertificateRevocation: true);
        
        // Set timeouts for the read and write to 5 seconds.
        sslStream.ReadTimeout = 5000;
        sslStream.WriteTimeout = 5000;
        IncomingConnectionHandler(sslStream);
    }
    catch (AuthenticationException e)
    {
        Console.WriteLine("Exception: {0}", e.Message);
        if (e.InnerException != null)
        {
            Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
        }
        Console.WriteLine ("Authentication failed - closing the connection.");
        sslStream.Close();
        client.Close();
        return;
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

        while (enumerator.MoveNext())
        {
            var serverData = (KeyValuePair<ServerDataHolder.Key, ServerDataHolder.ServerData>) enumerator.Current;

            TcpClient tcpClient;

            try
            {
                tcpClient = new TcpClient(serverData.Value.Ip, serverData.Value.Port);
            }
            catch
            {
                Console.WriteLine("Goodbye " + serverData.Value.X + ", " + serverData.Value.Y + ".");
                holder.DeleteServer(serverData.Value.X, serverData.Value.Y);
                continue;
            }
            
            if (!tcpClient.Connected)
            {
                Console.WriteLine("Goodbye " + serverData.Value.X + ", " + serverData.Value.Y + ".");
                holder.DeleteServer(serverData.Value.X, serverData.Value.Y);
            }
            tcpClient.GetStream().Write("\n"u8);
        }
        ((IDisposable)enumerator).Dispose();
    }
}