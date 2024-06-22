using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class NetworkController
{
    public static HexTileController activeServer;

    public static void SendTCPMessage(NetworkMessage message)
    {
        if (activeServer == null)
            return;

        try
        {
            activeServer.serverTCPPipe.Add(message);
        }
        catch (System.Exception) { }
    }

    public static void SendUDPMessage(NetworkMessage message)
    {
        if (activeServer == null)
            return;

        try
        {
            activeServer.serverUDPPipe.Add(message);
        }
        catch (System.Exception) { }
    }
}
