using System.Net.Security;

namespace HiveMindCore;
using System.Net.Sockets;
using System.Text;

public class RequestHandler
{
    protected SslStream client; 
    protected ServerDataHolder holder;
    
    protected RequestHandler(){}
    
    protected void HandleIt(SslStream client, ServerDataHolder holder)
    {

        this.client = client;
        this.holder = holder;
        
        try
        {
            SwitchRequest(CoreCommunication.GetStringFromStream(client));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        
        client.Close();
    }

    protected virtual void SwitchRequest(string request)
    {
        throw new Exception("RequestHandler:SwitchRequest was called, which should not happen. Please call an inherited instance of RequestHandler (ServerRequestHandler or ClientRequestHandler are available to you by default).");
    }
}