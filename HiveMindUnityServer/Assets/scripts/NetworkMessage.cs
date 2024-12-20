using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkMessage
{
    public string messageType;
    public string spawningClient;
    public int numBytes;
    public byte[] message;

    public NetworkMessage(string spawningClient, string messageType, byte[] message)
    {
        this.messageType = messageType;
        this.spawningClient = spawningClient;
        numBytes = message.Length;
        this.message = message;
    }
}
