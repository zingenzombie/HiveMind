using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{

    [SerializeField] GameObject playerPrefab;
    [SerializeField] Transform PlayersTransform;

    Dictionary<string, GameObject> players = new Dictionary<string, GameObject>();

    BlockingCollection<PlayerInfo> newPlayerPipe = new BlockingCollection<PlayerInfo>();
    public BlockingCollection<NetworkMessage> messagePipe = new BlockingCollection<NetworkMessage>();

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
        HandleNewMessages();
        InstantiateNewPlayers();
    }

    void HandleNewMessages()
    {
        while (messagePipe.TryTake(out NetworkMessage networkMessage))
        {
            switch (networkMessage.messageType)
            {
                case "PlayerPos":
                    UpdatePlayerPos(networkMessage);
                    break;

                default:
                    break;
            }
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

        SendMessageToAllExceptPID(networkMessage);
    }

    //This should probably be changed to send multiple player updates at once
    void SendMessageToAllExceptPID(NetworkMessage networkMessage)
    {
        foreach(var player in players)
        {
            if (player.Key == networkMessage.spawningClient)
                continue;

            player.Value.GetComponent<PlayerData>().serverPipeOut.Add(networkMessage);
        }
    }

    void InstantiateNewPlayers()
    {
        //Must be updated to support the updating of players who were already known from other servers

        //NONE OF THESE OPERATIONS MUST BE BLOCKING BASED IN PLAYER INPUT!!!
        while (newPlayerPipe.TryTake(out PlayerInfo playerInfo))
        {

            GameObject newPlayer = Instantiate(playerPrefab, PlayersTransform);
            players.Add(playerInfo.playerID, newPlayer);
            newPlayer.GetComponent<PlayerData>().InitializePlayerData(playerInfo.tileStream, playerInfo.playerID);
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
