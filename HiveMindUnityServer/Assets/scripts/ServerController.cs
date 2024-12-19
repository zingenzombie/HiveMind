using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System;
using System.Threading;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography;
using System.Collections.Generic;

public class ServerController : MonoBehaviour
{
    private Thread listen;
    private IPAddress coreAddress;
    public int corePort;
    public GameObject hexTile;
    public string serverIPString, coreIPString;

    public bool requestXY;
    public IPAddress ipAddress;
    public int port;
    public int x, y;
    public string serverName;
    public string ownerID;
    [SerializeField] GameObject hexTileTemplate;
    private float tileSize;
    public bool initialized = false;

    [SerializeField] PlayerManager playerManager;

    List<string> staticHashes;

    private RSACryptoServiceProvider localRSA;

    static string objectDirectory = "objectDirectory/";

    private void OnApplicationQuit()
    {
        if(listen != null)
            listen.Abort();
    }

    // Start is called before the first frame update
    private void Start()
    {
        staticHashes = new List<string>();

        //Initialize static objects
        ObjectDecomposer decomposer = ScriptableObject.CreateInstance<ObjectDecomposer>();

        Transform tileObjects = hexTile.transform.GetChild(1);

        int numChildren = tileObjects.transform.childCount;

        for(int i = 0; i < numChildren; i++)
            staticHashes.Add(decomposer.Decompose(tileObjects.transform.GetChild(i).gameObject));



        ObjectComposer objectComposer = gameObject.AddComponent<ObjectComposer>();

        foreach(var iter in staticHashes)
            StartCoroutine(objectComposer.Compose(iter, transform));



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

        InitializeWithCore();

        tileSize = hexTileTemplate.GetComponent<Renderer>().bounds.size.z;

        float offsetX = x * tileSize * Mathf.Cos(Mathf.Deg2Rad * 30);
        float offsetY = x % 2 == 0 ? 0 : tileSize / 2;

        offsetY += y * tileSize;

        hexTile.transform.SetLocalPositionAndRotation(new UnityEngine.Vector3(offsetX, 250 * Mathf.PerlinNoise(offsetX / 5000, offsetY / 5000), offsetY), transform.rotation);

        ClientConnectListener();
    }

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
            if (server.Pending())
            {
                Thread thread = new Thread(() => HandleNewClient(server.AcceptTcpClient()));
                thread.Start();
            }
        }
    }

    private void HandleNewClient(TcpClient tcpStream)
    {
        TileStream client = new TileStream(tcpStream);
        client.ActivateStream(localRSA);

        switch (client.GetStringFromStream())
        {
            case "getStaticAssets":
                GetStaticAssets(client);
                break;
            case "getAssets":
                GetAssets(client);
                break;
            case "joinServer":
                playerManager.AddPlayer(client);
                break;

            default: break;
        }
    }

    void GetStaticAssets(TileStream client)
    {
        client.SendBytesToStream(BitConverter.GetBytes(staticHashes.Count));

        //Return hash(es) of static element(s)
        foreach(string iter in staticHashes)
        {
            client.SendStringToStream(iter);
            GetAssets(client);
        }

    }

    void GetAssets(TileStream client)
    {
        while (true)
        {
            //Read if asset wanted (end if 0)
            if (client.GetBytesFromStream()[0] == 0)
                return;

            //Read hash of file desired
            string hash = client.GetStringFromStream();

            if (!File.Exists(objectDirectory + hash))
            {
                client.SendBytesToStream(new byte[1]{0});
                continue;
            }

            //Write 1 and then write bytes
            client.SendBytesToStream(new byte[1] {1});
            client.SendBytesToStream(File.ReadAllBytes(objectDirectory + hash));
        }
    }
}
