using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEditor.PackageManager;
using UnityEngine;

public static class CoreCommunication
{
    public static string GetStringFromStream(TcpClient client)
    {

        string request = "";
        byte[] buffer = new byte[1];

        while (IsConnected(client))
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


    public static bool IsConnected(TcpClient client)
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
