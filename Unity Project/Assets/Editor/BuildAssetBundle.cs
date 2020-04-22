using UnityEngine;
using UnityEditor;
using System.IO;

public class AssetBundles : EditorWindow
{
    // Add menu item
    [MenuItem("KC Toolkit/Build Asset Bundle")]
    static void Init()
    {
        EditorWindow window = EditorWindow.CreateInstance<AssetBundles>();
        window.Show();
    }

    static void BuildMapABs()
    {
        var win32Path = Path.Combine(Application.dataPath, "Workspace/win32");
        if (!Directory.Exists(win32Path))
            Directory.CreateDirectory(win32Path);
        BuildPipeline.BuildAssetBundles(win32Path, BuildAssetBundleOptions.AppendHashToAssetBundleName, BuildTarget.StandaloneWindows);

        var win64Path = Path.Combine(Application.dataPath, "Workspace/win64");
        if (!Directory.Exists(win64Path))
            Directory.CreateDirectory(win64Path);
        BuildPipeline.BuildAssetBundles(win64Path, BuildAssetBundleOptions.AppendHashToAssetBundleName, BuildTarget.StandaloneWindows64);

        var osxPath = Path.Combine(Application.dataPath, "Workspace/osx");
        if (!Directory.Exists(osxPath))
            Directory.CreateDirectory(osxPath);
        BuildPipeline.BuildAssetBundles(osxPath, BuildAssetBundleOptions.AppendHashToAssetBundleName, BuildTarget.StandaloneOSX);

        var linuxPath = Path.Combine(Application.dataPath, "Workspace/linux");
        if (!Directory.Exists(linuxPath))
            Directory.CreateDirectory(linuxPath);
        BuildPipeline.BuildAssetBundles(linuxPath, BuildAssetBundleOptions.AppendHashToAssetBundleName, BuildTarget.StandaloneLinuxUniversal);
    }

    //Example of using a custom asset bundle definition
    static void CustomAssetBundles()
    {
        // Create the array of bundle build details.
        //AssetBundleBuild[] buildMap = new AssetBundleBuild[1];

        //string[] hats = new string[16];
        //hats[0] = "Assets/Workspace/Hats/BrownHat.prefab";
        //hats[1] = "Assets/Workspace/Hats/BrownNewsboyHat.prefab";
        //hats[2] = "Assets/Workspace/Hats/FarmerGreenHat.prefab";
        //hats[3] = "Assets/Workspace/Hats/FarmerPlainHat.prefab";
        //hats[4] = "Assets/Workspace/Hats/GrayNewsboyHat.prefab";
        //hats[5] = "Assets/Workspace/Hats/PurpleLadyHat.prefab";
        //hats[6] = "Assets/Workspace/Hats/TopHat.prefab";
        //hats[7] = "Assets/Workspace/Hats/WhitePinkLadyHat.prefab";

        //hats[8] = "Assets/Workspace/Hats/HatBlack.material";
        //hats[9] = "Assets/Workspace/Hats/HatBrown.material";
        //hats[10] = "Assets/Workspace/Hats/HatGray.material";
        //hats[11] = "Assets/Workspace/Hats/HatGreen.material";
        //hats[12] = "Assets/Workspace/Hats/HatPink.material";
        //hats[13] = "Assets/Workspace/Hats/HatPurple.material";
        //hats[14] = "Assets/Workspace/Hats/HatStraw.material";
        //hats[15] = "Assets/Workspace/Hats/HatWhite.material";

        //buildMap[0].assetNames = hats;

        //buildMap[0].assetBundleName = "hats_windows32";
        //BuildPipeline.BuildAssetBundles("Assets/Workspace", buildMap, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);

        //buildMap[0].assetBundleName = "hats_windows64";
        //BuildPipeline.BuildAssetBundles("Assets/Workspace", buildMap, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);

        //buildMap[0].assetBundleName = "hats_osx";
        //BuildPipeline.BuildAssetBundles("Assets/Workspace", buildMap, BuildAssetBundleOptions.None, BuildTarget.StandaloneOSX);

        //buildMap[0].assetBundleName = "hats_linux";
        //BuildPipeline.BuildAssetBundles("Assets/Workspace", buildMap, BuildAssetBundleOptions.None, BuildTarget.StandaloneLinuxUniversal);
    }

    //string bundleName;
    Rect buttonRect;
    void OnGUI()
    {
        {
            GUILayout.Label("Asset Bundle Build Window", EditorStyles.boldLabel);
            if (GUILayout.Button("Learn about configuring asset bundles", GUILayout.Width(300)))
            {
                Application.OpenURL(@"https://docs.unity3d.com/2018.2/Documentation/Manual/AssetBundles-Workflow.html");
            }
            GUILayout.Space(50);

            GUILayout.Label("Build asset bundles as defined in the editor:", EditorStyles.wordWrappedLabel);
            //bundleName = GUILayout.TextField(bundleName);
            if (GUILayout.Button("Build Asset Bundles", GUILayout.Width(300)))
            {
                BuildMapABs();
            }
            //GUILayout.Space(40);
            //GUILayout.Label("If you want more control over your asset bundles, you can define the function CustomAssetBundles in ToolkitTools/BuildAssetBundle.cs. Take a peak at that function to see how. The button below calls that function:", EditorStyles.wordWrappedLabel);
            //if (GUILayout.Button("Build Custom Asset Bundles", GUILayout.Width(300)))
            //{
            //    CustomAssetBundles();
            //}
            if (Event.current.type == EventType.Repaint) buttonRect = GUILayoutUtility.GetLastRect();
        }
    }
}

