using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class UIAutoCreateWindow : EditorWindow
{
    private static UIAutoCreateWindow window;
    private string PrefabPath = UIHelperString.PrefabPath;
    private string ConfigPath = UIHelperString.ConfigPath;
    private string CodePath = UIHelperString.CodePath;
    private string Namespace = "";
    private bool IsSimplyName = false;
    private bool IsWithExtension = false;

    [MenuItem("Tools/UI/自动生成UI预制配置窗口", priority = 1)]
    public static void ShowWindow()
    {
        ShowUIAutoCreateWindow();
    }

    private static void ShowUIAutoCreateWindow()
    {
        window = GetWindow<UIAutoCreateWindow>(true);
        window.Show();
    }

    private void OnFocus()
    {
        UpdateDate();
    }

    private void UpdateDate()
    {
        var config = AssetDatabase.LoadAssetAtPath<UIAutoCreateConfig>(ConfigPath);
        if (config != null)
        {
            PrefabPath = config.PrefabPath;
            IsSimplyName = config.IsSimplyName;
            IsWithExtension = config.IsWithExtension;
            CodePath = config.CodePath;
            Namespace = config.Namespace;
        }
    }

    private void OnGUI()
    {
        GUILayout.BeginVertical();
        GUILayout.BeginHorizontal();
        PrefabPath = EditorGUILayout.TextField("Prefab存放路径: ", PrefabPath);
        if (GUILayout.Button("选择路径", GUILayout.Width(140f)))
        {
            var path = EditorUtility.OpenFolderPanel("选择Prefab存放路径", Application.dataPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                PrefabPath = path;
            }
        }
        GUILayout.EndHorizontal();
        IsSimplyName = EditorGUILayout.Toggle("是否使用简化名称", IsSimplyName);
        IsWithExtension = EditorGUILayout.Toggle("是否带扩展名", IsWithExtension);
        EditorGUILayout.HelpBox("", MessageType.None);
        GUILayout.BeginHorizontal();
        CodePath = EditorGUILayout.TextField("代码存放路径: ", CodePath);
        if (GUILayout.Button("选择路径", GUILayout.Width(140f)))
        {
            var path = EditorUtility.OpenFolderPanel("选择代码存放路径", Application.dataPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                CodePath = path;
            }
        }
        GUILayout.EndHorizontal();
        Namespace = EditorGUILayout.TextField("代码所在命名空间: ", Namespace);

        if (GUILayout.Button("保存配置"))
        {
            var newConfig = ScriptableObject.CreateInstance<UIAutoCreateConfig>();
            newConfig.PrefabPath = PrefabPath;
            newConfig.IsSimplyName = IsSimplyName;
            newConfig.IsWithExtension = IsWithExtension;
            newConfig.CodePath = CodePath;
            newConfig.Namespace = Namespace;
            AssetDatabase.CreateAsset(newConfig, ConfigPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        GUILayout.EndVertical();
    }
}
