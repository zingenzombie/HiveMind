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
        //CoreCommunication.SendStringToStream(client, "Core is requesting your server info... ");
        string serverRequest = CoreCommunication.GetStringFromStream(client);
        //CoreCommunication.SendStringToStream(client, "(CORE RECEIVED SERVER DATA)");

        if (!holder.CreateServer(serverRequest, client))
            CoreCommunication.SendStringToStream(client, "FAIL (could not parse your data json into a 'serverdata' type in the core server)");
    }
}