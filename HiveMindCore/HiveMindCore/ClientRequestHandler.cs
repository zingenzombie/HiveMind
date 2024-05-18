using System.Net.Sockets;

namespace HiveMindCore;

public class ClientRequestHandler
{
    ClientRequestHandler(TcpClient client)
    {
        if (client.Connected)  //while the client is connected, we look for incoming messages
        {
            return;
        }
        else
        {
            Console.WriteLine("Error! TCP client connection attempt received, but connection failed before handling!");
        }
    }
}