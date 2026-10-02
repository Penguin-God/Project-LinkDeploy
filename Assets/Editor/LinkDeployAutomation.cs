using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// File-based local build hook also works when the user already has the project open.
// Nothing runs without an explicit request file created in this project.
[InitializeOnLoad]
public static class LinkDeployAutomation
{
    const string RequestPath="Tools/editor-command.txt";
    static double nextCheck;
    static LinkDeployAutomation(){EditorApplication.update+=CheckRequest;}
    static void CheckRequest()
    {
        if(EditorApplication.timeSinceStartup<nextCheck||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        nextCheck=EditorApplication.timeSinceStartup+1;
        if(!File.Exists(RequestPath))return;
        string command=File.ReadAllText(RequestPath).Trim();File.Delete(RequestPath);
        Directory.CreateDirectory("Artifacts");
        try
        {
            switch(command)
            {
                case "build":LinkDeployEditor.BuildWindows();break;
                case "verify":LinkDeployEditor.Verify();break;
                case "prepare":LinkDeployEditor.Prepare();break;
                default:throw new InvalidOperationException("Unknown local editor command.");
            }
            File.WriteAllText("Artifacts/editor-job.txt","SUCCESS "+command+" "+DateTime.UtcNow.ToString("O"));
        }
        catch(Exception exception)
        {
            File.WriteAllText("Artifacts/editor-job.txt","FAILED "+command+"\n"+exception);Debug.LogException(exception);
        }
    }
}
