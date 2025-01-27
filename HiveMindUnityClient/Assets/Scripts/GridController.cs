using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
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

    NetworkController networkController;

    static string objectDirectory = "objectDirectory/";

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

    private Dictionary<Key, GameObject> grid = new Dictionary<Key, GameObject>();

    private void clearFolder(string FolderName)
    {
        DirectoryInfo dir = new DirectoryInfo(FolderName);

        foreach (FileInfo fi in dir.GetFiles())
            fi.Delete();

        foreach (DirectoryInfo di in dir.GetDirectories())
        {
            clearFolder(di.FullName);
            di.Delete();
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        networkController = GameObject.FindWithTag("NetworkController").GetComponent<NetworkController>();

        if (!Directory.Exists(objectDirectory))
            Directory.CreateDirectory(objectDirectory);

        if (!IPAddress.TryParse(coreIPString, out coreAddress))
        {
            try
            {
                coreAddress = Dns.GetHostAddresses(coreIPString)[0];
            }
            catch (Exception)
            {
                throw new Exception("No address was returned in the DNS lookup for the core server.");
            }
        }

        tileSize = hexTileTemplate.GetComponent<Renderer>().bounds.size.z;
        SpawnTiles();

        exitTile = Instantiate(exitTilePrefab, (grid[new Key(0, 0)]).transform);
        MoveExit exitTileMove = exitTile.GetComponent<MoveExit>();

        StartCoroutine(networkController.ChangeActiveServer((grid[new Key(0, 0)]).GetComponent<HexTileController>()));

        exitTileMove.gridController = this;
        exitTileMove.x = 0;
        exitTileMove.y = 0;

        player.transform.SetPositionAndRotation((grid[new Key(0, 0)]).transform.GetChild(1).transform.position, player.transform.rotation);
    }

    void SpawnTiles(int posX = 0, int posY = 0)
    {

        HashSet<Key> tiles = new HashSet<Key>();

        foreach (var entry in grid)
            tiles.Add(entry.Key);

        for (int x = -renderDistance + posX; x < renderDistance + posX; x++)
            for (int y = -renderDistance + posY; y < renderDistance + posY; y++)
            {
                SpawnTile(x, y/*, sslStream*/);
                tiles.Remove(new Key(x, y));
            }

        foreach(var key in tiles)
        {
            Destroy(grid[key]);
            grid.Remove(key);
        }
    }

    public void ChangeActiveTile(byte direction, int x, int y)
    {
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

        MoveExit moveExit = exitTile.GetComponent<MoveExit>();

        moveExit.x = newX;
        moveExit.y = newY;

        exitTile.transform.parent = (grid[new Key(newX, newY)]).transform;
        exitTile.transform.localPosition = new Vector3(0, exitTile.transform.localPosition.y, 0);

        HexTileController newTileController = (grid[new Key(newX, newY)]).GetComponent<HexTileController>();
        StartCoroutine(networkController.ChangeActiveServer(newTileController));

        SpawnTiles(newX, newY);
    }

    void SpawnTile(int x, int y/*, SslStream tcpClient*/)
    {

        if (grid.ContainsKey(new Key(x, y)))
            return;

        float offsetX = x * tileSize * Mathf.Cos(Mathf.Deg2Rad * 30);
        float offsetY = x % 2 == 0 ? 0 : tileSize / 2;

        offsetY += y * tileSize;

        grid.Add(new Key(x, y), Instantiate(hexTile, new UnityEngine.Vector3(offsetX, 250 * Mathf.PerlinNoise(offsetX / 5000, offsetY / 5000), offsetY), transform.rotation, this.transform));

        GameObject tile = (grid[new Key(x, y)]);
        HexTileController tileController = tile.GetComponent<HexTileController>();

        tile.name = x + ", " + y;
        tileController.x = x;
        tileController.y = y;
        tileController.player = player.GetComponent<PlayerInfo>();
        tileController.ActivateTile();
    }
}
