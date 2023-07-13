using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class UIPrefabProcess
{

    [UnityEditor.Callbacks.DidReloadScripts]
    private static void OnScriptsReloaded()
    {
        UIPrefabAddComp();
    }

    public static void UIPrefabAddComp()
    {
        var csNameStr = EditorPrefs.GetString("CreatingCs", "");
        if (!string.IsNullOrEmpty(csNameStr))
        {
            bool needResetCreatingCsStr = true;

            var nameArr = csNameStr.Split('|');
            if (nameArr.Length <= 0) return;

            var config = AssetDatabase.LoadAssetAtPath<UIAutoCreateConfig>(UIHelperString.ConfigPath);

            foreach (var name in nameArr)
            {
                if (!string.IsNullOrEmpty(name))
                {
                    var prefabPath = config.PrefabPath + name + ".prefab";
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

                    var type = GetTypeByName(name);
                    if (type == null)
                    {
                        needResetCreatingCsStr = false;
                        // Debug.LogError($"获取 {name} 类型失败");
                        continue;
                    }

                    AddComponentEx(prefab, type);
                    AddComponentEx<ComponentAutoBindTool>(prefab);

                    EditorUtility.SetDirty(prefab);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            }

            if (needResetCreatingCsStr)
                EditorPrefs.SetString("CreatingCs", "");
        }
    }

    public static System.Type GetTypeByName(string name)
    {
        foreach (Assembly assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (System.Type type in assembly.GetTypes())
            {
                if (type.Name == name)
                    return type;
            }
        }

        return null;
    }

    public static T AddComponentEx<T>(GameObject obj) where T : Component
    {
        var comp = obj.GetComponent<T>();
        if (comp == null)
        {
            comp = obj.AddComponent<T>();
        }

        return comp;
    }

    public static void AddComponentEx(GameObject obj, Type t)
    {
        var comp = obj.GetComponent(t);
        if (comp == null)
        {
            obj.AddComponent(t);
        }
    }
}
