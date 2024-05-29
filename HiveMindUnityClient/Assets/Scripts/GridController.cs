using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class GridController : MonoBehaviour
{

    public GameObject hexTile;
    public GameObject hexTileTemplate;
    public int renderDistance;
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

    // Start is called before the first frame update
    void Start()
    {

        tileSize = hexTileTemplate.GetComponent<Renderer>().bounds.size.z;

        InitialSpawning();
    }

    void InitialSpawning(int posX = 0, int posY = 0)
    {

        for (int x = -renderDistance + posX; x < renderDistance; x++)
        {
            
            float offsetX = x * tileSize * Mathf.Cos(Mathf.Deg2Rad * 30);
            float offsetY = x % 2 == 0 ? 0 : tileSize / 2;

            for (int y = -renderDistance + posY; y < renderDistance; y++)
            {

                offsetY += tileSize;

                grid.Add(new Key(x, y), Instantiate(hexTile, new Vector3(offsetX, 1000 * Mathf.PerlinNoise(offsetX / 5000, offsetY / 5000), offsetY), transform.rotation, this.transform));
                ((GameObject) grid[new Key(x, y)]).name = x + ", " + y;

            }
        }
        IPAddress tmpAddress = IPAddress.Parse("127.0.0.1");
        ((GameObject) grid[new Key(0,0)]).GetComponent<HexTileController>().SetServer(tmpAddress, 3622, "testServer", "Zin");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
