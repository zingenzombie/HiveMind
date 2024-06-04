using System.Collections;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System;
using System.Threading;
using UnityEditor;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

public class ServerController : MonoBehaviour
{
    private Thread listen;
    public IPAddress coreAddress;
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


    private void OnApplicationQuit()
    {
        if(listen != null)
            listen.Abort();
    }

    // Start is called before the first frame update
    private void Start()
    {
        ipAddress = IPAddress.Parse(serverIPString);

        //Delete this later and fix
        coreAddress = IPAddress.Parse(coreIPString);

        groundHolder = hexTile.transform.GetChild(0).gameObject;
        groundHolder = hexTile.transform.GetChild(1).gameObject;

        ContactCore();
        ClientConnectListener();
    }

    private void ClientConnectListener()
    {
        listen = new Thread(() => ListenForClients());
        listen.Start();
    }

    void ContactCore()
    {
        ServerData tmp = new ServerData(requestXY, x, y, serverName, ipAddress.ToString(), port, ownerID);
        string jsonString = JsonUtility.ToJson(tmp) + '\n';
        byte[] bytes = Encoding.ASCII.GetBytes(jsonString);

        using TcpClient tcpClient = new TcpClient(coreAddress.ToString(), corePort);

        if (!tcpClient.Connected)
        {
            Console.WriteLine("Failed to connect to core!");
            return;
        }

        byte[] buffer = Encoding.ASCII.GetBytes("server");
        tcpClient.GetStream().Write(buffer);

        buffer = Encoding.ASCII.GetBytes("newServer\n");
        tcpClient.GetStream().Write(buffer);

        tcpClient.GetStream().Write(bytes);
        Debug.Log("Wrote " + bytes.Length + " bytes.");

        foreach(var character in bytes)
            Console.WriteLine((char)character);

        string result = CoreCommunication.GetStringFromStream(tcpClient);

        if(!result.Equals("SUCCESS"))
            throw new Exception("Failed to reserve space on core grid. Message received: " + result);
    }

    private void ListenForClients()
    {
        TcpListener server = new TcpListener(IPAddress.Any, port);
        // we set our IP address as server's address, and we also set the port

        server.Start();  // this will start the server

        while (true)   //we wait for a connection
        {
            TcpClient client;
            if (server.Pending())
            {
                client = server.AcceptTcpClient();  //if a connection exists, the server will accept it

                if (client.Connected)  //while the client is connected, we look for incoming messages
                {
                    Thread thread = new Thread(() => HandleNewClient(client));
                    thread.Start();
                }
                else
                {
                    Debug.Log("Error! TCP client connection attempt received, but connection failed before handling!");
                }
            }
        }
    }

    private void HandleNewClient(TcpClient client)
    {
        //Any calls to GetStringFromStream() MUST NOT be called
        //from the main thread to prevent hanging in the case of
        //a lagging \n character. This violates that rule...

        switch (CoreCommunication.GetStringFromStream(client))
        {
            case "getAssets":
                GetAssets(client);
                break;
            case "joinServer":

                break;

            default: break;
        }
    }

    private void GetAssets(TcpClient client)
    {

        string assetBundleDirectoryPath = Application.dataPath + "/AssetBundles";

        string typeOS = CoreCommunication.GetStringFromStream(client);

        if (!(typeOS.Equals("w") || typeOS.Equals("m") || typeOS.Equals("l")))
        {
            client.Close();
            Debug.Log("Client sent invalid OS type.");
            return;
        }

        Debug.Log(typeOS);

        string[] fileNames = Directory.GetFiles(assetBundleDirectoryPath + "/" + typeOS);

        client.GetStream().Write(Encoding.ASCII.GetBytes(fileNames.Length.ToString() + '\n'));

        foreach(string fileName in fileNames)
        {
            Debug.Log(fileName);
            Debug.Log(new System.IO.FileInfo(fileName).Name);
            Debug.Log(new System.IO.FileInfo(fileName).Length);

            client.GetStream().Write(Encoding.ASCII.GetBytes(new System.IO.FileInfo(fileName).Name+ '\n'));
            client.GetStream().Write(Encoding.ASCII.GetBytes(((int) new System.IO.FileInfo(fileName).Length).ToString() + '\n'));
            
            //SendFile() had issues with files above ~16 KB in size on macOS, so this solution was implemented instead.
            byte[] buffer = new byte[8192]; // 8 KB buffer size
            using (FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                int bytesRead;
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    client.GetStream().Write(buffer, 0, bytesRead);
                }
            }
            //client.Client.SendFile(fileName);
        }
    }
}
