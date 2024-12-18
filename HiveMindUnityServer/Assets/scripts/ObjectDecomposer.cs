using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public class ObjectDecomposer : ScriptableObject
{
    static string objectDirectory = "objectDirectory/";
    
    static string tmpTreePath = "tmpTreeFile";
    static string tmpObjectPath = "tmpObjectFile";
    static string tmpComponentPath = "tmpComponentFile";
    static string tmpBytePath = "tmpByteFile";

    int id;

    List<GameObject> objectsByID;

    public string Decompose(GameObject objectToDecompose)
    {
        //Reset in case of re-use of decomposer
        id = 0;
        objectsByID = new List<GameObject>();

        if (!Directory.Exists(objectDirectory))
            Directory.CreateDirectory(objectDirectory);

        FileStream fs = startTmpFile(tmpTreePath);

        //The actual decomposition process:

        //Give each gameobject an ID and add them to the list
        prepareGameObjects(objectToDecompose);

        //Break down the gameobjects and components into their respective files by hashes
        breadthFirstDecompose(objectToDecompose, fs);

        //Clear IDs (this isn't strictly necessary, but it does prevent extra components from being awkwardly left behind when not needed)
        foreach (var iter in objectsByID)
            Destroy(iter.GetComponent<GameObjectID>());

        return fileHashRename(fs, tmpTreePath);
    }

    void prepareGameObjects(GameObject decomposable)
    {

        objectsByID.Add(decomposable);

        //This should always run, but it's here in case someone accidentally adds the GameObjectID script to an object.
        if(decomposable.GetComponent<GameObjectID>() == null)
            decomposable.AddComponent<GameObjectID>();

        decomposable.GetComponent<GameObjectID>().id = id++;

        int numChildren = decomposable.transform.childCount;

        for (int i = 0; i < numChildren; i++)
            prepareGameObjects(decomposable.transform.GetChild(i).gameObject);
    }

    void breadthFirstDecompose(GameObject decomposable, FileStream fsTree)
    {

        FileStream fsObject = startTmpFile(tmpObjectPath);

        Component[] components = decomposable.GetComponents<Component>();

        //Must subtract 1 for GameObjectID component
        Write(components.Length - 1, fsObject);

        //Decompose components
        foreach (Component component in components)
        {

            string componentType = component.GetType().FullName;

            if (componentType == "GameObjectID")
                continue;

            FileStream fsComponent = startTmpFile(tmpComponentPath);

            switch (componentType)
            {
                case "UnityEngine.Transform":

                    Write(componentType, fsComponent);
                    UnityEngine_Transform((Transform)component, fsComponent);
                    break;

                case "UnityEngine.MeshFilter":

                    Write(componentType, fsComponent);
                    UnityEngine_MeshFilter((MeshFilter)component, fsComponent);
                    break;

                case "UnityEngine.MeshRenderer":

                    Write(componentType, fsComponent);
                    UnityEngine_MeshRenderer((MeshRenderer)component, fsComponent);
                    break;

                default:

                    componentType = "UNSUPPORTED";
                    Write(componentType, fsComponent);
                    break;

                    /*
                    fsObject.Close();
                    File.Delete(objectDirectory + tmpComponentPath);
                    throw new Exception("Cannot decompose component of type " + componentType);*/
            }

            Write(componentType, fsComponent);

            Write(fileHashRename(fsComponent, tmpComponentPath), fsObject);
        }

        Write(fileHashRename(fsObject, tmpObjectPath), fsTree);

        int numChildren = decomposable.transform.childCount;
        Write(numChildren, fsTree);

        //Decompose children
        for (int i = 0; i < numChildren; i++)
            breadthFirstDecompose(decomposable.transform.GetChild(i).gameObject, fsTree);
    }

    //Creates a temporary filestream to be closed by fileHashRename
    FileStream startTmpFile(string path)
    {

        if (File.Exists(objectDirectory + path))
            File.Delete(objectDirectory + path);

        return File.Create(objectDirectory + path);
    }

    //Renames files 
    string fileHashRename(FileStream fs, string path)
    {

        fs.Position = 0;

        //Finish file construction
        byte[] hash = SHA256.Create().ComputeHash(fs);
        string hashStr = BitConverter.ToString(hash).Replace("-", "").ToLower();

        fs.Close();

        //This appears to be leaving the old file in place and making a copy...
        if (!File.Exists(objectDirectory + hashStr))
            File.Move(objectDirectory + path, objectDirectory + hashStr);

        return hashStr;

    }



    //Component Decomposer (these decompose supported components of the GameObject):

    /*Transform: 
     * Component type
     * Pos X
     * Pos Y
     * Pos Z
     * Rot W
     * Rot X
     * Rot Y
     * Rot Z
     */
    static void UnityEngine_Transform(Transform component, FileStream fs)
    {

        Write(component.localPosition, fs);
        Write(component.localRotation, fs);
        Write(component.localScale, fs);
    }

    /*MeshFilter: 
     * Component type
     * 0 If no mesh 1 if mesh
     * Mesh(?)
     */
    void UnityEngine_MeshFilter(MeshFilter component, FileStream fsComponent)
    {

        Mesh mesh = component.mesh;

        if(mesh == null)
        {
            Write(0, fsComponent);
            return;
        }

        Write(1, fsComponent);

        Write(DecomposeMesh(mesh), fsComponent);

    }

    /*MeshRenderer: 
     * Component type
     * Number of materials
     * Materials
     */
    void UnityEngine_MeshRenderer(MeshRenderer component, FileStream fs)
    {

        Material[] materials = component.materials;
        Write(materials.Length, fs);

        foreach(Material material in materials)
            Write(DecomposeMaterial(material), fs);
    }



    //Helper Functions
    static void Write(Vector3 vector, FileStream fs)
    {
        Write(vector.x, fs);
        Write(vector.y, fs);
        Write(vector.z, fs);
    }

    static void Write(Quaternion quat, FileStream fs)
    {
        Write(quat.x, fs);
        Write(quat.y, fs);
        Write(quat.z, fs);
        Write(quat.w, fs);
    }

    static void Write(string text, FileStream fs)
    {
        Write(Encoding.ASCII.GetBytes(text), fs);
    }

    static void Write(float value, FileStream fs)
    {
        fs.Write(BitConverter.GetBytes(value), 0, sizeof(float));

    }

    static void Write(int value, FileStream fs)
    {
        fs.Write(BitConverter.GetBytes(value), 0, sizeof(int));

    }

    static void Write(byte[] bytes, FileStream fs)
    {
        fs.Write(BitConverter.GetBytes(bytes.Length));
        fs.Write(bytes);
    }

    /*Decompose Mesh:
     * Number of vertices
     * Vertices
     * Number of triangles
     * Triangles
     * Number of normals
     * Normals
     */
    string DecomposeMesh(Mesh mesh)
    {

        FileStream fsData = startTmpFile(tmpBytePath);

        Vector3[] vertices = mesh.vertices;
        Write(vertices.Length, fsData);

        foreach (Vector3 vertex in vertices)
            Write(vertex, fsData);

        int[] triangles = mesh.triangles;
        Write(triangles.Length, fsData);

        foreach (int triangle in triangles)
            Write(triangle, fsData);

        Vector3[] normals = mesh.normals;
        Write(normals.Length, fsData);

        foreach (Vector3 normal in normals)
            Write(normal, fsData);

        return fileHashRename(fsData, tmpBytePath);
    }

    string DecomposeMaterial(Material material)
    {
        //Need to create standardized trusted shaders.

        FileStream fs = startTmpFile(tmpBytePath);

        Color color = material.color;
        DecomposeColor(color, fs);

        return fileHashRename(fs, tmpBytePath);
    }

    static void DecomposeColor(Color color, FileStream fs)
    {
        Write(color.r, fs);
        Write(color.g, fs);
        Write(color.b, fs);
        Write(color.a, fs);
    }
}
