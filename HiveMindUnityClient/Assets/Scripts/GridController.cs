using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEngine;

public class GridController : MonoBehaviour
{
    public int corePort;
    public string coreIPString;
    public IPAddress coreAddress;
    public GameObject player;

    [SerializeField] GameObject playerPrefab;
    [SerializeField] GameObject hexTile;
    [SerializeField] GameObject hexTileTemplate;
    [SerializeField] GameObject exitTilePrefab;
    
    GameObject exitTile;

    [SerializeField] int renderDistance;
    private float tileSize;

    private struct Key
    {
        public readonly int Dimension1;
        public readonly int Dimension2;
        public Key(int x, int y)
        {
            Dimension1 = x;
            Dimension2 = y;
        }
    }

    private Hashtable grid = new Hashtable();

    private void clearFolder(string FolderName)
    {
        DirectoryInfo dir = new DirectoryInfo(FolderName);

        foreach (FileInfo fi in dir.GetFiles())
        {
            fi.Delete();
        }

        foreach (DirectoryInfo di in dir.GetDirectories())
        {
            clearFolder(di.FullName);
            di.Delete();
        }
    }

    // Start is called before the first frame update
    void Start()
    {

        //Delete this later and fix
        if (!IPAddress.TryParse(coreIPString, out coreAddress))
            coreAddress = Dns.Resolve(coreIPString).AddressList[0];

        //This set of lines is a hack and needs to be fixed later

        string assetBundleDirectoryPath = Application.dataPath + "/AssetBundles";

        if (Directory.Exists(assetBundleDirectoryPath))
            clearFolder(assetBundleDirectoryPath);


        tileSize = hexTileTemplate.GetComponent<Renderer>().bounds.size.z;

        SpawnTiles();


        exitTile = Instantiate(exitTilePrefab, ((GameObject)grid[new Key(0, 0)]).transform);

        MoveExit exitTileMove = exitTile.GetComponent<MoveExit>();

        NetworkController.activeServer = ((GameObject)grid[new Key(0, 0)]).GetComponent<HexTileController>();

        exitTileMove.gridController = this;
        exitTileMove.x = 0;
        exitTileMove.y = 0;

        player.transform.SetPositionAndRotation(((GameObject)grid[new Key(0, 0)]).transform.GetChild(1).transform.position, player.transform.rotation);
    }

    void SpawnTiles(int posX = 0, int posY = 0)
    {

        HashSet<Key> tiles = new HashSet<Key>();

        foreach (DictionaryEntry entry in grid)
            tiles.Add((Key) entry.Key);

        /*TcpClient tcpClient;

        try
        {
            tcpClient = new TcpClient(coreAddress.ToString(), corePort);
        }
        catch (Exception)
        {
            //throw new Exception("Failed to connect to core.");
            return;
        }*/

        //SslStream sslStream = CoreCommunication.EstablishSslStreamFromTcpAsClient(tcpClient);

        //CoreCommunication.SendStringToStream(sslStream, "client");

        //CoreCommunication.SendStringToStream(sslStream, "getServers");

        for (int x = -renderDistance + posX; x < renderDistance + posX; x++)
            for (int y = -renderDistance + posY; y < renderDistance + posY; y++)
            {
                SpawnTile(x, y/*, sslStream*/);
                tiles.Remove(new Key(x, y));
            }

        foreach(var key in tiles)
        {
            Destroy((GameObject) grid[key]);
            grid.Remove(key);
        }
    }

    //This function is public; I'm concerned that a malicious server could use this to force the client onto a different server.
    public void ChangeActiveTile(byte direction, int x, int y)
    {
        ((GameObject)grid[new Key(x, y)]).GetComponent<HexTileController>().Disconnect();

        int newX, newY;
        int offset = Math.Abs(x % 2);

        switch (direction)
        {
            case 0:
                newX = x;
                newY = y + 1;
                break;
            case 1:
                newX = x + 1;
                newY = y + offset;
                break;
            case 2:
                newX = x + 1;
                newY = y - 1 + offset;
                break;
            case 3:
                newX = x;
                newY = y - 1;
                break;
            case 4:
                newX = x - 1;
                newY = y - 1 + offset;
                break;
            case 5:
                newX = x - 1;
                newY = y + offset;
                break;
            default:
                throw new Exception("Unsupported direction received!");
        }

        float yCoord = exitTile.transform.position.y;

        Destroy(exitTile);

        exitTile = Instantiate(exitTilePrefab, ((GameObject)grid[new Key(newX, newY)]).transform);
        exitTile.transform.position = new UnityEngine.Vector3(exitTile.transform.position.x, yCoord, exitTile.transform.position.z);

        MoveExit exitTileMove = exitTile.GetComponent<MoveExit>();

        exitTileMove.gridController = this;
        exitTileMove.x = newX;
        exitTileMove.y = newY;

        NetworkController.activeServer = ((GameObject)grid[new Key(newX, newY)]).GetComponent<HexTileController>();

        SpawnTiles(newX, newY);

        ((GameObject)grid[new Key(newX, newY)]).GetComponent<HexTileController>().ContactTileServer();
    }

    void SpawnTile(int x, int y/*, SslStream tcpClient*/)
    {

        if (grid.ContainsKey(new Key(x, y)))
            return;

        float offsetX = x * tileSize * Mathf.Cos(Mathf.Deg2Rad * 30);
        float offsetY = x % 2 == 0 ? 0 : tileSize / 2;

        offsetY += y * tileSize;

        grid.Add(new Key(x, y), Instantiate(hexTile, new UnityEngine.Vector3(offsetX, 250 * Mathf.PerlinNoise(offsetX / 5000, offsetY / 5000), offsetY), transform.rotation, this.transform));

        GameObject tile = ((GameObject)grid[new Key(x, y)]);
        HexTileController tileController = tile.GetComponent<HexTileController>();

        tile.name = x + ", " + y;
        tileController.x = x;
        tileController.y = y;
        tileController.player = player.GetComponent<PlayerInfo>();
        tileController.ActivateTile();
    }
}
