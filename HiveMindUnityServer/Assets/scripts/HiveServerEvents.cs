using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using Unity.VisualScripting;
using UnityEngine;

public class HiveServerEvents : MonoBehaviour
{
    public delegate void CoreMessageReceived();
    public static event CoreMessageReceived OnCoreMessageReceived;

    public static void callCoreMessageReceived()
    {

    }

    public delegate void InitStatusFromCore(string message);
    public static event InitStatusFromCore OnInitStatusFromCore;

    public static void callInitCoreStatus(string message)
    {
        OnInitStatusFromCore?.Invoke(message);
    }

    public delegate void PlayerJoined(TileStream tcpClient);
    public static event PlayerJoined OnPlayerJoined;

    public static void callPlayerJoined(TileStream tcpClient)
    {
        OnPlayerJoined?.Invoke(tcpClient);
    }

    public delegate void GlobalMessage(PlayerData originator, NetworkMessage msg);
    public static event GlobalMessage OnGlobalPlayerMessage;

    public static void callGlobalPlayerMessage(PlayerData originator, NetworkMessage msg)
    {
        OnGlobalPlayerMessage?.Invoke(originator, msg);
    }
}
