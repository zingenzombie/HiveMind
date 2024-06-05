using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEditor;
using UnityEngine;

public class HexTileController : MonoBehaviour
{
    public int corePort, x, y;
    public string coreAddress;
    public bool hasServer = false;
    public ServerData serverData;

    public GameObject groundHolder;
    public GameObject templateGroundHolder;
    public GameObject tileObjects;


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
        coreAddress = "hive.honeydragonproductions.com";
        corePort = 3621;

        ClearServer();
    }

    public void ContactCore()
    {
        TcpClient tcpClient;

        try
        {
            tcpClient = new TcpClient(coreAddress, corePort);
        }
        catch (Exception)
        {
            //throw new Exception("Failed to connect to core.");
            return;
        }

        if (!tcpClient.Connected)
            throw new Exception("Failed to connect to core.");

        byte[] buffer = Encoding.ASCII.GetBytes("client");
        tcpClient.GetStream().Write(buffer);

        buffer = Encoding.ASCII.GetBytes("getServer\n");
        tcpClient.GetStream().Write(buffer);

        buffer = Encoding.ASCII.GetBytes(x.ToString() + '\n');
        tcpClient.GetStream().Write(buffer);

        buffer = Encoding.ASCII.GetBytes(y.ToString() + '\n');
        tcpClient.GetStream().Write(buffer);

        string serverJSON = CoreCommunication.GetStringFromStream(tcpClient);

        if (serverJSON.Equals("DoesNotExist"))
            return;

        print(serverJSON);

        try
        {
            serverData = JsonUtility.FromJson<ServerData>(serverJSON);
        }
        catch (Exception)
        {
            Debug.Log("Server data JSON given was improper");
            return;
        }

        hasServer = true;

        ClearGroundAndTileObjects();
        ContactServerAndRequestObjects();
    }

    private void OnDestroy()
    {
        //Kill coroutine communicating with server.
    }

    //Sets a tile back to its default state with the template ground.
    void ClearServer()
    {

        StartCoroutine(ClearGroundAndTileObjects());
        Instantiate(templateGroundHolder, groundHolder.transform);

    }

    //Erases all ground and TileObject data
    IEnumerator ClearGroundAndTileObjects()
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
        ContactServerAndRequestObjects();
    }

    void ContactServerAndRequestObjects()
    {

        using TcpClient tcpClient = new TcpClient(serverData.Ip, serverData.Port);

        if(!tcpClient.Connected)
            Console.WriteLine("Failed to connect to server of tile " + transform.parent.name + ".");

        StartCoroutine(ServerConnectAndGetGameObjects(tcpClient));

    }

    //This should be moved to another thread and piped back.
    IEnumerator ServerConnectAndGetGameObjects(TcpClient server)
    {

        byte[] buffer = Encoding.ASCII.GetBytes("getAssets\n");
        server.GetStream().Write(buffer);

        switch (Application.platform)
        {
            case RuntimePlatform.WindowsPlayer:
            case RuntimePlatform.WindowsEditor:
                server.GetStream().Write(Encoding.ASCII.GetBytes("w\n"));
                break;
            case RuntimePlatform.OSXPlayer:
            case RuntimePlatform.OSXEditor:
                server.GetStream().Write(Encoding.ASCII.GetBytes("m\n"));
                break;
            case RuntimePlatform.LinuxPlayer:
            case RuntimePlatform.LinuxEditor:
                server.GetStream().Write(Encoding.ASCII.GetBytes("l\n"));
                break;
            default:
                throw new Exception("Unsupported OS");
        }

        string numFilesStr = CoreCommunication.GetStringFromStream(server);

        int numFiles;

        if (Int32.TryParse(numFilesStr, out numFiles))
        {

            string assetBundleDirectoryPath = Application.dataPath + "/AssetBundles/" + x + "," + y + "/";

            if (!Directory.Exists(assetBundleDirectoryPath))
                Directory.CreateDirectory(assetBundleDirectoryPath);

            Debug.Log(numFiles);

            for(int i = 0; i < numFiles; i++)
            {
                string fileName = CoreCommunication.GetStringFromStream(server);

                //This does not verify that the given string is a long!!!
                int fileSize = Int32.Parse(CoreCommunication.GetStringFromStream(server));

                byte[] fileBuffer = new byte[fileSize];

                /*
                while (server.Available < fileSize) { }
                server.GetStream().Read(fileBuffer, 0, fileSize);*/

                int bytesRead = 0;
                while (bytesRead < fileSize)
                {
                    int read = server.GetStream().Read(fileBuffer, bytesRead, fileSize - bytesRead);
                    if(read == 0 && !CoreCommunication.IsConnected(server))
                    {
                        //Handle stream closed or error connection
                        break;
                    }
                    bytesRead += read;
                }

                System.IO.File.WriteAllBytes(assetBundleDirectoryPath + fileName, fileBuffer);

                Debug.Log(fileName);
            }

            var prefab = AssetBundle.LoadFromFile(assetBundleDirectoryPath + "tileobjects");

            UnityEngine.Object[] tileObjectsAll = prefab.LoadAllAssets();

            foreach (var tileObject in tileObjectsAll)
            {
                Instantiate(tileObject, tileObjects.transform);
            }

            prefab.Unload(false);

        }
            

        yield return null;
    }
}
