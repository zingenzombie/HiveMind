using System.Linq.Expressions;
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
            case "getServers":
                Console.WriteLine("Fetching servers...");
                GetServers();
                break;
            default:
                throw new Exception("Client request does not match any accepted by core. Given request was " + request + ".");
        }
    }

    private void GetServers()
    {
        client.GetStream().Write(Encoding.ASCII.GetBytes("howdy server!"), 0, Encoding.ASCII.GetBytes("howdy server!").Length);

        string serverRequest = GetStringFromStream();
        Console.WriteLine(serverRequest);

        holder.CreateServer(serverRequest);
    }
}