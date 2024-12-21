using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class NetworkController : MonoBehaviour
{
    HexTileController activeServer;
    [SerializeField] PlayerInfo player;

    Thread tileStreamSender, tileStreamReceiver;
    BlockingCollection<NetworkMessage> messagePipeOut, messagePipeIn;

    private void Start()
    {
        messagePipeOut = new BlockingCollection<NetworkMessage>();
        messagePipeIn = new BlockingCollection<NetworkMessage>();
    }

    private void Update()
    {
        HandleNewMessages();
    }

    void HandleNewMessages()
    {
        while (messagePipeIn.TryTake(out NetworkMessage networkMessage))
        {
            switch (networkMessage.messageType)
            {
                default:
                    break;
            }
        }
    }

    /* When changing tiles, I want to stay connected to the prior tile until a new 
     * connection is established or fails to establish. This will (hopefully) 
     * prevent huccups in things like audio and player movement.
     */
    public IEnumerator ChangeActiveServer(HexTileController activeServer)
    {
        this.activeServer = activeServer;

        CoroutineResult<TileStream> streamReturn = new CoroutineResult<TileStream>();
        Thread connectThread = new Thread(() => connectToNewServer(streamReturn));
        connectThread.Start();

        while (connectThread.IsAlive)
            yield return null;

        //How can I make sure this message gets sent before the thread handling it is aborted?

        if(tileStreamSender != null && tileStreamSender.IsAlive)
            messagePipeOut.Add(new NetworkMessage("Goodbye", new byte[0]));

        if (streamReturn.Value == null)
        {
            Debug.Log("An exception occurred while attempting to join a new tile.");
            yield break;
        }

        tileStreamReceiver = new Thread(() => TCPThreadIn(streamReturn.Value));
        tileStreamReceiver.Start();

        tileStreamSender = new Thread(() => TCPThreadOut(streamReturn.Value));
        tileStreamSender.Start();
    }

    void TCPThreadIn(TileStream tileStream)
    {
        try
        {
            while (true)
            {
                while (tileStream.Available > 0)
                {

                    string messageType = tileStream.GetStringFromStream();

                    if(messageType == "Goodbye")
                    {
                        tileStream.Close();
                        return;
                    }

                    byte[] message = tileStream.GetBytesFromStream();

                    NetworkMessage netMessage = new NetworkMessage(player.playerID, messageType, message);

                    messagePipeIn.Add(netMessage);
                }

                Thread.Yield();
            }
        }catch(Exception e)
        {
            Debug.Log(e);
        }
        
    }

    void TCPThreadOut(TileStream tileStream)
    {
        try
        {
            while (true)
            {
                if (!tileStream.Connected)
                {
                    tileStream.Close();
                    //GRCEFULLY end connection ***TODO***

                    break;
                }

                //Send outgoing TCP messages
                while (messagePipeOut.TryTake(out NetworkMessage newObject))
                {
                    tileStream.SendStringToStream(newObject.messageType);
                    tileStream.SendBytesToStream(newObject.message);

                    if(newObject.messageType == "Goodbye")
                        return;
                }

                Thread.Yield();
            }
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }

    void connectToNewServer(CoroutineResult<TileStream> streamReturn)
    {

        ServerData serverData = activeServer.serverData;
        TileStream newTileStream;

        try
        {
            newTileStream = new TileStream(new TcpClient(serverData.Ip, serverData.Port));
            newTileStream.ActivateStream(serverData.PublicKey);
        }
        catch (Exception e)
        {
            Debug.Log(e);
            streamReturn.Value = null;
            return;
        }

        newTileStream.SendStringToStream("joinServer");

        //Send playerID and verify player
        newTileStream.SendStringToStream(player.GetPlayerPublicRSA());
        newTileStream.SendBytesToStream(player.VerifyPlayer(newTileStream.GetBytesFromStream()));

        string acknowledge = newTileStream.GetStringFromStream();
        Debug.Log(acknowledge);

        if (!acknowledge.Equals("ACK"))
        {
            Debug.Log("Failed to receive ACK from tile server!");
            streamReturn.Value = null;
            return;
        }

        streamReturn.Value = newTileStream;
    }

    public void SendTCPMessage(NetworkMessage message)
    {
        if (activeServer == null)
            return;

        try
        {
            messagePipeOut.Add(message);
        }
        catch (System.Exception) { }
    }

    public void SendUDPMessage(NetworkMessage message)
    {
        if (activeServer == null)
            return;

        try
        {
            //activeServer.serverUDPPipe.Add(message);
        }
        catch (System.Exception) { }
    }
}
