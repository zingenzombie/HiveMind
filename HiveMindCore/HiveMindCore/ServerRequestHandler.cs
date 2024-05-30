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
                Console.WriteLine("Creating new server...");
                NewServer();
                break;
            default:
                throw new Exception("Server request does not match any accepted by core. Given request was " + request + ".");
        }
    }

    private void NewServer()
    {
        //client.GetStream().Write(Encoding.ASCII.GetBytes("howdy server!"), 0, Encoding.ASCII.GetBytes("howdy server!").Length);

        string serverRequest = GetStringFromStream();
        Console.WriteLine(serverRequest);

        holder.CreateServer(serverRequest);
    }
}