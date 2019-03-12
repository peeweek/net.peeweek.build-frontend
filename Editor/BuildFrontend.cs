using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;

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
        titleContent = Contents.title;
        PopulateAssets();
    }
    
    Dictionary<BuildTemplate, BuildReport> Reports = new Dictionary<BuildTemplate, BuildReport>();
    string reportText = string.Empty;

    private void OnGUI()
    {
        using(new GUILayout.HorizontalScope(GUILayout.Height(88)))
        {
            var rect = GUILayoutUtility.GetRect(88,88, Styles.Icon, GUILayout.Width(88));
            GUI.DrawTexture(rect, Contents.icon);
            using(new GUILayout.VerticalScope())
            {
                GUILayout.Space(8);
                GUILayout.Label(Contents.title, Styles.Title);
                GUILayout.FlexibleSpace();
                DrawProgressBar();
                GUILayout.Space(8);
            }
            using(new GUILayout.VerticalScope(GUILayout.Width(128)))
            {
                GUILayout.Space(12);
                if(GUILayout.Button("Build All", Styles.BuildButton, GUILayout.Height(32)))
                {
                    // Run Build
                    DoAllBuild();
                }

                BuildTemplate template = (Selection.activeObject as BuildTemplate);

                EditorGUI.BeginDisabledGroup(template == null);
                if(GUILayout.Button("Build Selected", EditorStyles.miniButton, GUILayout.Height(16)))
                {
                    var report = template.DoBuild();
                    if(report != null)
                        Reports[template] = report;
                }
                if(GUILayout.Button("Run", EditorStyles.miniButton, GUILayout.Height(16)))
                {
                    template.Run();
                }
                EditorGUI.EndDisabledGroup();
            }
        }
        using(new GUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            if(GUILayout.Button("Refresh", EditorStyles.toolbarButton))
            {
                PopulateAssets();
            }
            GUILayout.FlexibleSpace();
        }

        using (new GUILayout.HorizontalScope())
        {
            DrawTemplateList();
            EditorGUILayout.TextArea(reportText, EditorStyles.label, GUILayout.ExpandHeight(true));
        }
    }

    void DrawProgressBar()
    {
        GUI.backgroundColor = Color.gray;
        using(new GUILayout.HorizontalScope(Styles.progressBarItem, GUILayout.Height(24), GUILayout.ExpandWidth(true)))
        {
            foreach(var cat in m_BuildTemplates)
            {
                foreach(var template in cat.Value)
                {
                    GUI.backgroundColor = new Color(.3f,.3f,.3f,1.0f);
                    GUI.contentColor = Color.white;
                    if(!template.BuildEnabled)
                    {
                        GUI.backgroundColor = new Color(.4f,.4f,.4f,1.0f);
                        GUI.contentColor = new Color(1.0f,1.0f,1.0f, 0.5f);
                    }
                    else if(Reports.ContainsKey(template) && Reports[template] != null)
                    {
                        var report = Reports[template];
                        switch(report.summary.result)
                        {
                            case BuildResult.Succeeded:
                                GUI.backgroundColor = new Color(.15f,.5f,.05f,1.0f);
                                GUI.contentColor = new Color(0.3f,1.0f,0.1f, 1.0f);                                
                                break;
                            case BuildResult.Cancelled:
                            case BuildResult.Unknown:
                            case BuildResult.Failed:
                                GUI.backgroundColor = new Color(.5f,.05f,.15f,1.0f);
                                GUI.contentColor = new Color(1.0f,0.1f,0.3f, 1.0f);                                
                                break;

                        }

                    }
                
                    GUILayout.Label(template.name, Styles.progressBarItem, GUILayout.ExpandHeight(true));
                }
            }
        }
        GUI.contentColor = Color.white;
        GUI.backgroundColor = Color.white;    
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
                        Reports[template] = Report;
                    }
                    Repaint();
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    Vector2 scrollPosition = Vector2.zero;

    void DrawTemplateList()
    {
        using (new GUILayout.ScrollViewScope(scrollPosition, false, true, GUILayout.Width(240)))
        {
            using (new GUILayout.VerticalScope(EditorStyles.label))
            {
                foreach (var catKVP in m_BuildTemplates)
                {
                    EditorGUILayout.LabelField(catKVP.Key == string.Empty? "General": catKVP.Key, EditorStyles.boldLabel);

                    foreach (var template in catKVP.Value)
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Space(16);

                            template.BuildEnabled = GUILayout.Toggle(template.BuildEnabled,GUIContent.none, GUILayout.Width(24));
                            if(GUI.changed)
                            {
                                EditorUtility.SetDirty(template);
                            }

                            if (GUILayout.Button(template.Name != null && template.Name != string.Empty ? template.Name : template.name, template == CurrentTemplate? Styles.SelectedProfile : EditorStyles.label))
                            {
                                if (Reports.ContainsKey(template) && Reports[template] != null)
                                    FormatReportGUI(Reports[template]);
                                else
                                    GUILayout.Label("Build has not been run yet");

                                CurrentTemplate = template;
                                CurrentProfile = CurrentTemplate.Profile;
                                CurrentSceneList = CurrentTemplate.SceneList;
                                Selection.activeObject = template;
                            }
                        }
                    }
                    GUILayout.Space(16);
                }
            }
        }
    }

    void DropDownGUI()
    {
        GUILayout.Label(Contents.template, EditorStyles.toolbarButton);
        if (GUILayout.Button(CurrentTemplate == null ? "(no template)" : CurrentTemplate.name, EditorStyles.toolbarPopup))
            TemplateMenu.ShowAsContext();
        GUILayout.Space(64);

        if (CurrentTemplate != null)
        {
            GUILayout.Label(Contents.profile, EditorStyles.toolbarButton);
            if (GUILayout.Button(CurrentProfile == null ? "(no profile)" : CurrentProfile.name, EditorStyles.toolbarPopup))
                ProfileMenu.ShowAsContext();
            GUILayout.Space(64);

            GUILayout.Label(Contents.sceneList, EditorStyles.toolbarButton);
            if (GUILayout.Button(CurrentSceneList == null ? "(no scenelist)" : CurrentSceneList.name, EditorStyles.toolbarPopup))
                SceneListMenu.ShowAsContext();
        }
    }

    [SerializeField]
    BuildTemplate CurrentTemplate;
    [SerializeField]
    BuildProfile CurrentProfile;
    [SerializeField]
    SceneList CurrentSceneList;

    GenericMenu TemplateMenu;
    GenericMenu ProfileMenu;
    GenericMenu SceneListMenu;

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

        TemplateMenu = new GenericMenu();
        foreach (var templateGUID in buildTemplates)
        {
            string templatePath = AssetDatabase.GUIDToAssetPath(templateGUID);
            BuildTemplate template = (BuildTemplate)AssetDatabase.LoadAssetAtPath(templatePath, typeof(BuildTemplate));
            if (!m_BuildTemplates.ContainsKey(template.Category))
                m_BuildTemplates.Add(template.Category, new List<BuildTemplate>());

            m_BuildTemplates[template.Category].Add(template);

            TemplateMenu.AddItem(new GUIContent(template.MenuEntry), false, MenuSetTemplate, template);
        }

        ProfileMenu = new GenericMenu();
        foreach (var profileGUID in buildProfiles)
        {
            string profilePath = AssetDatabase.GUIDToAssetPath(profileGUID);
            BuildProfile profile = (BuildProfile)AssetDatabase.LoadAssetAtPath(profilePath, typeof(BuildProfile));
            m_BuildProfiles.Add(profile);
            ProfileMenu.AddItem(new GUIContent(profile.MenuEntry), false, MenuSetProfile, profile);
        }

        SceneListMenu = new GenericMenu();
        foreach (var sceneListGUID in sceneLists)
        {
            string sceneListPath = AssetDatabase.GUIDToAssetPath(sceneListGUID);
            SceneList sceneList = (SceneList)AssetDatabase.LoadAssetAtPath(sceneListPath, typeof(SceneList));
            m_SceneLists.Add(sceneList);
            SceneListMenu.AddItem(new GUIContent(sceneList.MenuEntry), false, MenuSetSceneList, sceneList);
        }
    }

    void MenuSetTemplate(object o)
    {
        CurrentTemplate = (BuildTemplate)o;
        CurrentProfile = CurrentTemplate.Profile;
        CurrentSceneList = CurrentTemplate.SceneList;
    }

    void MenuSetProfile(object o)
    {
        CurrentProfile = (BuildProfile)o;
        if(CurrentTemplate != null && !CurrentTemplate.name.EndsWith("*"))
        {
            CurrentTemplate = Instantiate<BuildTemplate>(CurrentTemplate) as BuildTemplate;
            CurrentTemplate.name += "*";
        }

        CurrentTemplate.Profile = CurrentProfile;
    }

    void MenuSetSceneList(object o)
    {
        CurrentSceneList = (SceneList)o;
        if (CurrentTemplate != null && !CurrentTemplate.name.EndsWith("*"))
        {
            CurrentTemplate = Instantiate<BuildTemplate>(CurrentTemplate) as BuildTemplate;
            CurrentTemplate.name += "*";
        }

        CurrentTemplate.SceneList = CurrentSceneList;
    }

    void FormatReportGUI(BuildReport report)
    {

        var summary = report.summary;
        
        GUILayout.Label(summary.result.ToString(), Styles.boldLabelLarge);
        GUILayout.Space(8);
        GUILayout.Label("Total Build Time :" + summary.totalTime);
        EditorGUILayout.IntField("Build Size",(int)summary.totalSize);
        EditorGUILayout.IntField("Errors",(int)summary.totalErrors);
        EditorGUILayout.IntField("Warnings",(int)summary.totalWarnings);
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
            EditorGUI.indentLevel++;
            GUILayout.Label(step.name, EditorStyles.foldout);
            EditorGUI.indentLevel++;
            foreach(var message in step.messages)
            {
                EditorGUILayout.LabelField( message.type + " : " + message.content);
            }
            EditorGUI.indentLevel-= 2;
        }
    }

    static class Styles
    {
        public static GUIStyle BuildButton;
        public static GUIStyle progressBarItem;
        public static GUIStyle SelectedProfile;
        public static GUIStyle Title;
        public static GUIStyle Icon;
        public static GUIStyle boldLabelLarge;

        static Styles()
        {
            BuildButton = new GUIStyle(EditorStyles.miniButton);
            BuildButton.fontSize = 14;

            SelectedProfile = new GUIStyle(EditorStyles.label);
            var pink = EditorGUIUtility.isProSkin? new Color(1.0f, 0.2f, 0.5f, 1.0f) : new Color(1.0f, 0.05f, 0.4f, 1.0f);
            SelectedProfile.active.textColor = pink;
            SelectedProfile.focused.textColor = pink;
            SelectedProfile.hover.textColor = pink;
            SelectedProfile.normal.textColor = pink;

            Title = new GUIStyle(EditorStyles.label);
            Title.fontSize = 18;

            Icon = new GUIStyle(EditorStyles.label);

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
        }
    }
    static class Contents
    {
        public static GUIContent title = new GUIContent("Build Frontend");
        public static GUIContent build = new GUIContent("Build");
        public static GUIContent template = new GUIContent("Template:");
        public static GUIContent profile = new GUIContent("Profile:");
        public static GUIContent sceneList = new GUIContent("Scene List:");
        public static Texture icon;

        static Contents()
        {
            icon = AssetDatabase.LoadAssetAtPath<Texture>("Packages/net.peeweek.build-frontend/Editor/Icons/BuildFrontend.png");
        }

    }
}
