using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;

public class PlayerData : MonoBehaviour
{
    string playerID;
    TileStream tileStream;

    public string avatarHash;

    public string username;

    PlayerManager playerManager;
    public BlockingCollection<NetworkMessage> serverPipeOut = new BlockingCollection<NetworkMessage>();
    public BlockingCollection<bool> messageUpdateTime = new BlockingCollection<bool>();
    Thread tcpThreadIn, tcpThreadOut;

    bool initialized = false;

    public BlockingCollection<NetworkMessage> messagePipe = new BlockingCollection<NetworkMessage>();
    public BlockingCollection<TileStream> ObjectInTileStream = new BlockingCollection<TileStream>();

    float timeOfLastResponse;

    ObjectManager objectManager;

    private void Start()
    {
        objectManager = GameObject.FindWithTag("ObjectController").GetComponent<ObjectManager>();
        timeOfLastResponse = Time.time;
    }

    bool armed = false;
    void FixedUpdate()
    {
        if (messageUpdateTime.TryTake(out _))
            timeOfLastResponse = Time.fixedTime;

        if (timeOfLastResponse + 3 < Time.fixedTime)
        {
            if (!armed)
            {
                armed = true;
                serverPipeOut.Add(new NetworkMessage("", "ping", new byte[0]));
            }

        }
        else
            armed = false;

        if (timeOfLastResponse + 8 < Time.time)
        {
            Debug.Log("LOCAL GOODBYE TRIGGERED");
            messagePipe.Add(new NetworkMessage(playerID, "Goodbye", new byte[0]));
        }
    }

    void Update()
    {
        HandleMessages();
    }

    void OnDestroy()
    {
        Debug.Log("Destroy triggered!");
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

        //serverPipeOut = new BlockingCollection<NetworkMessage>(); //Will need to be separated into udp and tcp pipes!

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

                messagePipe.Add(new NetworkMessage(playerID, messageType, message));
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
                Debug.Log("LOCAL GOODBYE 2 TRIGGERED");
                messagePipe.Add(new NetworkMessage(playerID, "Goodbye", new byte[0]));

                break;
            }

            //Send outgoing TCP messages
            while (serverPipeOut.TryTake(out NetworkMessage newObject))
            {
                //This line is a new addition that will definitely break everything.
                tileStream.SendStringToStream(newObject.spawningClient);
                tileStream.SendStringToStream(newObject.messageType);
                tileStream.SendBytesToStream(newObject.message);
            }

            Thread.Yield();
        }
    }

    void HandleMessages()
    {

        while (messagePipe.TryTake(out NetworkMessage networkMessage))
        {

            messageUpdateTime.Add(true);

            switch (networkMessage.messageType)
            {

                case "ChangeAvatar":
                    StartCoroutine(ChangeAvatarFromClient(networkMessage));
                    break;

                case "SpawnObject":
                    StartCoroutine(CreateDynamicObjectFromClient(networkMessage));
                    break;

                //First check for local request
                //case "PlayerPos":
                //    UpdatePlayerPosAndRot(message);
                //    break;

                //If can't be handled locally, send to MessageManager
                default:
                    playerManager.HandleNewMessages(networkMessage);
                    break;
            }

        }

        IEnumerator CreateDynamicObjectFromClient(NetworkMessage networkMessage)
        {
            string hash = ASCIIEncoding.ASCII.GetString(networkMessage.message);

            Debug.Log("Creating Object " + hash + "...");

            if (objectManager.HashExists(hash))
            {
                Debug.Log("Local hash found!");
                yield return StartCoroutine(objectManager.SpawnObjectAsServer(hash, transform.GetChild(0)));


                avatarHash = hash;
                playerManager.HandleNewMessages(networkMessage);

                yield break;
            }

            Debug.Log("Local hash not found; prompting client for new stream...");
            serverPipeOut.Add(new NetworkMessage("", "OpenConnection", new byte[0]));

            TileStream tmpTileStream;

            while (!ObjectInTileStream.TryTake(out tmpTileStream))
                yield return null;

            Debug.Log("Received stream for avatar data transfer.");

            //Compose new one
            yield return StartCoroutine(objectManager.SpawnObjectAsServer(hash, null, tmpTileStream));

            playerManager.HandleNewMessages(networkMessage);

        }

        IEnumerator ChangeAvatarFromClient(NetworkMessage networkMessage)
        {

            Debug.Log("Updating avatar...");

            string hash = ASCIIEncoding.ASCII.GetString(networkMessage.message);
            Debug.Log("Changing avatar to " + hash + "...");

            //Destroy old avatar
            Destroy(transform.GetChild(0).GetChild(0).gameObject);

            if (objectManager.HashExists(hash))
            {
                Debug.Log("Local hash found!");
                yield return StartCoroutine(objectManager.SpawnObjectAsServer(hash, transform.GetChild(0)));


                avatarHash = hash;
                playerManager.HandleNewMessages(networkMessage);

                yield break;
            }

            Debug.Log("Local hash not found; prompting client for new stream...");
            serverPipeOut.Add(new NetworkMessage("", "OpenConnection", new byte[0]));

            TileStream tmpTileStream;

            while(!ObjectInTileStream.TryTake(out tmpTileStream))
                yield return null;

            Debug.Log("Received stream for avatar data transfer.");

            //Compose new one
            yield return StartCoroutine(objectManager.SpawnObjectAsServer(hash, transform.GetChild(0), tmpTileStream));

            //Change avatar hash locally and notify other members AFTER composition has finished to ensure that all files are available.
            avatarHash = hash;
            playerManager.HandleNewMessages(networkMessage);

        }
    }
}