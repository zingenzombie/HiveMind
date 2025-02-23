using System.Collections.Concurrent;
using System.Threading;
using System;
using UnityEngine;
using System.Collections;

public class OtherClient : MonoBehaviour
{
    public string playerID;
    public string username = "Unnamed";

    Vector3 newPosition;
    Quaternion newRotation;

    GameObject avatarHolder;

    ObjectManager objectController;
    NetworkController networkController;

    private void Start()
    {
        networkController = GameObject.FindWithTag("NetworkController").GetComponent<NetworkController>();
        objectController = GameObject.FindWithTag("ObjectController").GetComponent<ObjectManager>();

        newPosition = transform.position;
        newRotation = transform.rotation;

        avatarHolder = this.transform.GetChild(0).gameObject;
    }

    void Update()
    {

        // Interpolate position
        transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime*5);
        
        // Interpolate rotation
        transform.rotation = Quaternion.Lerp(transform.rotation, newRotation, Time.deltaTime * 5);
    }

    public void ChangeTarget(Vector3 newPosition, Quaternion newRotation)
    {
        this.newPosition = newPosition;
        this.newRotation = newRotation;
    }

    public void UpdateRelativeBonePositions()
    {
        //TODO: For extra limbs of avatar.
    }

    public IEnumerator UpdateAvatar(string avatarHash)
    {

        int numChildren = avatarHolder.transform.childCount;

        for(int i = 0; i < numChildren; i++)
            Destroy(avatarHolder.transform.GetChild(i).gameObject);

        if (objectController.HashExists(avatarHash))
        {
            yield return StartCoroutine(objectController.Compose(avatarHash, this.transform.GetChild(0)));
            yield break;
        }

        CoroutineResult<TileStream> tileStream = new CoroutineResult<TileStream>();

        yield return StartCoroutine(networkController.CreateFetchStream(tileStream));

        Debug.Log("uwu");

        yield return StartCoroutine(objectController.Compose(avatarHash, this.transform.GetChild(0), tileStream.Value));

    }
}
