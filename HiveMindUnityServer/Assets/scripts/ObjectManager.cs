using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ObjectManager : MonoBehaviour
{

    static string objectDirectory = "objectDirectory/";
    ObjectDecomposer decomposer;
    [SerializeField] ObjectComposer composer;
    [SerializeField] GameObject dynamicObjectPrefab;

    [SerializeField] Transform dynamicObjectTransform;

    Dictionary<string, GameObject> dynamicObjects = new Dictionary<string, GameObject>();

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
        while (client.GetBytesFromStream()[0] != 0)
        {
            //Read if asset wanted (end if 0)
            //if (client.GetBytesFromStream()[0] == 0)
              //  return;

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

    public IEnumerator SpawnObjectAsServer(string hash, Transform parentTransform = null, TileStream tileStream = null)
    {
        //Used to instantiate objects with a defined parent
        if(parentTransform != null)
        {

            //This is actually probably not necessary? Why does the server need to instantiate objects?
            yield return StartCoroutine(composer.Compose(hash, parentTransform, tileStream));

            yield break;
        }

        //Used to declare dynamic objects as children of the object manager
        GameObject newObjectHolder = Instantiate(dynamicObjectPrefab, transform);

        //Wait frame for instantiation completion.
        yield return null;

        dynamicObjects.Add(hash, newObjectHolder);
        newObjectHolder.GetComponent<ObjectTag>().setObjectHash(hash);

        //Instantiate hash data.
        //This is actually probably not necessary? Why does the server need to instantiate objects?
        yield return StartCoroutine(composer.Compose(hash, dynamicObjectTransform, tileStream));
    }

}
