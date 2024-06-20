using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkMessage
{
    public int numBytes;
    public string messageType;
    public byte[] message;

    NetworkMessage(string messageType, byte[] message)
    {
        this.messageType = messageType;
        numBytes = message.Length;
        this.message = message;
    }
}
