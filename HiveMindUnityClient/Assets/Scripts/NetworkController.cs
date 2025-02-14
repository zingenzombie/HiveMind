using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class NetworkController : MonoBehaviour
{
    HexTileController activeServer;
    [SerializeField] PlayerInfo player;
    [SerializeField] GameObject otherPlayerPrefab;
    Transform playerHolder;

    Thread tileStreamSender, tileStreamReceiver, connectThread;
    BlockingCollection<NetworkMessage> messagePipeOut = new BlockingCollection<NetworkMessage>();
    BlockingCollection<NetworkMessage> messagePipeIn = new BlockingCollection<NetworkMessage>();

    Dictionary<string, GameObject> players = new Dictionary<string, GameObject>();
    private void Update()
    {
        HandleNewMessages();
    }

    private void OnDestroy()
    {
        if (tileStreamSender != null && tileStreamSender.IsAlive)
            tileStreamSender.Abort();

        if (tileStreamReceiver != null && tileStreamReceiver.IsAlive)
            tileStreamReceiver.Abort();

        if (connectThread != null && connectThread.IsAlive)
            connectThread.Abort();
    }

    void HandleNewMessages()
    {
        while (messagePipeIn.TryTake(out NetworkMessage networkMessage))
        {
            switch (networkMessage.messageType)
            {
                case "newPlayer":
                    Debug.Log("Received new player " + networkMessage.spawningClient + "!");
                    CreateNewPlayer(networkMessage);
                    break;
                case "Goodbye":
                    Debug.Log("Player " + networkMessage.spawningClient + "left the server.");
                    DeleteOtherPlayer(networkMessage);
                    //Handle removal of player
                    break;
                case "PlayerPos":
                    UpdatePlayerPosition(networkMessage);
                    break;
                default:
                    break;
            }
        }
    }

    void CreateNewPlayer(NetworkMessage networkMessage)
    {
        GameObject newPlayer = Instantiate(otherPlayerPrefab, playerHolder);
        players[networkMessage.spawningClient] = newPlayer;
    }

    void DeleteOtherPlayer(NetworkMessage networkMessage)
    {

        Destroy(players[networkMessage.spawningClient]);
        players.Remove(networkMessage.spawningClient);
    }

    void UpdatePlayerPosition(NetworkMessage networkMessage)
    {

        byte[] transformInfo = networkMessage.message;
        //***CHECK THAT MESSAGE TIME IS NEWER THAN CURRENT UPDATE***

        float posX = BitConverter.ToSingle(transformInfo, 0);
        float posY = BitConverter.ToSingle(transformInfo, 4);
        float posZ = BitConverter.ToSingle(transformInfo, 8);

        float rotX = BitConverter.ToSingle(transformInfo, 12);
        float rotY = BitConverter.ToSingle(transformInfo, 16);
        float rotZ = BitConverter.ToSingle(transformInfo, 20);
        float rotW = BitConverter.ToSingle(transformInfo, 24);
        try
        {
            players[networkMessage.spawningClient].transform.SetPositionAndRotation(new Vector3(posX, posY, posZ), new Quaternion(rotX, rotY, rotZ, rotW));
        }catch (Exception) { Debug.Log("Player " + networkMessage.spawningClient + " does not exist locally but received position update!"); }
    }

    /* When changing tiles, I want to stay connected to the prior tile until a new 
     * connection is established or fails to establish. This will (hopefully) 
     * prevent huccups in things like audio and player movement.
     */

    public IEnumerator ChangeActiveServer(HexTileController activeServer)
    {
        this.activeServer = activeServer;

        playerHolder = activeServer.transform.GetChild(1);

        foreach(var player in players)
            Destroy(player.Value);

        players.Clear();

        CoroutineResult<TileStream> streamReturn = new CoroutineResult<TileStream>();
        connectThread = new Thread(() => connectToNewServer(streamReturn));
        connectThread.Start();

        while (connectThread.ThreadState == ThreadState.Running)
            yield return null;

        //How can I make sure this message gets sent before the thread handling it is aborted?

        if (tileStreamSender != null && tileStreamSender.ThreadState == ThreadState.Running)
        {
            Debug.Log("Leaving");
            messagePipeOut.Add(new NetworkMessage("Goodbye", new byte[1]));

            while (connectThread.ThreadState == ThreadState.Running)
                yield return null;
        }

        if (streamReturn.Value == null)
        {
            Debug.Log("An exception occurred while attempting to join a new tile.");
            yield break;
        }

        if (tileStreamReceiver != null && tileStreamReceiver.ThreadState == ThreadState.Running)
        {
            tileStreamReceiver.Abort();
            while (tileStreamReceiver.ThreadState == ThreadState.Running)
                yield return null;
        }

        tileStreamReceiver = new Thread(() => TCPThreadIn(streamReturn.Value));
        tileStreamReceiver.Start();

        if(tileStreamSender != null)
            while (tileStreamSender.ThreadState == ThreadState.Running)
                yield return null;

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
                    string spawningClient = tileStream.GetStringFromStream();
                    string messageType = tileStream.GetStringFromStream();
                    byte[] message = tileStream.GetBytesFromStream();

                    NetworkMessage netMessage = new NetworkMessage(spawningClient, messageType, message);

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
