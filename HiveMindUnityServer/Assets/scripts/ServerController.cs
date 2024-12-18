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

    public Dictionary<string, GameObject> players = new Dictionary<string, GameObject>();

    private void OnApplicationQuit()
    {
        if(listen != null)
            listen.Abort();
    }

    // Start is called before the first frame update
    private void Start()
    {

        GameObject testObject = GameObject.FindWithTag("TESTTARGET");

        ObjectDecomposer decomposer = ScriptableObject.CreateInstance<ObjectDecomposer>();

        string objectHash = decomposer.Decompose(testObject);

        //ObjectComposer composer = new ObjectComposer();

        gameObject.AddComponent<ObjectComposer>();

        StartCoroutine(this.GetComponent<ObjectComposer>().Compose(objectHash, DynamicObjects.transform));



        localRSA = new RSACryptoServiceProvider();

        ipAddress = IPAddress.Parse(serverIPString);

        if (!IPAddress.TryParse(coreIPString, out coreAddress))
        {
            try
            {
                coreAddress = Dns.GetHostAddresses(coreIPString)[0];
            }catch(Exception)
            {
                throw new Exception("No address was returned in the DNS lookup for the core server.");
            }

        }

        //groundHolder = hexTile.transform.GetChild(0).gameObject;
        groundHolder = hexTile.transform.GetChild(1).gameObject;

        InitializeWithCore();

        tileSize = hexTileTemplate.GetComponent<Renderer>().bounds.size.z;

        float offsetX = x * tileSize * Mathf.Cos(Mathf.Deg2Rad * 30);
        float offsetY = x % 2 == 0 ? 0 : tileSize / 2;

        offsetY += y * tileSize;

        hexTile.transform.SetLocalPositionAndRotation(new UnityEngine.Vector3(offsetX, 250 * Mathf.PerlinNoise(offsetX / 5000, offsetY / 5000), offsetY), transform.rotation);

        StartCoroutine(InstantiateNewClients());

        ClientConnectListener();
    }

    public void ShoutMessage(NetworkMessage message, bool sendToNeighbors = true)
    {
        Debug.Log($"Shouting: {message.messageType}");
        foreach (GameObject playerData in players.Values)
            playerData.GetComponent<PlayerData>().serverPipeOut.Add(message);

        if (!sendToNeighbors)
            return;

        //Implement neighbor communication and replace below foreach with list of neighbors.

        /*
        foreach (GameObject playerData in players.Values)
            playerData.GetComponent<PlayerData>().serverPipeOut.Add(message);*/
    }

    //I love the idea, but i think we should depreciate this and replace it with ShoutMessage(NetworkMessage message).
    //This will allow us to send any sort of NetworkMessage, and it will make it easier to forward messages.
    //We could also just leave both.
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

        sslStream.Close();
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
            case "updateAvatar":
                UpdateAvatar(client);
                break;

            default: break;
        }
    }

    private void GetAssets(TileStream client)
    {

        string assetBundleDirectoryPath = Application.dataPath + "/AssetBundles";

        string typeOS = client.GetStringFromStream();

        if (!(typeOS.Equals("w") || typeOS.Equals("m") || typeOS.Equals("l")))
        {
            Debug.Log("Client sent invalid OS type.");
            return;
        }

        Debug.Log(typeOS);

        string[] fileNames = Directory.GetFiles(assetBundleDirectoryPath + "/" + typeOS);

        client.SendBytesToStream(BitConverter.GetBytes(fileNames.Length));

        foreach (string fileName in fileNames)
        {

            client.SendStringToStream(new System.IO.FileInfo(fileName).Name);

            byte[] buffer = new byte[new System.IO.FileInfo(fileName).Length];

            using (FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                fileStream.Read(buffer);

            client.SendBytesToStream(buffer);
        }
    }

    private void JoinServer(TileStream client)
    {
        playerPipe.Add(client);
    }

    void UpdateAvatar(TileStream client)
    {

        /*Steps:

        * Prove client is who they say they are and are a part of this server.

        * Download avatar hash. This can be used to verify that a local copy of the avatar
        doesn't exist (saving on storage + bandwidth).
        

        * Download avatar AssetBundle.

        * Set client avatar to downloaded avatar.
        */

        /*Avatar File Format:
        "ClientID" folder containing "lastOnline" (text document), "w", "m", and "l" folders (the actual
        platform-specific AssetBundles).
         */
    }
}
