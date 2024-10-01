using System.Collections;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System;
using System.Threading;
using System.IO;
using System.Collections.Concurrent;
using System.Net.Security;
using System.Security.Cryptography;
using System.Collections.Generic;

public class ServerController : MonoBehaviour
{
    private Thread listen;
    private IPAddress coreAddress;
    public int corePort;
    public GameObject hexTile;
    private GameObject groundHolder, tileObjects;
    public string serverIPString, coreIPString;

    public bool requestXY;
    public IPAddress ipAddress;
    public int port;
    public int x, y;
    public string serverName;
    public string ownerID;

    [SerializeField] GameObject playerPrefab;
    [SerializeField] GameObject Players;
    [SerializeField] GameObject DynamicObjects;
    [SerializeField] GameObject hexTileTemplate;
    private float tileSize;
    public bool initialized = false;

    private RSACryptoServiceProvider localRSA;

    BlockingCollection<TileStream> playerPipe = new BlockingCollection<TileStream>();

    Thread coreListener;
    BlockingCollection<string> CoreMessages = new BlockingCollection<string>();

    public Dictionary<string, GameObject> players = new Dictionary<string, GameObject>();

    SslStream coreConnection;

    private void OnApplicationQuit()
    {
        if(listen != null)
            listen.Abort();
        if(coreListener != null)
            coreListener.Abort();
    }

    // Start is called before the first frame update
    private void Start()
    {
        localRSA = new RSACryptoServiceProvider();

        ipAddress = IPAddress.Parse(serverIPString);

        //Delete this later and fix
        if(!IPAddress.TryParse(coreIPString, out coreAddress))
            coreAddress = Dns.Resolve(coreIPString).AddressList[0];

        //groundHolder = hexTile.transform.GetChild(0).gameObject;
        groundHolder = hexTile.transform.GetChild(1).gameObject;

        InitializeWithCore();

        tileSize = hexTileTemplate.GetComponent<Renderer>().bounds.size.z;

        float offsetX = x * tileSize * Mathf.Cos(Mathf.Deg2Rad * 30);
        float offsetY = x % 2 == 0 ? 0 : tileSize / 2;

        offsetY += y * tileSize;

        hexTile.transform.SetLocalPositionAndRotation(new UnityEngine.Vector3(offsetX, 250 * Mathf.PerlinNoise(offsetX / 5000, offsetY / 5000), offsetY), transform.rotation);

        StartCoroutine(InstantiateNewClients());

        Thread checkIn = new Thread(() => CheckIn());
        Thread checkWithCore = new Thread(() => CheckWithCore());

        StartCoroutine(ReadCoreMessages());

        ClientConnectListener();
    }

    void CheckWithCore()
    {
        while (true)
        {
            Thread.Sleep(5000);
            string coreMes = CoreCommunication.GetStringFromStream(coreConnection);
            CoreMessages.Add(coreMes);
        }
    }

    IEnumerator ReadCoreMessages()
    {
        while (true)
        {
            if (CoreMessages.TryTake(out string mes))
                Debug.Log(mes);


            yield return new WaitForSeconds(100);
        }
    }

    void CheckIn()
    {
        foreach(GameObject player in players.Values)
        {
            PlayerData.IsConnected(player.GetComponent<PlayerData>().tileStream);
        }

        Thread.Sleep(5000);
    }

    public void ShoutMessage(string type, string message)
    {
        Debug.Log($"Shouting: {message}");
        foreach (GameObject playerData in players.Values)
        {
            var nwm = new NetworkMessage(type, Encoding.ASCII.GetBytes(message));
            playerData.GetComponent<PlayerData>().serverPipeOut.Add(nwm);
        }
    }

    public void KillPlayer(string playerID)
    {
        //This probably won't work for reasons

        Destroy(players[playerID]);

        players.Remove(playerID);

        //Also announce player destruction to other players.
    }

    public void KillServer()
    {

    }

    IEnumerator InstantiateNewClients()
    {
        while (true)
        {
            if(playerPipe.TryTake(out TileStream client))
            {
                GameObject newPlayer = Instantiate(playerPrefab, Players.transform);

                var newComp = newPlayer.GetComponent<PlayerData>();
                newComp.InitializePlayerData(client);

                ShoutMessage("newPlayer", $"{newComp.playerID}");

                players.Add(newComp.playerID, newPlayer);
            }
            yield return null;
        }
    }

    //Needs to be replaced by one using PID instead!

    private void ClientConnectListener()
    {
        listen = new Thread(() => ListenForClientsTCP());
        listen.Start();
    }

    void InitializeWithCore()
    {
        ServerData tmp = new ServerData(requestXY, x, y, serverName, ipAddress.ToString(), port, ownerID, localRSA.ToXmlString(false));
        string jsonString = JsonUtility.ToJson(tmp);
        byte[] bytes = Encoding.ASCII.GetBytes(jsonString);

        using TcpClient client = new TcpClient(coreAddress.ToString(), corePort);

        SslStream sslStream = CoreCommunication.EstablishSslStreamFromTcpAsClient(client);
        coreConnection = sslStream;

        //telling the core what kind of connection this is
        CoreCommunication.SendStringToStream(sslStream, "server");
        
        CoreCommunication.SendStringToStream(sslStream, "newServer");

        byte[] bytesFinal = new byte[bytes.Length + 1];

        int i = 0;
        foreach(byte b in bytes)
            bytesFinal[i++] = b;

        bytesFinal[i] = 0x00;

        sslStream.Write(bytesFinal);
        Debug.Log($"Sent over server info (of size: {bytesFinal.Length} bytes)");

        var tileReqResult = CoreCommunication.GetStringFromStream(sslStream);
        Debug.Log(/*tileReqAck + */tileReqResult);

        if(tileReqResult.Contains("GRANTED"))
            initialized = true;
    }

    private void ListenForClientsTCP()
    {
        TcpListener server = new TcpListener(IPAddress.Any, port);
        // we set our IP address as server's address, and we also set the port

        server.Start();  // this will start the server

        while (true)   //we wait for a connection
        {
            TileStream client;
            if (server.Pending())
            {
                client = new TileStream(server.AcceptTcpClient());
                client.ActivateStream(localRSA);

                Thread thread = new Thread(() => HandleNewClient(client));
                thread.Start();
            }
        }
    }

    /*
    public void SendPlayerPositionToOthers(string ipStr, Transform playerTransform)
    {
        byte[] ip = IPAddress.Parse(ipStr).GetAddressBytes();

        byte[] posX = BitConverter.GetBytes(playerTransform.position.x);
        byte[] posY = BitConverter.GetBytes(playerTransform.position.y);
        byte[] posZ = BitConverter.GetBytes(playerTransform.position.z);

        byte[] rotX = BitConverter.GetBytes(playerTransform.rotation.x);
        byte[] rotY = BitConverter.GetBytes(playerTransform.rotation.y);
        byte[] rotZ = BitConverter.GetBytes(playerTransform.rotation.z);
        byte[] rotW = BitConverter.GetBytes(playerTransform.rotation.w);

        byte[] message = new byte[28 + ip.Length];

        for (int i = 0; i < 28; i++)
        {
            if (i < 4)
                message[i] = posX[i];
            else if (i < 8)
                message[i] = posY[i - 4];
            else if (i < 12)
                message[i] = posZ[i - 8];
            else if (i < 16)
                message[i] = rotX[i - 12];
            else if (i < 20)
                message[i] = rotY[i - 16];
            else if (i < 24)
                message[i] = rotZ[i - 20];
            else
                message[i] = rotW[i - 24];
        }

        for(int i = 0; i < ip.Length; i++)
            message[28 + i] = ip[i];

        foreach(GameObject player in players)
        {
            if (player.GetComponent<PlayerData>().ip == ipStr)
                continue;

            player.GetComponent<PlayerData>().serverPipeOut.Add(new NetworkMessage("playerPos", message));
        }
    }*/

    private void HandleNewClient(TileStream client)
    {

        switch (client.GetStringFromStream())
        {
            case "getAssets":
                GetAssets(client);
                break;
            case "joinServer":
                JoinServer(client);
                break;

            default: break;
        }
    }

    private void JoinServer(TileStream client)
    {
        playerPipe.Add(client);
    }

    private void GetAssets(TileStream client)
    {

        string assetBundleDirectoryPath = Application.dataPath + "/AssetBundles";

        string typeOS = client.GetStringFromStream();

        //string typeOS = CoreCommunication.GetStringFromStream(client);

        if (!(typeOS.Equals("w") || typeOS.Equals("m") || typeOS.Equals("l")))
        {
            Debug.Log("Client sent invalid OS type.");
            return;
        }

        Debug.Log(typeOS);

        string[] fileNames = Directory.GetFiles(assetBundleDirectoryPath + "/" + typeOS);

        //client.Write(Encoding.ASCII.GetBytes(fileNames.Length.ToString() + (char) 0x00));

        client.SendBytesToStream(BitConverter.GetBytes(fileNames.Length));

        foreach(string fileName in fileNames)
        {

            client.SendStringToStream(new System.IO.FileInfo(fileName).Name);
            //client.SendBytesToStream(BitConverter.GetBytes(new System.IO.FileInfo(fileName).Length));

            //client.Write(Encoding.ASCII.GetBytes(new System.IO.FileInfo(fileName).Name + (char) 0x00));
            //client.Write(Encoding.ASCII.GetBytes(((int) new System.IO.FileInfo(fileName).Length).ToString() + (char) 0x00));

            //SendFile() had issues with files above ~16 KB in size on macOS, so this solution was implemented instead.
            /*byte[] buffer = new byte[8192]; // 8 KB buffer size
            int bytesRead;

            using (FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                    client.Write(buffer, 0, bytesRead);*/

            byte[] buffer = new byte[new System.IO.FileInfo(fileName).Length];

            using (FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                fileStream.Read(buffer);

            client.SendBytesToStream(buffer);
        }
    }
}
