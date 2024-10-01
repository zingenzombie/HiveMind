using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;
using static UnityEngine.InputSystem.InputRemoting;

public class HexTileController : MonoBehaviour
{
    int corePort;
    public int x, y;
    string coreAddress;
    public bool hasServer = false;
    public ServerData serverData;
    public PlayerInfo player;

    public GameObject groundHolder;
    public GameObject templateGroundHolder;
    public GameObject tileObjects;

    private TileStream tileStream;

    private Thread serverTCP = null, serverUDP = null;
    public BlockingCollection<NetworkMessage> serverPipeIn, serverPipeOut;

    public Hashtable players = new Hashtable();


    /* *** Welcome to the HexTileControler ***
     * 
     * This function is repsonsible for all actions on the client side
     * relating to a specific tile. A tile consists of a ground object,
     * tile objects, and players (this excludes the user's player). 
     * 
     * The ground is the hexagonal platform on which a server may place 
     * objects. It can be replaced by any object of the server's 
     * choosing, or no object at all. This may be removed later as it 
     * isn't strictly necessary, and I'm not completely sure why this
     * would need to be treated differently than any other tile object.
     * By default, all tiles have the default hexagon tile ground, which
     * has a width (the small width) of 64 meters.
     * 
     * Tile objects are any object that a server posesses which are not
     * the ground (or players). Buildings, items, scripts (I'm scared of
     * this one...), etc.
     */

    // Start is called before the first frame update
    void Awake()
    {
        coreAddress = GameObject.FindWithTag("Grid").GetComponent<GridController>().coreAddress.ToString();
        corePort = GameObject.FindWithTag("Grid").GetComponent<GridController>().corePort;

        StartCoroutine(HandleMessage());

        //ClearServer();
    }

    public void ContactTileServer()
    {
        if (!hasServer)
            return;

        serverTCP = new Thread(() => ServerTCP());
        serverTCP.Start();
    }

    public void Disconnect()
    {
        if(!hasServer) 
            return;

        //This is definitely broken

        try
        {
            serverTCP.Abort();
            serverTCP = null;
        }catch(Exception) { }
        
        try
        {
            serverUDP.Abort();
            serverUDP = null;
        }catch (Exception) { }

        tileStream.SendStringToStream("killMe");
        tileStream.SendBytesToStream(new byte[0]);

        tileStream.Close();

    }

    public void ServerTCP()
    {

        tileStream = new TileStream(new TcpClient(serverData.Ip, serverData.Port));
        tileStream.ActivateStream(serverData.PublicKey);

        tileStream.SendStringToStream("joinServer");

        //Send playerID and verify player
        tileStream.SendStringToStream(player.GetPlayerPublicRSA());
        tileStream.SendBytesToStream(player.VerifyPlayer(tileStream.GetBytesFromStream()));

        string acknowledge = tileStream.GetStringFromStream();
        Debug.Log(acknowledge);

        if (!acknowledge.Equals("ACK"))
        {
            Debug.Log("Failed to receive ACK from tile server!");
            return;
        }

        //tileStream.SendStringToStream(player.username);

        serverPipeIn = new BlockingCollection<NetworkMessage>();
        serverPipeOut = new BlockingCollection<NetworkMessage>();

        //Should probably start the UDP thread at some point here, right?
        serverUDP = new Thread(() => ServerUDP());
        serverUDP.Start();

        //Running TCP server loop
        try
        {
            while (true)
            {
                //Send outgoing TCP messages
                if (serverPipeOut.TryTake(out NetworkMessage newObject))
                {
                    //Debug.Log($"Sending Message to server: (Type): {newObject.messageType} (message): {Encoding.ASCII.GetString(newObject.message)}");
                    tileStream.SendStringToStream(newObject.messageType);
                    tileStream.SendBytesToStream(newObject.message);

                }

                //Receive incoming TCP messages
                if(tileStream.Available > 0)
                    serverPipeIn.Add(GetNetworkMessage(tileStream));
            }

        }
        catch (Exception e) {

            Debug.Log("Failed to run message loop");
            Debug.Log(e.Message);
        }
    }

    public NetworkMessage GetNetworkMessage(TileStream tileStream)
    {
        string messageType = tileStream.GetStringFromStream();

        /*byte[] tmpMessageLength = new byte[4];

        while (serverTCPSocket.Available < 4) { }
        
        serverTCPSocket.Read(tmpMessageLength, 0, tmpMessageLength.Length);
        int messageLength = BitConverter.ToInt32(tmpMessageLength, 0);*/

        //while (serverTCPSocket.Available < messageLength) { }

        //byte[] message = new byte[messageLength];

        byte[] message = tileStream.GetBytesFromStream();

        //serverTCPSocket.GetStream().Read(message, 0, messageLength);

        return new NetworkMessage(messageType, message);
    }

    IEnumerator HandleMessage()
    {

        while (true)
        {
            yield return null;

            if (!(serverPipeIn == null))
                break;
        }

        while (true)
        {
            if (serverPipeIn.TryTake(out NetworkMessage message))
            {
                Debug.Log($"Got message (Type): {message.messageType}\n (message):{Encoding.ASCII.GetString(message.message)}");
                switch (message.messageType)
                {
                    case "tempType":
                        Debug.Log(message.message.ToString());
                        break;
                    case "PlayerPos":
                        Debug.Log(message.message.ToString());
                        break;
                    default: break;
                }
            }

            yield return null;
        }
    }

    public void ServerUDP()
    {
        UdpClient server = new UdpClient(serverData.Ip, serverData.Port);

        byte[] buffer = Encoding.ASCII.GetBytes("joinServer\n");
        server.Send(buffer, buffer.Length);
    }

    public void ActivateTile()
    {
        StartCoroutine(InitializeMe());
    }

    IEnumerator InitializeMe()
    {
        BlockingCollection<ServerData> pipe = new BlockingCollection<ServerData>(1);

        Thread thread = new Thread(() => ContactCore(pipe));
        thread.Start();

        while (true)
        {
            yield return null;

            if (!thread.IsAlive)
                break;
        }

        pipe.TryTake(out ServerData tmpData);

        if (tmpData == null)
            yield break;

        serverData = tmpData;
        hasServer = true;

        thread = new Thread(() => ContactServerAndRequestObjects());
        thread.Start();

        while (true)
        {
            yield return null;

            if (!thread.IsAlive)
                break;
        }

        string assetBundleDirectoryPath = Application.dataPath + "/AssetBundles/" + x + "," + y + "/";

        if(File.Exists(assetBundleDirectoryPath + "tileobjects"))
        {
            var prefab = AssetBundle.LoadFromFile(assetBundleDirectoryPath + "tileobjects");

            UnityEngine.Object[] tileObjectsAll = prefab.LoadAllAssets();

            foreach (var tileObject in tileObjectsAll)
                Instantiate(tileObject, tileObjects.transform);

            prefab.Unload(false);
        }

        if (File.Exists(assetBundleDirectoryPath + "ground"))
        {
            var prefab = AssetBundle.LoadFromFile(assetBundleDirectoryPath + "ground");

            UnityEngine.Object[] tileObjectsAll = prefab.LoadAllAssets();

            foreach (var tileObject in tileObjectsAll)
                Instantiate(tileObject, groundHolder.transform);

            prefab.Unload(false);
        }
    }

    public void ContactCore(BlockingCollection<ServerData> pipe)
    {

        using TcpClient client = new TcpClient(coreAddress.ToString(), corePort);

        SslStream sslStream = CoreCommunication.EstablishSslStreamFromTcpAsClient(client);

        CoreCommunication.SendStringToStream(sslStream, "client");

        CoreCommunication.SendStringToStream(sslStream, "getServer");

        CoreCommunication.SendStringToStream(sslStream, x.ToString());

        CoreCommunication.SendStringToStream(sslStream, y.ToString());

        string serverJSON = CoreCommunication.GetStringFromStream(sslStream);

        if (serverJSON.Equals("DoesNotExist"))
            return;

        serverData = JsonUtility.FromJson<ServerData>(serverJSON);
        pipe.Add(serverData);
    }

    //Kill coroutine communicating with server.
    private void OnDestroy()
    {
        //serverTCP.Abort();
        //serverUDP.Abort();
    }

    //Sets a tile back to its default state with the template ground.
    void ClearServer()
    {

        StartCoroutine(ClearGroundAndTileObjects());
        Instantiate(templateGroundHolder, groundHolder.transform);

    }

    //Erases all ground and TileObject data
    public IEnumerator ClearGroundAndTileObjects()
    {
        //This should only ever run once at a time, but idk,
        //maybe someone will do some magic and have a server with multiple ground objects...
        while (groundHolder.transform.childCount > 0)
        {
            Destroy(groundHolder.transform.GetChild(0));
            yield return null;
        }


        while (tileObjects.transform.childCount > 0)
        {
            Destroy(groundHolder.transform.GetChild(0));
            yield return null;
        }
    }

    //Assigns a server to the tile, then contacts the server to load all necessary data.
    public void SetServer(IPAddress address, int port, string name, string owner)
    {
        serverData.Ip = address.ToString();
        serverData.Port = port;
        serverData.Name = name;
        serverData.OwnerID = owner;

        ClearGroundAndTileObjects();
    }

    public void ContactServerAndRequestObjects()
    {
        TileStream tileStream;

        try
        {
            tileStream = new TileStream(new TcpClient(serverData.Ip, serverData.Port));
            tileStream.ActivateStream(serverData.PublicKey);
        }
        catch(Exception)
        {
            return;
        }

        if (!tileStream.Connected)
        {
            Console.WriteLine("Failed to connect to server of tile " + transform.parent.name + ".");
            return;
        }

        ServerConnectAndGetGameObjects(tileStream);
    }

    public void ServerConnectAndGetGameObjects(TileStream server)
    {

        server.SendStringToStream("getAssets");

        switch (Application.platform)
        {
            case RuntimePlatform.WindowsPlayer:
            case RuntimePlatform.WindowsEditor:
                server.SendStringToStream("w");
                break;
            case RuntimePlatform.OSXPlayer:
            case RuntimePlatform.OSXEditor:
                server.SendStringToStream("m");
                break;
            case RuntimePlatform.LinuxPlayer:
            case RuntimePlatform.LinuxEditor:
                server.SendStringToStream("l");
                break;
            default:
                throw new Exception("Unsupported OS");
        }

        int numFiles = BitConverter.ToInt32(server.GetBytesFromStream());

        string assetBundleDirectoryPath = Application.dataPath + "/AssetBundles/" + x + "," + y + "/";

        if (!Directory.Exists(assetBundleDirectoryPath))
            Directory.CreateDirectory(assetBundleDirectoryPath);

        for(int i = 0; i < numFiles; i++)
        {
            string fileName = server.GetStringFromStream();

            byte[] fileBuffer = server.GetBytesFromStream();

            System.IO.File.WriteAllBytes(assetBundleDirectoryPath + fileName, fileBuffer);
        }
    }
}
