using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{

    [SerializeField] GameObject playerPrefab;
    [SerializeField] Transform PlayersTransform;

    Dictionary<string, GameObject> players = new Dictionary<string, GameObject>();

    BlockingCollection<PlayerInfo> newPlayerPipe = new BlockingCollection<PlayerInfo>();
    public BlockingCollection<NetworkMessage> messagePipe = new BlockingCollection<NetworkMessage>();

    Dictionary<string, NetworkMessage> positionUpdates = new Dictionary<string, NetworkMessage>();
    Queue<NetworkMessage> generalUpdates = new Queue<NetworkMessage>();

    struct PlayerInfo
    {
        public TileStream tileStream;
        public string playerID;

        public PlayerInfo(TileStream tileStream, string playerID)
        {
            this.tileStream = tileStream;
            this.playerID = playerID;
        }
    }

    private void Update()
    {
        InstantiateNewPlayers();
        HandleNewMessages();
        SendMessagesToAllExceptPID();
    }

    void HandleNewMessages()
    {
        while (messagePipe.TryTake(out NetworkMessage networkMessage))
        {
            switch (networkMessage.messageType)
            {
                case "PlayerPos":
                    //Testing:
                    Debug.Log("Player moved!!");

                    UpdatePlayerPos(networkMessage);
                    break;
                case "Goodbye":
                    Debug.Log("Player " + networkMessage.spawningClient + " left the server.");
                    DisconnectPlayer(networkMessage);
                    break;
                default:
                    break;
            }
        }
    }

    void DisconnectPlayer(NetworkMessage networkMessage)
    {
        //Serverside deletion
        Destroy(players[networkMessage.spawningClient]);
        players.Remove(networkMessage.spawningClient);

        //Send some message to clients that one client has left.
        generalUpdates.Enqueue(networkMessage);

        foreach(var iter in players)
        {
            Debug.Log("Existing player: " + iter.Key);
        }
    }

    void UpdatePlayerPos(NetworkMessage networkMessage)
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

        players[networkMessage.spawningClient].transform.SetPositionAndRotation(new Vector3(posX, posY, posZ), new Quaternion(rotX, rotY, rotZ, rotW));

        //SendMessageToAllExceptPID(networkMessage);

        //Replacing sender with dictionary so that each player can only have one position update.
        //SendMessateToAllExceptPID moved to be called on every update.
        positionUpdates[networkMessage.spawningClient] = networkMessage;
    }

    //This is effectively broadcast
    void SendMessagesToAllExceptPID()
    {
        //Send each position update to each other client
        foreach (var iter in positionUpdates)
            foreach (var player in players)
            {
                if (player.Key == iter.Key)
                    continue;

                player.Value.GetComponent<PlayerData>().serverPipeOut.Add(iter.Value);
            }

        //Empty positionUpdates
        positionUpdates.Clear();

        //Send each additional update to each other client
        while(generalUpdates.TryDequeue(out NetworkMessage iter))
        {
            Debug.Log(iter.messageType);
            foreach (var player in players)
            {
                if (player.Key == iter.spawningClient)
                    continue;

                player.Value.GetComponent<PlayerData>().serverPipeOut.Add(iter);
            }
        }
    }

    void InstantiateNewPlayers()
    {
        //Must be updated to support the updating of players who were already known from other servers

        //NONE OF THESE OPERATIONS MUST BE BLOCKING BASED IN PLAYER INPUT!!!
        while (newPlayerPipe.TryTake(out PlayerInfo playerInfo))
        {

            GameObject newPlayer = Instantiate(playerPrefab, PlayersTransform);

            //Send existing client info to new player
            foreach (var player in players)
                newPlayer.GetComponent<PlayerData>().serverPipeOut.Add(
                    new NetworkMessage(player.Key, "newPlayer", new byte[1]));

            players.Add(playerInfo.playerID, newPlayer);
            newPlayer.GetComponent<PlayerData>().InitializePlayerData(playerInfo.tileStream, playerInfo.playerID);

            Debug.Log("Player " + playerInfo.playerID + " joined the server.");
            //Send new player info to other players.

            /* Player info needed to send:
             * PID (Required)
             * Username?
             */

            generalUpdates.Enqueue(new NetworkMessage(playerInfo.playerID, "newPlayer", ASCIIEncoding.UTF8.GetBytes("USERNAMETMP")));
        }
    }

    public void AddPlayer(TileStream tileStream)
    {
        CoroutineResult<string> playerID = new CoroutineResult<string>();

        //Authenticate client
        if (!VerifyPlayer(playerID, tileStream))
        {
            //Failed authentication
            return;
        }

        //Generate new player object and store to list
        tileStream.SendStringToStream("ACK");
        newPlayerPipe.Add(new PlayerInfo(tileStream, playerID.Value));
    }

    bool VerifyPlayer(CoroutineResult<string> playerID, TileStream tileStream)
    {
        //Generate challenge
        RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();

        byte[] challengeKey = new byte[100];
        rng.GetBytes(challengeKey);

        //Encrypt challenge with playerKey and send
        RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();

        playerID.Value = tileStream.GetStringFromStream();
        rsa.FromXmlString(playerID.Value);

        tileStream.SendBytesToStream(rsa.Encrypt(challengeKey, false));

        byte[] response = tileStream.GetBytesFromStream();

        for (int i = 0; i < challengeKey.Length; i++)
            if (challengeKey[i] != response[i])
                return false;

        return true;
    }
}
