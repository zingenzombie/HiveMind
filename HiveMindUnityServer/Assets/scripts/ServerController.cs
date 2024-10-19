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
    public static SynchronizationContext mainThreadContext;

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

    [SerializeField] GameObject PlayersContainer;
    [SerializeField] GameObject DynamicObjects;
    [SerializeField] GameObject hexTileTemplate;
    private float tileSize;
    public bool initialized = false;

    private RSACryptoServiceProvider localRSA;

    Thread coreListener;
    BlockingCollection<string> CoreMessages = new BlockingCollection<string>();

    SslStream coreConnection;

    private void OnApplicationQuit()
    {
        if (listen != null)
            listen.Abort();
        if (coreListener != null)
            coreListener.Abort();
    }

    private void Awake()
    {
        mainThreadContext = SynchronizationContext.Current;
        Debug.Log(mainThreadContext);
        HiveServerEvents.OnInitStatusFromCore += HandleCoreRequestResult;
        HiveServerEvents.OnPlayerJoined += PlayerData.SpawnNewPlayer;
    }

    private void OnDisable()
    {
        HiveServerEvents.OnInitStatusFromCore -= HandleCoreRequestResult;
        HiveServerEvents.OnPlayerJoined -= PlayerData.SpawnNewPlayer;
    }

    // Start is called before the first frame update
    private void Start()
    {
        
        localRSA = new RSACryptoServiceProvider();

        ipAddress = IPAddress.Parse(serverIPString);

        //Delete this later and fix
        if (!IPAddress.TryParse(coreIPString, out coreAddress))
            coreAddress = Dns.Resolve(coreIPString).AddressList[0];

        //groundHolder = hexTile.transform.GetChild(0).gameObject;
        groundHolder = hexTile.transform.GetChild(1).gameObject;

        InitializeWithCore();
        if (initialized == false)
            throw new System.Exception("You could not reserve a spot on the server even after getting verified with it");

        tileSize = hexTileTemplate.GetComponent<Renderer>().bounds.size.z;

        float offsetX = x * tileSize * Mathf.Cos(Mathf.Deg2Rad * 30);
        float offsetY = x % 2 == 0 ? 0 : tileSize / 2;

        offsetY += y * tileSize;

        hexTile.transform.SetLocalPositionAndRotation(new UnityEngine.Vector3(offsetX, 250 * Mathf.PerlinNoise(offsetX / 5000, offsetY / 5000), offsetY), transform.rotation);

        Thread checkWithCore = new Thread(() => CheckWithCore());

        StartCoroutine(ReadCoreMessages());

        //begin checking for new client connections
        listen = new Thread(() => ListenForClientsTCP());
        listen.Start();
    }

    void InitializeWithCore()
    {
        ServerData tmp = new ServerData(requestXY, x, y, serverName, ipAddress.ToString(), port, ownerID, localRSA.ToXmlString(false));
        string jsonString = JsonUtility.ToJson(tmp);
        byte[] bytes = Encoding.ASCII.GetBytes(jsonString);

        using TcpClient core = new TcpClient(coreAddress.ToString(), corePort);

        SslStream sslStream = CoreCommunication.EstablishSslStreamFromTcpAsClient(core);
        coreConnection = sslStream;

        //telling the core what kind of connection this is
        CoreCommunication.SendStringToStream(coreConnection, "server");

        CoreCommunication.SendStringToStream(coreConnection, "newServer");

        byte[] bytesFinal = new byte[bytes.Length + 1];

        int i = 0;
        foreach (byte b in bytes)
            bytesFinal[i++] = b;

        bytesFinal[i] = 0x00;

        sslStream.Write(bytesFinal);
        Debug.Log($"Sent over server info (of size: {bytesFinal.Length} bytes)");

        var tileReqResult = CoreCommunication.GetStringFromStream(coreConnection);
        HiveServerEvents.callInitCoreStatus(tileReqResult);
    }

    private void HandleCoreRequestResult(string result)
    {
        if (result.Contains("GRANTED"))
            initialized = true;
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

    public void KillServer()
    {

    }

    private void ListenForClientsTCP()
    {
        TcpListener server = new TcpListener(IPAddress.Any, port);
        // we set our IP address as server's address, and we also set the port

        server.Start();  // this will start the server

        while (true)   //we wait for a connection
        {
            TileStream client = new TileStream(server.AcceptTcpClient()); // Blocking call
            Debug.Log("New Client Connected");
            client.ActivateStream(localRSA);
            mainThreadContext.Post(_ => PlayerData.SpawnNewPlayer(client), null);
        }
    }
}
