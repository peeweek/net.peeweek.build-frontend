using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;

public class BuildProfile : BuildFrontendAssetBase
{
    [Header("Build Profile")]
    public bool IL2CPP;
    public bool DevPlayer;
    public BuildTarget Target;
}
