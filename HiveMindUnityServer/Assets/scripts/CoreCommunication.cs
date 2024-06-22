using System;
using System.Net.Sockets;

public static class CoreCommunication
{

    //Any calls to GetStringFromStream() MUST NOT be called
    //from the main thread to prevent hanging in the case of
    //a lagging \n character.
    public static string GetStringFromStream(TcpClient client)
    {
        string request = "";
        byte[] buffer = new byte[1];

        while (true)
        {
            int read;

            try
            {
                read = client.GetStream().Read(buffer, 0, 1);
            }catch (Exception) { return null; }

            if (read == 0)
            {
                if (IsConnected(client))
                    continue;

                throw new Exception("Client disconnected before receiving a '\\n' character.");
            }

            if (((char)buffer[0]).Equals('\n'))
                return request;

            request += System.Text.Encoding.UTF8.GetString(buffer);
        }

        throw new Exception("Client disconnected before receiving a '\\n' character.");
    }

    public static bool IsConnected(TcpClient client)
    {
        try
        {
            if (client != null && client.Client != null && client.Client.Connected)
            {
                // Detect if client disconnected
                if (client.Client.Poll(0, SelectMode.SelectRead))
                {
                    byte[] buff = new byte[1];
                    if (client.Client.Receive(buff, SocketFlags.Peek) == 0)
                        // Client disconnected
                        return false;
                    else
                        return true;
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
