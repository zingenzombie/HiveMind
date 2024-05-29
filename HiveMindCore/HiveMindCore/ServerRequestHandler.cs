using System.Linq.Expressions;
using System.Net.Sockets;

namespace HiveMindCore;

public class ServerRequestHandler
{
    private TcpClient server; 
    ServerDataHolder holder;
    
    public ServerRequestHandler(TcpClient server, ServerDataHolder holder)
    {

        this.server = server;
        this.holder = holder;

        string request = "";
        byte[] buffer = new byte[1];
        
        while (server.Available > 0 && server.Connected){

            server.GetStream().Read(buffer, 0, 1);

            if (((char)buffer[0]).Equals('\n'))
            {
                Console.WriteLine(request);
                SwitchRequest(request);
                break;
            }
            
            request += System.Text.Encoding.UTF8.GetString(buffer);

        }
    }

    private void SwitchRequest(string request)
    {
        switch (request)
        {
            case "newServer":
                Console.WriteLine("Creating new server...");
                NewServer();
                break;
            default:
                throw new Exception("Server request does not match any accepted by core.");
        }
    }

    private void NewServer()
    {
        
    }
}