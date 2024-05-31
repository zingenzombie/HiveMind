using System.Collections;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace HiveMindCore;

public class ClientRequestHandler : RequestHandler
{
    public ClientRequestHandler(TcpClient client, ServerDataHolder holder)
    {
        HandleIt(client, holder);
    }

    protected override void SwitchRequest(string request)
    {
        switch (request)
        {
            case "getAllServers":
                Console.WriteLine("Fetching servers...");
                GetAllServers();
                break;
            case "getServer":
                Console.WriteLine("Fetching server...");
                GetServer();
                break;
            default:
                throw new Exception("Client request does not match any accepted by core. Given request was " + request + ".");
        }
    }

    private void GetAllServers()
    {
        IEnumerator enumerator = holder.servers.GetEnumerator();

        while (enumerator.MoveNext())
        {
            var serverData = (KeyValuePair<ServerDataHolder.Key, ServerDataHolder.ServerData>) enumerator.Current;

            byte[] serverBytes = Encoding.ASCII.GetBytes(holder.GetServerJsonData(serverData.Key.Dimension1, serverData.Key.Dimension2));
            
            client.GetStream().Write(serverBytes, 0, serverBytes.Length);
        }

        ((IDisposable)enumerator).Dispose();
    }

    private void GetServer()
    {
        int x = 0;
        int y = 0;
        
        try
        {
            x = int.Parse(GetStringFromStream());
            y = int.Parse(GetStringFromStream());
        }
        catch
        {
            throw new Exception("Invalid x and/or y coordinate received.");
        }

        Console.WriteLine(x + ", " + y + " requested.");
        
        ServerDataHolder.ServerData serverData;

        byte[] noServer = Encoding.ASCII.GetBytes("DoesNotExist\n");

        if (!holder.servers.TryGetValue(new ServerDataHolder.Key(x, y), out serverData))
        {
            client.GetStream().Write(noServer);
            return;
        }

        byte[] serverDataBytes = Encoding.ASCII.GetBytes(serverData.GetServerJsonData() + '\n');
        client.GetStream().Write(serverDataBytes);
    }
}