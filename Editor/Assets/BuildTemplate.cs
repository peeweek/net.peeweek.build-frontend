using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;

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

        if(BuildPath == null)
            BuildPath = "Build/";
    }

    public BuildReport DoBuild()
    {
        if(BuildEnabled)
            return BuildPipeline.BuildPlayer(SceneList.scenePaths, BuildPath + ExecutableName, Profile.Target, BuildOptions.None);
        else
        {
            Debug.LogWarning("Build is disabled");
            return null;
        }
    }

    public void Run()
    {
        Debug.LogWarning("Not implemented yet");
    }
}
