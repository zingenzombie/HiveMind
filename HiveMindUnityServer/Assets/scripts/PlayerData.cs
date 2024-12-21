using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

public class PlayerData : MonoBehaviour
{
    string playerID;
    TileStream tileStream;

    public string username;

    PlayerManager playerManager;
    public BlockingCollection<NetworkMessage> serverPipeOut;
    Thread tcpThreadIn, tcpThreadOut;

    bool initialized = false;

    void OnDestroy()
    {
        tileStream.SendStringToStream("Goodbye");

        if (tcpThreadIn != null && tcpThreadIn.IsAlive)
            tcpThreadIn.Abort();

        if (tcpThreadOut != null && tcpThreadOut.IsAlive)
            tcpThreadOut.Abort();

        tileStream.Dispose();

        serverPipeOut.Dispose();
    }

    public void InitializePlayerData(TileStream client, string playerID)
    {
        if (initialized)
            return;

        initialized = true;

        tileStream = client;
        this.playerID = playerID;
        username = playerID;
        playerManager = GameObject.FindWithTag("PlayerManager").GetComponent<PlayerManager>();

        serverPipeOut = new BlockingCollection<NetworkMessage>(); //Will need to be separated into udp and tcp pipes!

        tcpThreadIn = new Thread(() => TCPThreadIn());
        tcpThreadOut = new Thread(() => TCPThreadOut());
        tcpThreadIn.Start();
        tcpThreadOut.Start();
    }

    void TCPThreadIn()
    {
        while (true)
        {
            while (tileStream.Available > 0)
            {

                string messageType = tileStream.GetStringFromStream();
                byte[] message = tileStream.GetBytesFromStream();

                NetworkMessage netMessage = new NetworkMessage(playerID, messageType, message);

                HandleMessage(netMessage);
            }

            Thread.Yield();
        }
    }

    void TCPThreadOut()
    {
        while (true)
        {
            if (!tileStream.Connected)
            {
                tileStream.Close();
                HandleMessage(new NetworkMessage(playerID, "Goodbye", new byte[0]));

                break;
            }

            //Send outgoing TCP messages
            while (serverPipeOut.TryTake(out NetworkMessage newObject))
            {
                tileStream.SendStringToStream(newObject.messageType);
                tileStream.SendBytesToStream(newObject.message);
            }

            Thread.Yield();
        }
    }

    private void HandleMessage(NetworkMessage message)
    {
        switch(message.messageType){

            //First check for local request
            //case "PlayerPos":
            //    UpdatePlayerPosAndRot(message);
            //    break;

            //If can't be handled locally, send to MessageManager
            default:
                playerManager.messagePipe.Add(message);
                break;
        }

    }
}
