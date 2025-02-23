using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
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

    [SerializeField] ObjectManager objectController;

    void Update()
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
        if (messagePipeIn.TryTake(out NetworkMessage networkMessage))
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
                case "OpenConnection":
                    Thread handleNewConnection = new Thread(() => OpenAChannel());
                    handleNewConnection.Start();
                    break;
                case "ChangeAvatar":
                    Debug.Log("Received new avatar for user " + networkMessage.spawningClient + ".");
                    ChangeAvatar(networkMessage);
                    break;
                case "ping":
                    messagePipeOut.Add(new NetworkMessage("", "pong", new byte[0]));
                    break;
                default:
                    break;
            }
        }
    }

    void ChangeAvatar(NetworkMessage networkMessage)
    {
        StartCoroutine(players[networkMessage.spawningClient].GetComponent<OtherClient>().UpdateAvatar(ASCIIEncoding.ASCII.GetString(networkMessage.message)));
    }

    void OpenAChannel()
    {

        TileStream tileStream = connectToNewServer("SendAssets");

        objectController.SendRequestedObjects(tileStream);
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
            players[networkMessage.spawningClient].GetComponent<OtherClient>().ChangeTarget(new Vector3(posX, posY, posZ), new Quaternion(rotX, rotY, rotZ, rotW));
        }catch (Exception) { Debug.Log("Player " + networkMessage.spawningClient + " does not exist locally but received position update!"); }
    }

    /* When changing tiles, I want to stay connected to the prior tile until a new 
     * connection is established or fails to establish. This will (hopefully) 
     * prevent huccups in things like audio and player movement.
     */

    public IEnumerator CreateFetchStream(CoroutineResult<TileStream> tileStream)
    {
        CoroutineResult<TileStream> streamReturn = new CoroutineResult<TileStream>();
        connectThread = new Thread(() => connectToNewServer("getAssets", false, tileStream));
        connectThread.Start();

        while (connectThread.ThreadState == ThreadState.Running)
            yield return null;
    }

    public IEnumerator ChangeActiveServer(HexTileController activeServer)
    {
        this.activeServer = activeServer;

        playerHolder = activeServer.transform.GetChild(1);

        foreach(var player in players)
            Destroy(player.Value);

        players.Clear();

        CoroutineResult<TileStream> streamReturn = new CoroutineResult<TileStream>();
        connectThread = new Thread(() => connectToNewServer("joinServer", true, streamReturn));
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

        messagePipeOut.Add(new NetworkMessage("", "ChangeAvatar", ASCIIEncoding.ASCII.GetBytes(player.avatarHash)));
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

    TileStream connectToNewServer(string connectionType = "joinServer", bool verify = true, CoroutineResult<TileStream> streamReturn = null)
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
            if(streamReturn != null)
                streamReturn.Value = null;
            return null;
        }

        newTileStream.SendStringToStream(connectionType);

        if(verify)
            if (!newTileStream.VerifySelf(player))
            {
                if (streamReturn != null)
                    streamReturn.Value = null;
                throw new Exception("Failed to verify self with server.");
            }

        if (streamReturn != null)
            streamReturn.Value = newTileStream;
        return newTileStream;
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
