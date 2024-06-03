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

        if(!Directory.Exists(assetBundleDirectoryPath))
            Directory.CreateDirectory(assetBundleDirectoryPath);

        try
        {

            BuildPipeline.BuildAssetBundles(assetBundleDirectoryPath,
                BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);

        }catch(System.Exception e) { 
            Debug.LogWarning(e); 
        }

    }
}
