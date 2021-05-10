using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace BuildFrontend
{

    public class BuildTemplate : BuildFrontendAssetBase
    {
        public bool BuildEnabled
        {
            get { return EditorPrefs.GetBool(preferenceName, true); }
            set { EditorPrefs.SetBool(preferenceName, value); }
        }

        string preferenceName
        {
            get { return $"BuildFrontend.{AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(this))}.Enabled"; }
        }

        [Header("Configuration")]
        public BuildProfile Profile;
        public SceneList SceneList;

        [Header("Output Options")]
        public string BuildPath;
        public string ExecutableName;


        [Header("Build/Run Options")]
        public bool CleanupBeforeBuild = true;
        public string RunWithArguments;
        public BuildProcessor[] processors;

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
                try
                {
                    if(processors != null)
                    {
                        foreach(var processor in processors)
                        {
                            if (processor == null)
                                continue;

                            EditorUtility.DisplayProgressBar("Build Frontend", $"Pre-Processing : {processor.name}", 0.0f);
                            if(!processor.OnPreProcess(this))
                            {
                                throw new BuildProcessorException(processor, this);
                            }
                        }
                    }

                    EditorUtility.DisplayProgressBar("Build Frontend", $"Building player : {name}", 0.0f);

                    BuildOptions options = BuildOptions.None;

                    if (Profile.DevPlayer)
                        options |= BuildOptions.Development;

                    if (CleanupBeforeBuild && System.IO.Directory.Exists(BuildPath))
                    {
                        EditorUtility.DisplayProgressBar("Build Frontend", $"Cleaning up folder : {BuildPath}", 0.05f);
                        System.IO.Directory.Delete(BuildPath, true);
                        System.IO.Directory.CreateDirectory(BuildPath);
                    }

                    report = BuildPipeline.BuildPlayer(SceneList.scenePaths, BuildPath + ExecutableName, Profile.Target, options);
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

                    if (processors != null)
                    {
                        foreach (var processor in processors)
                        {
                            if (processor == null)
                                continue;

                            EditorUtility.DisplayProgressBar("Build Frontend", $"Post-Processing : {processor.name}", 0.0f);
                            if (!processor.OnPostProcess(this))
                            {
                                throw new BuildProcessorException(processor, this);
                            }
                        }
                    }


                }
                catch (Exception e)
                {
                    Debug.LogException(e, this);
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }
            }
            else
            {
                Debug.LogWarning("Build is disabled");
            }

            return report;
        }

        public bool canRunBuild
        {
            get
            {
                return System.IO.File.Exists(Application.dataPath + "/../" + BuildPath + ExecutableName);
            }
        }

        public void RunBuild()
        {

#if UNITY_EDITOR_WIN
            bool canRun = (Profile != null && (Profile.Target == BuildTarget.StandaloneWindows64 || Profile.Target == BuildTarget.StandaloneWindows));
#elif UNITY_EDITOR_OSX
        bool canRun = (Profile != null && Profile.Target == BuildTarget.StandaloneOSX);
#elif UNITY_EDITOR_LINUX
        bool canRun = (Profile != null && Profile.Target == BuildTarget.StandaloneLinux64);
#else
        bool canRun = false;
#endif
            if (canRun)
            {
                ProcessStartInfo info = new ProcessStartInfo();
                string path = Application.dataPath + "/../" + BuildPath;
                info.FileName = path + ExecutableName;
                info.Arguments = RunWithArguments;
                info.WorkingDirectory = path;
                info.UseShellExecute = false;

                EditorUtility.DisplayProgressBar("Build Frontend", $"Running Player : {info.FileName}", 1.0f);

                Process process = Process.Start(info);
            }
            else
            {
                // TODO : Open File Explorer
                throw new NotImplementedException();
            }



        }
    }

}
