using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class BuildTemplate : BuildFrontendAssetBase
{
    public bool BuildEnabled;

    [Header("Build Template")]
    public string BuildPath;
    public string ExecutableName;

    public BuildProfile Profile;
    public SceneList SceneList;

    protected override void Awake()
    {
        base.Awake();

        if (BuildPath == null)
            BuildPath = "Build/";
    }

    public BuildReport DoBuild(bool run = false)
    {
        BuildReport report = null;

        if (BuildEnabled)
        {
            report = BuildPipeline.BuildPlayer(SceneList.scenePaths, BuildPath + ExecutableName, Profile.Target, BuildOptions.None);
            if (run)
            {
                if (
                    report.summary.result == BuildResult.Succeeded ||
                    EditorUtility.DisplayDialog("Run Failed Build", "The build has failed or has been canceled, do you want to attempt to run previous build instead?", "Yes", "No")
                  )
                {
                    RunBuild();
                }
            }
        }
        else
        {
            Debug.LogWarning("Build is disabled");
        }

        return report;
    }

    public void RunBuild()
    {
        ProcessStartInfo info = new ProcessStartInfo();
        string path = Application.dataPath + "/../" + BuildPath;
        info.FileName = path + ExecutableName;
        info.WorkingDirectory = path;
        info.UseShellExecute = false;

        Debug.Log($"Running Player : {info.FileName}");

        Process process = Process.Start(info);
        
    }
}
