using System.Diagnostics;
using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;

namespace HiveMindCore;

public class ClientRequestHandler
{
    public ClientRequestHandler(TcpClient client, ServerDataHolder holder)
    {
        while (client.Connected) //while the client is connected, we look for incoming messages
        {
            if (client.Available > 0)
            {
                byte[] buffer = new byte[client.Available];

                client.GetStream().Read(buffer, 0, client.Available);

                Console.WriteLine(System.Text.Encoding.UTF8.GetString(buffer));

            }

            return;
        }
    }
}