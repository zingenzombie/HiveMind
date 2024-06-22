using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkMessage
{
    public string messageType;
    public int numBytes;
    public byte[] message;

    public NetworkMessage(string messageType, byte[] message)
    {
        this.messageType = messageType;
        numBytes = message.Length;
        this.message = message;
    }
}
