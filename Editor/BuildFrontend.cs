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
        window.BuildDropdownMenus();
    }

    private void OnEnable()
    {
        titleContent = Contents.title;
        BuildDropdownMenus();
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
                GUILayout.Space(12);
                GUILayout.Label(Contents.title, Styles.Title);
            }
            if(GUILayout.Button("Build", Styles.BuildButton, GUILayout.Width(128), GUILayout.Height(64)))
            {
                // Run Build
                DoAllBuild();
            }
        }


        using (new GUILayout.HorizontalScope())
        {
            DrawTemplateList();
            EditorGUILayout.TextArea(reportText, GUILayout.ExpandHeight(true));
        }
    }

    void DoAllBuild()
    {
        foreach(var kvp_template in m_BuildTemplateActivation)
        {
            if(kvp_template.Value)
            {
                var Report = kvp_template.Key.DoBuild();
                Reports[kvp_template.Key] = Report;
            }
        }

    }

    Vector2 scrollPosition = Vector2.zero;

    void DrawTemplateList()
    {
        using (new GUILayout.ScrollViewScope(scrollPosition, false, true, GUILayout.Width(240)))
        {
            using (new GUILayout.VerticalScope(EditorStyles.textField))
            {
                foreach (var catKVP in m_BuildTemplates)
                {
                    EditorGUILayout.LabelField(catKVP.Key, EditorStyles.boldLabel);

                    foreach (var template in catKVP.Value)
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Space(16);

                            m_BuildTemplateActivation[template] = GUILayout.Toggle(m_BuildTemplateActivation[template],GUIContent.none, GUILayout.Width(24));
                            if(GUILayout.Button(template.Name != null? template.Name : template.name, template == CurrentTemplate? Styles.SelectedProfile : EditorStyles.label))
                            {
                                if (Reports.ContainsKey(template) && Reports[template] != null)
                                    reportText = FormatReport(Reports[template]);
                                else
                                    reportText = "Build has not been run yet.";

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
    Dictionary<BuildTemplate, bool> m_BuildTemplateActivation;

    void BuildDropdownMenus()
    {
        var buildTemplates = AssetDatabase.FindAssets("t:BuildTemplate");
        var buildProfiles = AssetDatabase.FindAssets("t:BuildProfile");
        var sceneLists = AssetDatabase.FindAssets("t:SceneList");

        m_BuildTemplates = new Dictionary<string, List<BuildTemplate>>();
        m_BuildProfiles = new List<BuildProfile>();
        m_SceneLists = new List<SceneList>();
        m_BuildTemplateActivation = new Dictionary<BuildTemplate, bool>();

        TemplateMenu = new GenericMenu();
        foreach (var templateGUID in buildTemplates)
        {
            string templatePath = AssetDatabase.GUIDToAssetPath(templateGUID);
            BuildTemplate template = (BuildTemplate)AssetDatabase.LoadAssetAtPath(templatePath, typeof(BuildTemplate));
            if (!m_BuildTemplates.ContainsKey(template.Category))
                m_BuildTemplates.Add(template.Category, new List<BuildTemplate>());

            m_BuildTemplates[template.Category].Add(template);
            m_BuildTemplateActivation.Add(template, true);
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

    string FormatReport(BuildReport report)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        var summary = report.summary;
        
        sb.AppendLine("Build Summary:");
        sb.AppendLine();
        sb.AppendLine("Result :" + summary.result);
        sb.AppendLine("Total Build Time :" + summary.totalTime);
        sb.AppendLine("Build SIze :" + summary.totalSize);
        sb.AppendLine("Errors :" + summary.totalErrors);
        sb.AppendLine("Warnings :" + summary.totalWarnings);
        sb.AppendLine("Output Path :" + summary.outputPath);
        sb.AppendLine(); sb.AppendLine();

        if(report.strippingInfo != null)
        {
            sb.AppendLine("Included Modules:");
            sb.AppendLine();
            var modules = report.strippingInfo.includedModules;
            foreach(var module in modules)
            {
                sb.AppendLine(" * " + module);
            }
            sb.AppendLine(); sb.AppendLine();
        }

        sb.AppendLine("Build Steps:");
        sb.AppendLine();
        var steps = report.steps;
        foreach(var step in steps)
        {
            sb.AppendLine("STEP " + step.name);
            foreach(var message in step.messages)
            {
                sb.AppendLine(message.type.ToString() + " : " + message.content);
            }
        }
        sb.AppendLine(); sb.AppendLine();

        return sb.ToString();
    }

    static class Styles
    {
        public static GUIStyle BuildButton;
        public static GUIStyle SelectedProfile;
        public static GUIStyle Title;
        public static GUIStyle Icon;

        static Styles()
        {
            BuildButton = new GUIStyle(EditorStyles.miniButton);
            BuildButton.fontSize = 14;
            BuildButton.margin = new RectOffset(0,0,12,12);

            SelectedProfile = new GUIStyle(EditorStyles.label);
            SelectedProfile.fontStyle = FontStyle.Bold;

            Title = new GUIStyle(EditorStyles.label);
            Title.fontSize = 18;

            Icon = new GUIStyle(EditorStyles.label);
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
