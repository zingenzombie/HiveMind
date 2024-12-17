using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

public class ObjectComposer : ScriptableObject
{
    static string objectDirectory = "objectDirectory/";

    List<GameObject> objectsByID;

    int id;
    TileStream tileStream;

    public GameObject Compose(string objHash, TileStream tileStream = null)
    {
        //Reset in case of re-use of decomposer
        id = 0;
        objectsByID = new List<GameObject>();
        GameObject returnObject = new GameObject();
        this.tileStream = tileStream;

        FileStream fs = openByHash(objHash);

        //Compose the object
        BreadthFirstCompose(returnObject, fs);

        //Replace placeholder references
        //TODO

        //Destroy objectsByID
        destryoObjectsByID();

        return returnObject;
    }

    void BreadthFirstCompose(GameObject parent, FileStream fsTree)
    {
        GameObject placeholder = new GameObject();
        placeholder.AddComponent<GameObjectID>();
        placeholder.GetComponent<GameObjectID>().id = id++;

        objectsByID.Add(placeholder);

        GameObject thisObject = new GameObject();
        thisObject.transform.parent = parent.transform;

        FileStream fsObject = openByHash(ReadString(fsTree));

        int numChildren = ReadInt(fsTree);

        int numComponents = ReadInt(fsObject);

        string componentType;
        for (int i = 0; i < numComponents; i++)
        {

            FileStream fsComponent = openByHash(ReadString(fsObject));

            componentType = ReadString(fsComponent);

            switch (componentType)
            {
                case "UnityEngine.Transform":
                    UnityEngine_Transform(thisObject, fsComponent);
                    break;

                case "UnityEngine.MeshFilter":
                    UnityEngine_MeshFilter(thisObject, fsComponent);
                    break;

                case "UnityEngine.MeshRenderer":
                    UnityEngine_MeshRenderer(thisObject, fsComponent);
                    break;

                default:
                    fsComponent.Close();
                    fsObject.Close();
                    fsTree.Close();
                    throw new Exception("Cannot compose component of type " + componentType + ". Aborting object composition.");
            }

            fsComponent.Close();
        }

        fsObject.Close();

        for (int i = 0; i < numChildren; i++)
            BreadthFirstCompose(thisObject, fsTree);
    }

    static FileStream openByHash(string hash)
    {
        try
        {
            if(!File.Exists(objectDirectory + hash))
            {
                //request file
                
                
            }

            FileStream fs = File.Open(objectDirectory + hash, FileMode.Open);

            fs.Position = 0;

            //Finish file construction
            byte[] hashCheck = SHA256.Create().ComputeHash(fs);
            string hashStr = BitConverter.ToString(hashCheck).Replace("-", "").ToLower();

            if (hashStr != hash)
            {
                File.Delete(objectDirectory + hash);
                throw new Exception();
            }

            fs.Position = 0;

            return fs;
        }
        catch (Exception)
        {
            throw new Exception("A file with the hash " + hash + " could not be found.");
        }
    }

    void destryoObjectsByID()
    {
        for (int i = 0; i < objectsByID.Count; i++)
            Destroy(objectsByID[i]);
    }

    public static GameObject BreadthFirstStaticCompose(GameObject parent, FileStream fs)
    {

        GameObject thisObject = new GameObject();
        thisObject.transform.SetParent(parent.transform);

        thisObject.name = ReadString(fs);

        int numComponents = ReadInt(fs);

        int numChildren = ReadInt(fs);

        String componentType;
        for(int i = 0; i < numComponents; i++)
        {
            componentType = ReadString(fs);

            switch (componentType)
            {
                case "UnityEngine.Transform":
                    UnityEngine_Transform(thisObject, fs);
                    break;

                case "UnityEngine.MeshFilter":
                    UnityEngine_MeshFilter(thisObject, fs);
                    break;

                case "UnityEngine.MeshRenderer":
                    UnityEngine_MeshRenderer(thisObject, fs);
                    break;

                default:

                    throw new Exception("Cannot compose component of type " + componentType);
            }
        }

        for(int i = 0; i < numChildren; i++)
            BreadthFirstStaticCompose(thisObject, fs);

        return thisObject;

    }



    //Component Composers (these generate the appropriate components for the GameObject):

    /*Transform:
     * Pos X
     * Pos Y
     * Pos Z
     * Rot W
     * Rot X
     * Rot Y
     * Rot Z
     */
    static void UnityEngine_Transform(GameObject thisObject, FileStream fs)
    {
        thisObject.transform.SetLocalPositionAndRotation(ReadVector3(fs), ReadQuaternion(fs));
        thisObject.transform.localScale = ReadVector3(fs);
    }

    /*MeshFilter: 
     * 0 If no mesh 1 if mesh
     * Mesh(?)
     */
    static void UnityEngine_MeshFilter(GameObject thisObject, FileStream fsComponent)
    {
        MeshFilter meshFilter = thisObject.AddComponent<MeshFilter>();

        if (ReadInt(fsComponent) == 0)
            return;

        FileStream fsData = openByHash(ReadString(fsComponent));

        meshFilter.mesh = ComposeMesh(fsData);

        fsData.Close();
    }

    /*MeshRenderer: 
     * Number of materials
     * Materials
     */
    static void UnityEngine_MeshRenderer(GameObject thisObject, FileStream fs)
    {
        MeshRenderer meshRenderer = thisObject.AddComponent<MeshRenderer>();

        int numMaterials = ReadInt(fs);
        meshRenderer.materials = new Material[numMaterials];

        List<Material> newMaterials = new List<Material>();

        for (int i = 0; i < numMaterials; i++)
            newMaterials.Add(ComposeMaterial(openByHash(ReadString(fs))));

        meshRenderer.SetMaterials(newMaterials);
    }


    //Helper Functions
    static Vector3 ReadVector3(FileStream fs)
    {
        return new Vector3(ReadFloat(fs), ReadFloat(fs), ReadFloat(fs));
    }

    static Quaternion ReadQuaternion(FileStream fs)
    {
        return new Quaternion(ReadFloat(fs), ReadFloat(fs), ReadFloat(fs), ReadFloat(fs));
    }

    static string ReadString(FileStream fs)
    {
        return System.Text.Encoding.ASCII.GetString(ReadBytes(fs));
    }

    static int ReadInt(FileStream fs)
    {

        byte[] bytes = new byte[4];
        fs.Read(bytes, 0, bytes.Length);

        return BitConverter.ToInt32(bytes);
    }

    static float ReadFloat(FileStream fs)
    {
        byte[] bytes = new byte[4];
        fs.Read(bytes, 0, bytes.Length);

        return BitConverter.ToSingle(bytes);
    }

    static byte[] ReadBytes(FileStream fs)
    {
        byte[] numBytesArr = new byte[4];

        fs.Read(numBytesArr, 0, 4);
        int numBytes = BitConverter.ToInt32(numBytesArr);

        byte[] bytes = new byte[numBytes];
        fs.Read(bytes, 0, numBytes);

        return bytes;
    }

    /*Decompose Mesh:
     * Number of vertices
     * Vertices
     * Number of triangles
     * Triangles
     * Number of normals
     * Normals
     */
    static Mesh ComposeMesh(FileStream fs)
    {
        Mesh mesh = new Mesh();

        int length = ReadInt(fs);
        Vector3[] vertices = new Vector3[length];

        for (int i = 0; i < length; i++)
            vertices[i] = ReadVector3(fs);

        mesh.vertices = vertices;



        length = ReadInt(fs);
        int[] triangles = new int[length];

        for (int i = 0; i < length; i++)
            triangles[i] = ReadInt(fs);

        mesh.triangles = triangles;



        length = ReadInt(fs);
        Vector3[] normals = new Vector3[length];

        for (int i = 0; i < length; i++)
            normals[i] = ReadVector3(fs);

        mesh.normals = normals;

        return mesh;

    }

    static Material ComposeMaterial(FileStream fs)
    {

        //Need to create standardized trusted shaders.
        Material material = new Material(ComposeShader(fs));

        material.color = ComposeColor(fs);

        fs.Close();

        return material;
    }

    static Color ComposeColor(FileStream fs)
    {
        return new Color(ReadFloat(fs), ReadFloat(fs), ReadFloat(fs), ReadFloat(fs));
    }

    static Shader ComposeShader(FileStream fs)
    {
        return Shader.Find("Standard");
    }
}
