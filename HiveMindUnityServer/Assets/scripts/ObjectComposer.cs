using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class ObjectComposer
{

    /* Welcome to the ObjectComposer class.
     * 
     * This class builds up a GameObject, including its children and supported components,
     * from a file, which is formed by the ObjectDecomposer class based off of an original GameObject.
     * 
     * Please visit ObjectDecomposer.cs for information about adding additional component support.
     */

    /*File Format (ALL ENTRIES ARE PRECEEDED BY THE BYTE LENGTH OF THEIR VALUE AS INTS):
     * Name
     * Number of components
     * Number of children
     * Components (Need a defined function to handle every component type, including their dependencies)
     * Children (Breadth First Decomposition)
     */

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
    static void UnityEngine_MeshFilter(GameObject thisObject, FileStream fs)
    {
        MeshFilter meshFilter = thisObject.AddComponent<MeshFilter>();

        if (ReadInt(fs) == 0)
            return;

        meshFilter.mesh = ComposeMesh(fs);
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
            newMaterials.Add(ComposeMaterial(fs));

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
        return BitConverter.ToInt32(ReadBytes(fs));
    }

    static float ReadFloat(FileStream fs)
    {
        return BitConverter.ToSingle(ReadBytes(fs));
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
