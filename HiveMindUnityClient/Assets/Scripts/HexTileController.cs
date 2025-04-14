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

    public BlockingCollection<NetworkMessage> serverPipeIn, serverPipeOut;

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
    }

    public void ActivateTile()
    {

        if (serverData == null)
        {
            hasServer = false;
            return;
        }

        hasServer = true;
        StartCoroutine(ContactServerAndRequestObjects());
    }

    Thread thread;

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

    public IEnumerator Ascend(float targetHeight = 250f)
    {
        float duration = 1.5f; // Total time for the ascent
        float elapsed = 0f;

        Vector3 start = transform.position;
        Vector3 end = new Vector3(start.x, start.y + targetHeight, start.z);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Ease-out curve: slows down as it reaches the end
            float easedT = 1f - Mathf.Pow(1f - t, 3);

            transform.position = Vector3.Lerp(start, end, easedT);
            yield return null;
        }

        // Snap to exact position to avoid float drift
        transform.position = end;
    }

    IEnumerator Descend()
    {
        float speed = 1.25f;
        float distanceTraveled = 0f;
        float maxDistance = 250f;

        while (distanceTraveled < maxDistance)
        {
            float delta = (speed + distanceTraveled) * Time.deltaTime;
            transform.position -= new Vector3(0, delta, 0);
            
            distanceTraveled += delta;
            yield return null;
        }
    }

    public IEnumerator DestroyTile()
    {
        yield return StartCoroutine(Descend());

        Destroy(gameObject);
    }

    void OnDestroy()
    {

        if (thread != null && thread.IsAlive)
            thread.Abort();

        if (getAssets != null && getAssets.IsAlive)
            getAssets.Abort();

        if (streamFinder != null && streamFinder.IsAlive)
            streamFinder.Abort();
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
        serverData.OwnerId = owner;

        ClearGroundAndTileObjects();
    }

    void foundTileStream(BlockingCollection<TileStream> tileStreamCollection){

        TileStream tileStream;

        try
        {
            tileStream = new TileStream(new TcpClient(serverData.Ip, serverData.Port));
            tileStream.ActivateStream(serverData.PublicKey);
        }
        catch(Exception)
        {
            tileStreamCollection.Add(null);
            return;
        }

        tileStreamCollection.Add(tileStream);

    }

    Thread streamFinder;

    public IEnumerator ContactServerAndRequestObjects()
    {

        BlockingCollection<TileStream> tileStreamCollection = new BlockingCollection<TileStream>();
        streamFinder = new Thread(() => foundTileStream(tileStreamCollection));
        streamFinder.Start();

        while(tileStreamCollection.Count == 0)
            yield return null;        

        TileStream server = tileStreamCollection.Take();

        if (server == null)
        {
            Console.WriteLine("Failed to connect to server of tile " + transform.parent.name + ".");
            yield break;
        }
        StartCoroutine(ServerConnectAndGetGameObjects(server));
    }

    void sendMessage(TileStream server, byte[] message){
        server.SendBytesToStream(message);
    }

    void sendMessage(TileStream server, string message){
        server.SendStringToStream(message);
    }

    void getMessage(TileStream server, BlockingCollection<byte[]> collection){
        collection.Add(server.GetBytesFromStream());
    }

    void getMessage(TileStream server, BlockingCollection<string> collection){
        collection.Add(server.GetStringFromStream());
    }

    Thread getAssets;

    public IEnumerator ServerConnectAndGetGameObjects(TileStream server)
    {
        ObjectComposer composer = gameObject.AddComponent<ObjectComposer>();

        getAssets = new Thread(() => sendMessage(server, "getStaticAssets"));
        getAssets.Start();

        BlockingCollection<byte[]> bytes = new BlockingCollection<byte[]>();

        while(getAssets.ThreadState == ThreadState.Running)
            yield return null;

        getAssets = new Thread(() => getMessage(server, bytes));
        getAssets.Start();

        while (getAssets.ThreadState == ThreadState.Running)
            yield return null;

        int numHashes = BitConverter.ToInt32(bytes.Take());

        for(int i = 0; i < numHashes; i++)
        {

            BlockingCollection<string> hashCollection = new BlockingCollection<string>();

            getAssets = new Thread(() => getMessage(server, hashCollection));
            getAssets.Start();

            while (hashCollection.Count < 1)
                yield return null;

            string hash = hashCollection.Take();
            
            yield return StartCoroutine(composer.Compose(hash, tileObjects.transform, server));

            getAssets = new Thread(() => sendMessage(server, new byte[1] { 0 }));
            getAssets.Start();
        }
    }
}
