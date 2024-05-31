using System.Linq.Expressions;
using System.Net.Sockets;
using System.Text;

namespace HiveMindCore;

public class ServerRequestHandler : RequestHandler
{
    public ServerRequestHandler(TcpClient client, ServerDataHolder holder)
    {
        HandleIt(client, holder);
    }

    protected override void SwitchRequest(string request)
    {
        switch (request)
        {
            case "newServer":
                NewServer();
                break;
            default:
                throw new Exception("Server request does not match any accepted by core. Given request was " + request + ".");
        }
    }

    private void NewServer()
    {
        string serverRequest = GetStringFromStream();
        Console.WriteLine(serverRequest);

        if (!holder.CreateServer(serverRequest))
        {
            client.GetStream().Write("FAIL\n"u8);
            return;
        }
        
        client.GetStream().Write("SUCCESS\n"u8);
    }
}