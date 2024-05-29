namespace HiveMindCore;
using System.Net.Sockets;
using System.Text;

public class RequestHandler
{
    protected TcpClient client; 
    protected ServerDataHolder holder;
    
    protected RequestHandler(){}
    
    protected void HandleIt(TcpClient client, ServerDataHolder holder)
    {

        this.client = client;
        this.holder = holder;
        
        try
        {
            SwitchRequest(GetStringFromStream());
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

    protected string GetStringFromStream()
    {

        string request = "";
        byte[] buffer = new byte[1];
        
        while (client.Connected){
            if (client.Available > 0)
            {
                client.GetStream().Read(buffer, 0, 1);

                if (((char)buffer[0]).Equals('\n'))
                    return request;
            
                request += System.Text.Encoding.UTF8.GetString(buffer);
            }
        }

        throw new Exception("Client disconnected before receiving a '\\n' character.");
    }
    
}