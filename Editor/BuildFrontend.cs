using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System;

public class BuildFrontend : EditorWindow
{
    public const int CreateAssetMenuPriority = 5801;
    public const int WindowMenuPriority = 203;

    [MenuItem("File/Build Frontend %&B", priority = WindowMenuPriority)]

    static void OpenWindow()
    {
        var window = GetWindow<BuildFrontend>();
        window.PopulateAssets();
    }

    private void OnEnable()
    {
        titleContent = Contents.windowTitle;
        PopulateAssets();
    }
    
    [SerializeField]
    Dictionary<BuildTemplate, BuildReport> m_Reports = new Dictionary<BuildTemplate, BuildReport>();
    string reportText = string.Empty;

    private void OnGUI()
    {
        System.Action nextAction = null;

        int iconSize = 48;

        using (new GUILayout.HorizontalScope(GUILayout.Height(iconSize)))
        {
            var rect = GUILayoutUtility.GetRect(iconSize, iconSize, Styles.Icon, GUILayout.Width(iconSize));
            GUI.DrawTexture(rect, Contents.icon);
            using(new GUILayout.VerticalScope())
            {
                GUILayout.Space(8);
                GUILayout.Label(Contents.title, Styles.Title);
                GUILayout.FlexibleSpace();
                GUILayout.Space(8);
            }
            GUILayout.Space(8);

            using (new GUILayout.VerticalScope(GUILayout.Width(120)))
            {
                GUILayout.Space(8);
                if(GUILayout.Button("Build All", Styles.BuildButton, GUILayout.Height(32)))
                {
                    // Run Build
                    nextAction = DoAllBuild;
                }
                GUILayout.Space(8);
            }
            GUILayout.Space(8);
        }

        var r = GUILayoutUtility.GetRect(-1, 1, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(r, Color.black);

        using (new GUILayout.HorizontalScope())
        {
            DrawTemplateList();
            if(nextAction != null) // If already Building All...
                DrawReport();
            else
                nextAction = DrawReport();
        }

        if (nextAction != null)
        {
            var selected = Selection.activeObject;
            nextAction.Invoke();
            Selection.activeObject = selected;
        }

    }

    Action DrawReport()
    {
        if (selectedTemplate == null)
        {
            using(new GUILayout.VerticalScope())
            {
                GUILayout.FlexibleSpace();
                using(new GUILayout.HorizontalScope(GUILayout.Height(32)))
                {
                    GUILayout.Space(180);
                    EditorGUILayout.HelpBox("Please select a build template in the left pane list", MessageType.Info);
                    GUILayout.Space(180);
                }
                GUILayout.FlexibleSpace();
            }
            return null;
        }

        BuildReport report = null;
        if(m_Reports.ContainsKey(selectedTemplate))
            report = m_Reports[selectedTemplate];
        reportScroll = EditorGUILayout.BeginScrollView(reportScroll, Styles.scrollView);

        var nextAction = FormatHeaderGUI(selectedTemplate, report);

        if (report != null)
            FormatReportGUI(selectedTemplate, report);
        else
            EditorGUILayout.HelpBox("No Build report has been generated yet, please build this template first", MessageType.Info);

        EditorGUILayout.EndScrollView();

        return nextAction;
    }

    void DoAllBuild()
    {
        try
        {
            foreach(var cat in m_BuildTemplates)
            {
                foreach(var template in cat.Value)
                {
                    EditorUtility.DisplayProgressBar("Build Frontend", $"Building {template.name} ...", 1.0f);
                    if(template.BuildEnabled)
                    {
                        var Report = template.DoBuild();
                        Repaint();
                    }
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    Vector2 templateScroll = Vector2.zero;
    Vector2 reportScroll = Vector2.zero;

    void DrawTemplateList()
    {
        templateScroll = GUILayout.BeginScrollView(templateScroll, false, true, GUILayout.Width(240));
        
        using (new GUILayout.VerticalScope(EditorStyles.label))
        {
            foreach (var catKVP in m_BuildTemplates)
            {
                EditorGUILayout.LabelField(catKVP.Key == string.Empty? "General": catKVP.Key, EditorStyles.boldLabel);

                foreach (var template in catKVP.Value)
                {
                    // Draw Selected background box
                    if(template == selectedTemplate)
                    {
                        Rect r = GUILayoutUtility.GetLastRect();
                        Vector2 pos = r.position;
                        pos.y += 18;
                        r.position = pos;
                        r.height += 2;
                        float gray = EditorGUIUtility.isProSkin? 1: 0;
                        EditorGUI.DrawRect(r, new Color(gray, gray, gray, 0.1f));
                    }

                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Space(16);

                        EditorGUI.BeginChangeCheck();
                        var enabled = GUILayout.Toggle(template.BuildEnabled,GUIContent.none, GUILayout.Width(24));
                        if(EditorGUI.EndChangeCheck())
                        {
                            template.BuildEnabled = enabled;
                            EditorUtility.SetDirty(template);
                        }

                        if (GUILayout.Button(template.Name != null && template.Name != string.Empty ? template.Name : template.name, template == selectedTemplate? Styles.SelectedProfile : EditorStyles.label))
                        {
                            selectedTemplate = template;
                            Selection.activeObject = selectedTemplate;
                        }
                    }
                }
                GUILayout.Space(16);
            }
        }
        EditorGUILayout.EndScrollView();
    }

    [SerializeField]
    BuildTemplate selectedTemplate;
    BuildProfile currentProfile => selectedTemplate?.Profile;
    SceneList currentSceneList => selectedTemplate?.SceneList;

    Dictionary<string, List<BuildTemplate>> m_BuildTemplates;
    List<BuildProfile> m_BuildProfiles;
    List<SceneList> m_SceneLists;

    void PopulateAssets()
    {
        var buildTemplates = AssetDatabase.FindAssets("t:BuildTemplate");
        var buildProfiles = AssetDatabase.FindAssets("t:BuildProfile");
        var sceneLists = AssetDatabase.FindAssets("t:SceneList");

        m_BuildTemplates = new Dictionary<string, List<BuildTemplate>>();
        m_BuildProfiles = new List<BuildProfile>();
        m_SceneLists = new List<SceneList>();


        foreach (var templateGUID in buildTemplates)
        {
            string templatePath = AssetDatabase.GUIDToAssetPath(templateGUID);
            BuildTemplate template = (BuildTemplate)AssetDatabase.LoadAssetAtPath(templatePath, typeof(BuildTemplate));
            if (!m_BuildTemplates.ContainsKey(template.Category))
                m_BuildTemplates.Add(template.Category, new List<BuildTemplate>());

            m_BuildTemplates[template.Category].Add(template);
        }


        foreach (var profileGUID in buildProfiles)
        {
            string profilePath = AssetDatabase.GUIDToAssetPath(profileGUID);
            BuildProfile profile = (BuildProfile)AssetDatabase.LoadAssetAtPath(profilePath, typeof(BuildProfile));
            m_BuildProfiles.Add(profile);
        }

        foreach (var sceneListGUID in sceneLists)
        {
            string sceneListPath = AssetDatabase.GUIDToAssetPath(sceneListGUID);
            SceneList sceneList = (SceneList)AssetDatabase.LoadAssetAtPath(sceneListPath, typeof(SceneList));
            m_SceneLists.Add(sceneList);
        }
    }

    string FormatSize(ulong byteSize)
    {
        double size = byteSize;
        if (size < 1024) // Bytes
        {
            return $"{size.ToString("F2")} bytes";
        }
        else if (size < 1024 * 1024) // KiloBytes
        {
            return $"{(size / 1024).ToString("F2")} KiB ({byteSize} bytes)";
        }
        else if (size < 1024 * 1024 * 1024) // Megabytes
        {
            return $"{(size / (1024*1024)).ToString("F2")} MiB ({byteSize} bytes)";
        }
        else // Gigabytes
        {
            return $"{(size / (1024 * 1024 * 1024)).ToString("F2")} GiB ({byteSize} bytes)";
        }
    }

    Action FormatHeaderGUI(BuildTemplate template, BuildReport report = null)
    {
        Action nextAction = null;

        using (new GUILayout.HorizontalScope())
        {
            using(new GUILayout.VerticalScope())
            {
                GUILayout.Label($"{(template.Name == string.Empty ? template.name : template.Name)} {(template.Category == string.Empty ? "" : $"({template.Category})")}", Styles.boldLabelLarge);

                using (new GUILayout.HorizontalScope())
                {
                    if (report != null)
                    {
                        var summary = report.summary;
                        if (summary.result == BuildResult.Succeeded)
                            GUILayout.Label(Contents.buildSucceeded, GUILayout.Width(32));
                        else if (summary.result != BuildResult.Unknown)
                            GUILayout.Label(Contents.buildFailed, GUILayout.Width(32));

                        GUILayout.Label(summary.result.ToString(), Styles.boldLabelLarge);
                    }
                    else
                    {
                        GUILayout.Label(Contents.buildPending, GUILayout.Width(32));
                        GUILayout.Label("Build not yet started", Styles.boldLabelLarge);
                    }
                }
            }

            GUILayout.FlexibleSpace();


            using (new GUILayout.VerticalScope(GUILayout.Width(120)))
            {
                using (new GUILayout.HorizontalScope())
                {
                    EditorGUI.BeginDisabledGroup(template == null);

                    if (GUILayout.Button("Build", Styles.MiniButtonLeft))
                    {
                        nextAction = () =>
                        {
                            var report = template.DoBuild();
                            if (report != null)
                                m_Reports[template] = report;

                            selectedTemplate = template;
                            Repaint();
                        };
                    }
                    if (GUILayout.Button("+ Run", Styles.MiniButtonRight, GUILayout.Width(48)))
                    {
                        nextAction = () =>
                        {
                            var report = template.DoBuild(true);
                            if (report != null)
                                m_Reports[template] = report;

                            selectedTemplate = template;
                            Repaint();
                        };
                    }
                    EditorGUI.EndDisabledGroup();
                }

                EditorGUI.BeginDisabledGroup(template == null || !template.canRunBuild);
                if (GUILayout.Button("Run Last Build", Styles.MiniButton))
                {
                    nextAction = () =>
                    {
                        template.RunBuild();
                        EditorUtility.ClearProgressBar();
                        Repaint();
                    };
                }
                EditorGUI.EndDisabledGroup();
            }

        }

        GUILayout.Space(8);
        var r = GUILayoutUtility.GetRect(-1, 1, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(r, new Color(0, 0, 0, 0.5f));
        GUILayout.Space(16);

        return nextAction;
    }


    void FormatReportGUI(BuildTemplate template, BuildReport report)
    {
        var summary = report.summary;

        GUILayout.Space(8);
        GUILayout.Label("Total Build Time :" + summary.totalTime);
        EditorGUILayout.LabelField($"Build Size : {FormatSize(summary.totalSize)} ");

        if(summary.totalErrors > 0)
        {
            GUILayout.Label(new GUIContent($"{(int)summary.totalErrors} Errors", Contents.errorIconSmall.image));
        }

        if(summary.totalWarnings > 0)
        {
            GUILayout.Label(new GUIContent($"{(int)summary.totalWarnings} Warnings", Contents.warnIconSmall.image));
        }

        EditorGUILayout.TextField("Output Path", summary.outputPath);
        
        if(report.strippingInfo != null)
        {
            GUILayout.Space(8);
            
            GUILayout.Label("Included Modules", EditorStyles.boldLabel);
            var modules = report.strippingInfo.includedModules;
            foreach(var module in modules)
            {
                EditorGUILayout.LabelField(module, EditorStyles.foldout);
            }
        }

        GUILayout.Space(8);
        GUILayout.Label("Build Steps", EditorStyles.boldLabel);
        var steps = report.steps;
        foreach(var step in steps)
        {
            string prefName = $"BuildFrontend.Foldout'{step.name}'";

            using (new GUILayout.HorizontalScope())
            {
                if (step.messages.Any(o => o.type == LogType.Error || o.type == LogType.Assert || o.type == LogType.Exception))
                    GUILayout.Label(Contents.errorIconSmall, Styles.Icon,  GUILayout.Width(16));
                else if (step.messages.Any(o => o.type == LogType.Warning))
                    GUILayout.Label(Contents.warnIconSmall, Styles.Icon, GUILayout.Width(16));
                else
                    GUILayout.Label(Contents.successIcon, Styles.Icon, GUILayout.Width(16));

                if (step.messages.Length > 0)
                {
                    bool pref = EditorPrefs.GetBool(prefName, false);

                    bool newPref = EditorGUILayout.Foldout(pref, step.name);

                    if (GUI.changed && pref != newPref)
                        EditorPrefs.SetBool(prefName, newPref);
                }
                else
                {
                    EditorGUILayout.LabelField(step.name);
                }

            }

            EditorGUI.indentLevel++;
            if (EditorPrefs.GetBool(prefName, false))
            {
                foreach (var message in step.messages)
                {
                    MessageType type = MessageType.Error;
                    if (message.type == LogType.Log)
                        type = MessageType.Info;
                    else if (message.type == LogType.Warning)
                        type = MessageType.Warning;

                    EditorGUILayout.HelpBox(message.content, type, true);
                }
            }
            EditorGUI.indentLevel--;

            GUILayout.Space(2);
        }
        GUILayout.Space(128);
    }

    static class Styles
    {
        public static GUIStyle BuildButton;
        public static GUIStyle MiniButton;
        public static GUIStyle MiniButtonLeft;
        public static GUIStyle MiniButtonRight;
        public static GUIStyle progressBarItem;
        public static GUIStyle SelectedProfile;
        public static GUIStyle Title;
        public static GUIStyle Icon;
        public static GUIStyle boldLabelLarge;
        public static GUIStyle scrollView;

        static Styles()
        {
            BuildButton = new GUIStyle(EditorStyles.miniButton);
            BuildButton.fontSize = 14;
            BuildButton.fontStyle = FontStyle.Bold;
            BuildButton.fixedHeight = 32;

            MiniButton = new GUIStyle(EditorStyles.miniButton);
            MiniButton.fixedHeight = 22;
            MiniButton.fontSize = 12;

            MiniButtonLeft = new GUIStyle(EditorStyles.miniButtonLeft);
            MiniButtonLeft.fixedHeight = 22;
            MiniButtonLeft.fontSize = 12;

            MiniButtonRight = new GUIStyle(EditorStyles.miniButtonRight);
            MiniButtonRight.fixedHeight = 22;
            MiniButtonRight.fontSize = 12;


            SelectedProfile = new GUIStyle(EditorStyles.label);
            var pink = EditorGUIUtility.isProSkin? new Color(1.0f, 0.2f, 0.5f, 1.0f) : new Color(1.0f, 0.05f, 0.4f, 1.0f);
            SelectedProfile.active.textColor = pink;
            SelectedProfile.focused.textColor = pink;
            SelectedProfile.hover.textColor = pink;
            SelectedProfile.normal.textColor = pink;

            Title = new GUIStyle(EditorStyles.label);
            Title.fontSize = 18;

            Icon = new GUIStyle(EditorStyles.label);

            Icon.padding = new RectOffset();
            Icon.margin = new RectOffset();

            progressBarItem = new GUIStyle(EditorStyles.miniLabel);
            progressBarItem.alignment = TextAnchor.MiddleCenter;
            progressBarItem.margin = new RectOffset(0,0,0,0);
            progressBarItem.padding = new RectOffset(0,0,0,0);
            progressBarItem.wordWrap = true;
            progressBarItem.onActive.background = Texture2D.whiteTexture;
            progressBarItem.onFocused.background = Texture2D.whiteTexture;
            progressBarItem.onHover.background = Texture2D.whiteTexture;
            progressBarItem.onNormal.background = Texture2D.whiteTexture;
            progressBarItem.active.background = Texture2D.whiteTexture;
            progressBarItem.focused.background = Texture2D.whiteTexture;
            progressBarItem.hover.background = Texture2D.whiteTexture;
            progressBarItem.normal.background = Texture2D.whiteTexture;
            
            boldLabelLarge = new GUIStyle(EditorStyles.boldLabel);
            boldLabelLarge.fontSize = 16;

            scrollView = new GUIStyle();
            scrollView.padding = new RectOffset(8, 8, 8, 8);

        }
    }
    static class Contents
    {
        public static GUIContent windowTitle; 
        public static GUIContent title; 
        public static GUIContent build = new GUIContent("Build");
        public static GUIContent template = new GUIContent("Template:");
        public static GUIContent profile = new GUIContent("Profile:");
        public static GUIContent sceneList = new GUIContent("Scene List:");
        public static Texture icon;

        public static GUIContent buildSucceeded = EditorGUIUtility.IconContent("Collab.BuildSucceeded");
        public static GUIContent buildFailed = EditorGUIUtility.IconContent("Collab.BuildFailed");
        public static GUIContent buildPending = EditorGUIUtility.IconContent("Collab.Build");

        public static GUIContent successIcon = EditorGUIUtility.IconContent("Collab");
        public static GUIContent failIcon = EditorGUIUtility.IconContent("CollabError");

        public static GUIContent errorIcon = EditorGUIUtility.IconContent("console.erroricon");
        public static GUIContent errorIconSmall = EditorGUIUtility.IconContent("console.erroricon.sml");

        public static GUIContent infoIcon = EditorGUIUtility.IconContent("console.infoicon");
        public static GUIContent infoIconSmall = EditorGUIUtility.IconContent("console.infoicon.sml");

        public static GUIContent warnIcon = EditorGUIUtility.IconContent("console.warnicon");
        public static GUIContent warnIconSmall = EditorGUIUtility.IconContent("console.warnicon.sml");

        static Contents()
        {
            icon = AssetDatabase.LoadAssetAtPath<Texture>("Packages/net.peeweek.build-frontend/Editor/Icons/BuildFrontend.png");
            var titleIcon = AssetDatabase.LoadAssetAtPath<Texture>($"Packages/net.peeweek.build-frontend/Editor/Icons/BuildFrontendTab{(EditorGUIUtility.isProSkin?"":"Personal")}.png");
            windowTitle = new GUIContent("Build Frontend", titleIcon);
            title = new GUIContent("Build Frontend");
        }

    }
}
