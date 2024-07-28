using System.Linq.Expressions;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;

namespace HiveMindCore;

public class ServerRequestHandler : RequestHandler
{
    public ServerRequestHandler(SslStream client, ServerDataHolder holder)
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
        string serverRequest = CoreCommunication.GetStringFromStream(client);
        Console.WriteLine(serverRequest);

        if (!holder.CreateServer(serverRequest))
        {
            CoreCommunication.SendStringToStream(client, "FAIL");
            return;
        }
        
        CoreCommunication.SendStringToStream(client, "SUCCESS");
    }
}