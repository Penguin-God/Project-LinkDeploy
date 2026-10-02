using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class LinkDeployEditor
{
    [MenuItem("Link Deploy/Prepare project")]
    public static void Prepare()
    {
        const string catalogPath="Assets/Resources/LinkDeployCatalog.asset";
        if(AssetDatabase.LoadAssetAtPath<LinkDeployCatalog>(catalogPath)==null)
        {
            var catalog=ScriptableObject.CreateInstance<LinkDeployCatalog>();catalog.ApplyDocumentDefaults();
            AssetDatabase.CreateAsset(catalog,catalogPath);
        }
        if(!Directory.Exists("Assets/TextMesh Pro/Resources"))
        {
            var packageInfo=UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.ugui");
            var package=packageInfo==null?null:Path.Combine(packageInfo.resolvedPath,"Package Resources/TMP Essential Resources.unitypackage");
            if(package!=null&&File.Exists(package))AssetDatabase.ImportPackage(package,false);
        }
        foreach(var name in new[]{"FoundryAtlas","ForestFortress"})
        {
            string path=$"Assets/Resources/Art/{name}.png";
            if(AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.textureType=TextureImporterType.Default;
                importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
                importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.maxTextureSize=2048;importer.SaveAndReimport();
            }
        }
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Resources/Font/MaplestoryBold.asset");
        var shader=Shader.Find("TextMeshPro/Distance Field");
        if(font!=null&&shader!=null){font.material.shader=shader;EditorUtility.SetDirty(font.material);EditorUtility.SetDirty(font);}
        PlayerSettings.companyName="LinkDeploy";PlayerSettings.productName="Link Deploy";
        PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;
        PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;
        PlayerSettings.runInBackground=true;
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        Debug.Log("LINK_DEPLOY_PREPARED");
    }

    [MenuItem("Link Deploy/Verify gameplay")]
    public static void Verify()
    {
        Prepare();GameVerification.Run();
    }

    [MenuItem("Link Deploy/Build Windows game")]
    public static void BuildWindows()
    {
        Prepare();GameVerification.Run();
        if(Resources.Load<TMP_Settings>("TMP Settings")==null)throw new Exception("TextMesh Pro Essential Resources must finish importing before building. Run Prepare project, then build again.");
        Directory.CreateDirectory("Builds/Windows");
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes=new[]{"Assets/2_Scenes/SampleScene.unity"},
            locationPathName="Builds/Windows/LinkDeploy.exe",
            target=BuildTarget.StandaloneWindows64,options=BuildOptions.None
        });
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Windows build failed: "+report.summary.result);
        Debug.Log("LINK_DEPLOY_BUILD_SUCCESS "+report.summary.totalSize);
    }
}
