using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

public class CreateAssetBundle
{
    [MenuItem("Assets/Create Asset Bundles")]
    private static void BuildAllAssetBundles()
    {
        string assetBundleDirectoryPath = Application.dataPath+ "/AssetBundles";

        if (Directory.Exists(assetBundleDirectoryPath))
            clearFolder(assetBundleDirectoryPath);

        if (!Directory.Exists(assetBundleDirectoryPath))
            Directory.CreateDirectory(assetBundleDirectoryPath);

        Directory.CreateDirectory(assetBundleDirectoryPath + "/w");
        Directory.CreateDirectory(assetBundleDirectoryPath + "/m");
        Directory.CreateDirectory(assetBundleDirectoryPath + "/l");

        try
        {

            BuildPipeline.BuildAssetBundles(assetBundleDirectoryPath + "/w",
                BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);

            BuildPipeline.BuildAssetBundles(assetBundleDirectoryPath + "/m",
                BuildAssetBundleOptions.None, BuildTarget.StandaloneOSX);

            BuildPipeline.BuildAssetBundles(assetBundleDirectoryPath + "/l",
                BuildAssetBundleOptions.None, BuildTarget.StandaloneLinux64);

        }
        catch(System.Exception e) { 
            Debug.LogWarning(e); 
        }
    }

    private static void clearFolder(string FolderName)
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
}
