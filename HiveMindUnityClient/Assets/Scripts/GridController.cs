using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
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
        StartCoroutine(FirstTimeTileSpawning());
    }

    IEnumerator FirstTimeTileSpawning()
    {
        /*yield return*/ StartCoroutine(SpawnTiles(0, 0, false));

        exitTile = Instantiate(exitTilePrefab, (grid[new Key(0, 0)]).transform);
        MoveExit exitTileMove = exitTile.GetComponent<MoveExit>();

        exitTileMove.gridController = this;
        exitTileMove.x = 0;
        exitTileMove.y = 0;

        player.transform.SetPositionAndRotation((grid[new Key(0, 0)]).transform.GetChild(1).transform.position /*+ new Vector3(0,100,0)*/, player.transform.rotation);

        yield return StartCoroutine(networkController.ChangeActiveServer((grid[new Key(0, 0)]).GetComponent<HexTileController>()));

        }

    List<string> FetchServerDataFromCore(int x, int y)
    {
        using TcpClient client = new TcpClient(coreAddress.ToString(), corePort);

        SslStream sslStream = CoreCommunication.EstablishSslStreamFromTcpAsClient(client);

        CoreCommunication.SendStringToStream(sslStream, "client");

        CoreCommunication.SendStringToStream(sslStream, "getServers");

        CoreCommunication.SendIntToStream(sslStream, x);
        CoreCommunication.SendIntToStream(sslStream, y);
        CoreCommunication.SendIntToStream(sslStream, renderDistance + 1);

        int numRows = CoreCommunication.GetIntFromStream(sslStream);

        List<string> tileRows = new();

        for(int i = 0; i < numRows; i++)
            tileRows.Add(CoreCommunication.GetStringFromStream(sslStream));

        return tileRows;
    }

    IEnumerator SpawnTiles(int posX = 0, int posY = 0, bool animateTiles = true)
    {

        Debug.Log("Spawning position is " + posX + ", " + posY);

        //Send position + radius to core & have JSON of tiles returned.
        //Send data to relevant new tiles & start them.

        List<string> tileRows = null;
        bool isDone = false;

        Thread fetchTilesThread = new Thread(() =>
        {
            tileRows = FetchServerDataFromCore(posX, posY);
            isDone = true;
        });

        fetchTilesThread.Start();

        HashSet<Key> tiles = new HashSet<Key>();

        foreach (var entry in grid)
            tiles.Add(entry.Key);

        for (int x = -renderDistance + posX; x < renderDistance + posX; x++)
            for (int y = -renderDistance + posY; y < renderDistance + posY; y++)
            {
                SpawnTile(x, y, animateTiles);
                tiles.Remove(new Key(x, y));
            }

        foreach(var key in tiles)
        {
            StartCoroutine(grid[key].GetComponent<HexTileController>().DestroyTile());
            grid.Remove(key);
        }

        while (!isDone)
            yield return null;

        foreach(var tileRow in tileRows)
        {
            ServerData serverData = JsonUtility.FromJson<ServerData>(tileRow);

            HexTileController currentTile = grid[new Key(serverData.X, serverData.Y)].GetComponent<HexTileController>();

            if (currentTile.serverData != null && currentTile.serverData.PublicKey == serverData.PublicKey)
            {
                continue;
            }

            if (currentTile.serverData != null)
                currentTile.ClearGroundAndTileObjects();

            currentTile.serverData = serverData;
            currentTile.ActivateTile();
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

        StartCoroutine(SpawnTiles(newX, newY));
    }

    void SpawnTile(int x, int y, bool animateTiles)
    {

        Debug.Log("Spawning " +  x + ", " + y);

        if (grid.ContainsKey(new Key(x, y)))
            return;

        float offsetX = x * tileSize * Mathf.Cos(Mathf.Deg2Rad * 30);
        float offsetY = x % 2 == 0 ? 0 : tileSize / 2;

        offsetY += y * tileSize;

        if(animateTiles)
            grid.Add(new Key(x, y), Instantiate(hexTile, new UnityEngine.Vector3(offsetX, (250 * Mathf.PerlinNoise(offsetX / 5000, offsetY / 5000)) - 250, offsetY), transform.rotation, this.transform));
        else
            grid.Add(new Key(x, y), Instantiate(hexTile, new UnityEngine.Vector3(offsetX, (250 * Mathf.PerlinNoise(offsetX / 5000, offsetY / 5000)), offsetY), transform.rotation, this.transform));
        GameObject tile = (grid[new Key(x, y)]);
        HexTileController tileController = tile.GetComponent<HexTileController>();

        tile.name = x + ", " + y;
        tileController.x = x;
        tileController.y = y;
        tileController.player = player.GetComponent<PlayerInfo>();

        if(animateTiles)
            StartCoroutine(tileController.Ascend());
    }
}
