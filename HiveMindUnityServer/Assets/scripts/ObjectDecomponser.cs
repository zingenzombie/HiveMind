using System;
using System.IO;
using System.Text;
using UnityEngine;

public static class ObjectDecomponser {


    /*File Format (ALL ENTRIES ARE PRECEEDED BY THE LENGTH OF THEIR VALUE IN BYTES):
     * Name
     * Components (Need a defined function to handle every component type, including their dependencies)
     * Children (Breadth First Decomposition)
     */
    public static void BreadthFirstStaticDecompose(GameObject decomposable, FileStream fs)
    {
        Write(decomposable.name, fs);

        //Decompose components
        foreach(Component component in decomposable.GetComponents<Component>())
        {
            string componentType = component.GetType().FullName;
            switch(componentType)
            {
                case "UnityEngine.Transform":
                    UnityEngine_TransformDecomposer((Transform) component, fs);
                    break;

                case "UnityEngine.MeshFilter":
                    UnityEngine_MeshFilter((MeshFilter)component, fs);
                    break;

                default:
                    throw new Exception("Cannot handle component of type " + componentType);
            }
        }

        //Decompose children
        int numChildren = decomposable.transform.childCount;

        for(int i = 0; i < numChildren; i++)
        {
            BreadthFirstStaticDecompose(decomposable.transform.GetChild(i).gameObject, fs);
        }
    }



    //Component Decomposers:
    static void UnityEngine_TransformDecomposer(Transform component, FileStream fs)
    {
        Write(component.GetType().FullName, fs);

        Write(component.position.x, fs);
        Write(component.position.y, fs);
        Write(component.position.z, fs);

        Write(component.rotation.w, fs);
        Write(component.rotation.x, fs);
        Write(component.rotation.y, fs);
        Write(component.rotation.z, fs);
    }
    static void UnityEngine_MeshFilter(MeshFilter component, FileStream fs)
    {
        Mesh mesh = component.mesh;

    }



    //Helper Functions
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
}
