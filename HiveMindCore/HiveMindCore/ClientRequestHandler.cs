using System.Net.Security;

namespace HiveMindCore;

public class ClientRequestHandler : RequestHandler
{
    public ClientRequestHandler(SslStream client, ServerDataHolder holder)
    {
        HandleIt(client, holder);
    }

    protected override void SwitchRequest(string request)
    {
        switch (request)
        {
            case "getAllServers":
                GetAllServers();
                break;
            case "getServers":
                GetServers();
                break;
            //depreciated.
            case "getServer":
                GetServer();
                break;
            default:
                throw new Exception("Client request does not match any accepted by core. Given request was " + request + ".");
        }
    }

    private void GetServers()
    {
        int x = CoreCommunication.GetIntFromStream(client);
        int y = CoreCommunication.GetIntFromStream(client);
        int range = CoreCommunication.GetIntFromStream(client);
        
        List<string> tileRows = holder.GetServerRangeJsonData(x, y, range);
        
        CoreCommunication.SendIntToStream(client, tileRows.Count);

        foreach (var tileRow in tileRows)
        {
            CoreCommunication.SendStringToStream(client, tileRow);
        }
        
        Console.WriteLine("Client served.");
    }
    
    private void GetAllServers()
    {
        Console.WriteLine("GetAllServers not implemented!");
        /*
        IEnumerator enumerator = holder.servers.GetEnumerator();

        //Should be changed to issue single query to database instead of n!!!
        while (enumerator.MoveNext())
        {
            var serverData = (KeyValuePair<ServerDataHolder.Key, ServerDataHolder.ServerData>) enumerator.Current;

            byte[] serverBytes = Encoding.ASCII.GetBytes(holder.GetServerJsonData(serverData.Key.Dimension1, serverData.Key.Dimension2));
            
            client.Write(serverBytes, 0, serverBytes.Length);
        }

        ((IDisposable)enumerator).Dispose();*/
    }

    private void GetServer()
    {
        
        int x;
        int y;
        
        try
        {
            x = int.Parse(CoreCommunication.GetStringFromStream(client));
            y = int.Parse(CoreCommunication.GetStringFromStream(client));
        }
        catch
        {
            //throw new Exception("Invalid x and/or y coordinate received.");
            CoreCommunication.SendStringToStream(client, "INVALXY");
            return;
        }
        
        CoreCommunication.SendStringToStream(client, holder.GetServerJsonData(x, y));
    }
}