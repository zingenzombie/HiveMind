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
        
        while (IsConnected())
        {

            if (!(client.Available > 0))
                continue;
        
            client.GetStream().Read(buffer, 0, 1);

            if (((char)buffer[0]).Equals('\n'))
                return request;
        
            request += System.Text.Encoding.UTF8.GetString(buffer);
        }

        throw new Exception("Client disconnected before receiving a '\\n' character.");
    }
    
    //Thank you to Jalal Said from https://stackoverflow.com/questions/6993295/how-to-determine-if-the-tcp-is-connected-or-not!
    protected bool IsConnected()
    {
        try
        {
            if (client != null && client.Client != null && client.Client.Connected)
            {
                /* pear to the documentation on Poll:
                 * When passing SelectMode.SelectRead as a parameter to the Poll method it will return
                 * -either- true if Socket.Listen(Int32) has been called and a connection is pending;
                 * -or- true if data is available for reading;
                 * -or- true if the connection has been closed, reset, or terminated;
                 * otherwise, returns false
                 */

                // Detect if client disconnected
                if (client.Client.Poll(0, SelectMode.SelectRead))
                {
                    byte[] buff = new byte[1];
                    if (client.Client.Receive(buff, SocketFlags.Peek) == 0)
                    {
                        // Client disconnected
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }
        catch
        {
            return false;
        }
    }
}