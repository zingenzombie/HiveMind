using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class HexTileController : MonoBehaviour
{


    public IPAddress ipAddress;
    public int port;
    public string serverName;
    public string serverOwner;

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
    void Start()
    {
        ClearServer();
    }

    private void OnDestroy()
    {
        //Kill coroutine communicating with server.
    }

    //Sets a tile back to its default state with the template ground.
    void ClearServer()
    {
        ipAddress = null;
        port = 0;
        serverName = string.Empty;
        serverOwner = string.Empty;

        ClearGroundAndTileObjects();

        Instantiate(templateGroundHolder, groundHolder.transform);

    }

    //Erases all ground and TileObject data
    void ClearGroundAndTileObjects()
    {
        //This should only ever run once at a time, but idk,
        //maybe someone will do some magic and have a server with multiple ground objects...
        while (groundHolder.transform.childCount > 0)
            Destroy(groundHolder.transform.GetChild(0));


        while (tileObjects.transform.childCount > 0)
            Destroy(groundHolder.transform.GetChild(0));
    }

    //Assigns a server to the tile, then contacts the server to load all necessary data.
    public void SetServer(IPAddress address, int port, string name, string owner)
    {
        ipAddress = address;
        this.port = port;
        this.serverName = name;
        this.serverOwner = owner;

        ClearGroundAndTileObjects();
        ContactServerAndRequestObjects();
    }

    void ContactServerAndRequestObjects()
    {

        using TcpClient tcpClient = new TcpClient(ipAddress.ToString(), port);

        if(!tcpClient.Connected)
            Console.WriteLine("Failed to connect to server of tile " + transform.parent.name + ".");

        StartCoroutine(ServerConnectAndGetGameObjects(tcpClient));

    }

    IEnumerator ServerConnectAndGetGameObjects(TcpClient server)
    {
        byte[] buffer = new byte[5];
        buffer[0] = (byte) 't';
        buffer[1] = (byte)'e';
        buffer[2] = (byte)'s';
        buffer[3] = (byte)'t';
        buffer[4] = (byte)'\n';
        server.GetStream().Write(buffer, 0, 5);

        yield return null;
    }
}