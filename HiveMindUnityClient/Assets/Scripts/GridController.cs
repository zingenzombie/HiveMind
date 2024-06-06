using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Numerics;
using Unity.VisualScripting;
using UnityEngine;

public class GridController : MonoBehaviour
{

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
        // Equals and GetHashCode ommitted
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
        //This set of lines is a hack and needs to be fixed later

        string assetBundleDirectoryPath = Application.dataPath + "/AssetBundles";

        if (Directory.Exists(assetBundleDirectoryPath))
            clearFolder(assetBundleDirectoryPath);


        tileSize = hexTileTemplate.GetComponent<Renderer>().bounds.size.z;

        SpawnTiles();

        exitTile = Instantiate(exitTilePrefab, ((GameObject)grid[new Key(0, 0)]).transform);
        exitTile.GetComponent<MoveExit>().gridController = this;
        exitTile.GetComponent<MoveExit>().x = 0;
        exitTile.GetComponent<MoveExit>().y = 0;

        GameObject tmp = Instantiate(playerPrefab);

        tmp.transform.SetPositionAndRotation(((GameObject)grid[new Key(0, 0)]).transform.GetChild(1).transform.position, tmp.transform.rotation);
    }

    void SpawnTiles(int posX = 0, int posY = 0)
    {
        for (int x = -renderDistance + posX; x < renderDistance + posX; x++)
            for (int y = -renderDistance + posY; y < renderDistance + posY; y++)
                SpawnTile(x, y);

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

        float yCoord = exitTile.transform.position.y;

        Destroy(exitTile);

        exitTile = Instantiate(exitTilePrefab, ((GameObject)grid[new Key(newX, newY)]).transform);
        exitTile.transform.position = new UnityEngine.Vector3(exitTile.transform.position.x, yCoord, exitTile.transform.position.z);
        exitTile.GetComponent<MoveExit>().gridController = this;
        exitTile.GetComponent<MoveExit>().x = newX;
        exitTile.GetComponent<MoveExit>().y = newY;

        SpawnTiles(newX, newY);

        //Contact tile server (if present).
    }

    void SpawnTile(int x, int y)
    {

        if (grid.ContainsKey(new Key(x, y)))
            return;

        float offsetX = x * tileSize * Mathf.Cos(Mathf.Deg2Rad * 30);
        float offsetY = x % 2 == 0 ? 0 : tileSize / 2;

        offsetY += y * tileSize;

        grid.Add(new Key(x, y), Instantiate(hexTile, new UnityEngine.Vector3(offsetX, 250 * Mathf.PerlinNoise(offsetX / 5000, offsetY / 5000), offsetY), transform.rotation, this.transform));
        ((GameObject)grid[new Key(x, y)]).name = x + ", " + y;
        ((GameObject)grid[new Key(x, y)]).GetComponent<HexTileController>().x = x;
        ((GameObject)grid[new Key(x, y)]).GetComponent<HexTileController>().y = y;
        ((GameObject)grid[new Key(x, y)]).GetComponent<HexTileController>().ContactCore();
    }
}
