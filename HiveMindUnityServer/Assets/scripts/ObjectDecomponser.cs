using System;
using System.IO;
using System.Text;
using UnityEngine;

public static class ObjectDecomponser {

    /* Welcome to the ObjectDecomposer class.
     * 
     * This class breaks down a GameObject, including its children and supported components,
     * into a file, which can be composed by ObjectComposer into a perfect replica of the original object.
     * 
     * If you wish to add a new component, simply add its type to the Decompose components switch statement
     * and define a decomposer for it. Don't forget to mirror your handler on the ObjectComposer.cs script!
     */



    /*File Format (ALL ENTRIES ARE PRECEEDED BY THE BYTE LENGTH OF THEIR VALUE AS INTS):
     * Name
     * Number of components
     * Number of children
     * Components (Need a defined function to handle every component type, including their dependencies)
     * Children (Breadth First Decomposition)
     */

    public static void BreadthFirstStaticDecompose(GameObject decomposable, FileStream fs)
    {

        Write(decomposable.name, fs);

        Component[] components = decomposable.GetComponents<Component>();
        Write(components.Length, fs);

        int numChildren = decomposable.transform.childCount;
        Write(numChildren, fs);

        //Decompose components
        foreach (Component component in components)
        {
            string componentType = component.GetType().FullName;
            switch(componentType)
            {
                case "UnityEngine.Transform":
                    UnityEngine_Transform((Transform) component, fs);
                    break;

                case "UnityEngine.MeshFilter":
                    UnityEngine_MeshFilter((MeshFilter) component, fs);
                    break;

                case "UnityEngine.MeshRenderer":
                    UnityEngine_MeshRenderer((MeshRenderer) component, fs);
                    break;

                default:

                    //TEMPORARY
                    return;

                    throw new Exception("Cannot decompose component of type " + componentType);
            }
        }

        //Decompose children
        for(int i = 0; i < numChildren; i++)
            BreadthFirstStaticDecompose(decomposable.transform.GetChild(i).gameObject, fs);
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
        Write("UnityEngine.Transform", fs);

        Write(component.localPosition, fs);
        Write(component.localRotation, fs);
        Write(component.localScale, fs);
    }

    /*MeshFilter: 
     * Component type
     * 0 If no mesh 1 if mesh
     * Mesh(?)
     */
    static void UnityEngine_MeshFilter(MeshFilter component, FileStream fs)
    {
        Write("UnityEngine.MeshFilter", fs);

        Mesh mesh = component.mesh;

        if(mesh == null)
        {
            Write(0, fs);
            return;
        }

        Write(1, fs);

        DecomposeMesh(mesh, fs);

    }

    /*MeshRenderer: 
     * Component type
     * Number of materials
     * Materials
     */
    static void UnityEngine_MeshRenderer(MeshRenderer component, FileStream fs)
    {
        Write("UnityEngine.MeshRenderer", fs);

        Material[] materials = component.materials;
        Write(materials.Length, fs);

        foreach(Material material in materials)
            DecomposeMaterial(material, fs);
        
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
        Write(BitConverter.GetBytes(value), fs);
    }

    static void Write(int value, FileStream fs)
    {
        Write(BitConverter.GetBytes(value), fs);
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
    static void DecomposeMesh(Mesh mesh, FileStream fs)
    {

        Vector3[] vertices = mesh.vertices;
        Write(vertices.Length, fs);

        foreach (Vector3 vertex in vertices)
            Write(vertex, fs);

        int[] triangles = mesh.triangles;
        Write(triangles.Length, fs);

        foreach (int triangle in triangles)
            Write(triangle, fs);

        Vector3[] normals = mesh.normals;
        Write(normals.Length, fs);

        foreach (Vector3 normal in normals)
            Write(normal, fs);

    }

    static void DecomposeMaterial(Material material, FileStream fs)
    {
        //Need to create standardized trusted shaders.

        Color color = material.color;
        DecomposeColor(color, fs);
    }

    static void DecomposeColor(Color color, FileStream fs)
    {
        Write(color.r, fs);
        Write(color.g, fs);
        Write(color.b, fs);
        Write(color.a, fs);
    }
}
