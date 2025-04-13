using System.Collections;
using System.IO;
using System.IO.Pipes;
using UnityEngine;

public class ObjectManager : MonoBehaviour
{

    static string objectDirectory = "objectDirectory/";
    ObjectDecomposer decomposer;
    [SerializeField] ObjectComposer composer;

    // Start is called before the first frame update
    void Awake()
    {
        if (!Directory.Exists(objectDirectory))
            Directory.CreateDirectory(objectDirectory);

        decomposer = ScriptableObject.CreateInstance<ObjectDecomposer>();
    }
    public string DecomposeObject(GameObject objectToDecompose)
    {
        return decomposer.Decompose(objectToDecompose);
    }

    public bool HashExists(string hash)
    {
        return File.Exists(objectDirectory + hash);
    }

    public IEnumerator Compose(string avatarHash, Transform transform, TileStream tileStream = null)
    {
        yield return StartCoroutine(composer.Compose(avatarHash, transform, tileStream));
    }

    public bool GetRequestedAssets(string hash, out byte[] objectBytes)
    {
        if(!HashExists(hash)){
            objectBytes = null;
            return false;
        }

        objectBytes = File.ReadAllBytes(objectDirectory + hash);
        return true;
    }

    public void SendRequestedObjects(TileStream client)
    {
        while (true)
        {
            //Read if asset wanted (end if 0)
            if (client.GetBytesFromStream()[0] == 0)
                return;

            //Read hash of file desired
            string hash = client.GetStringFromStream();
            byte[] objectBytes;

            if (!GetRequestedAssets(hash, out objectBytes))
            {
                client.SendBytesToStream(new byte[1] { 0 });
                continue;
            }

            //Write 1 and then write bytes
            client.SendBytesToStream(new byte[1] { 1 });
            client.SendBytesToStream(objectBytes);
        }
    }

    public IEnumerator SpawnObjectAsClient(string hash, Transform parentTransform, TileStream tileStream = null)
    {
        yield return StartCoroutine(composer.Compose(hash, parentTransform, tileStream));
    }

}
