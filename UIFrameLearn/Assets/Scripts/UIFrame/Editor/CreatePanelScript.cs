using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

public class CreatePanelScript
{
    [MenuItem("GameObject/自定义/创建界面预制和对应脚本", priority = 0)]
    public static void CreatePanelPrefabAndCS()
    {
        if (Selection.objects.Length <= 0) return;

        var config = AssetDatabase.LoadAssetAtPath<UIAutoCreateConfig>(UIHelperString.ConfigPath);

        string csNameStr = "";

        int index = 0;
        var length = Selection.objects.Length;
        foreach (var obj in Selection.objects)
        {
            index++;
            EditorUtility.DisplayProgressBar("创建预制和脚本", $"{obj.name}正在创建 {index}/{length}", index / (float)length);
            if (obj.name.Contains("UI"))
            {
                csNameStr += (obj.name + "|");
                CreateCS(obj.name, config);
                var go = obj as GameObject;

                if (!Directory.Exists(config.PrefabPath))
                {
                    Directory.CreateDirectory(config.PrefabPath);
                }
                PrefabUtility.SaveAsPrefabAsset(go, config.PrefabPath + obj.name + ".prefab");
                Debug.Log("预制生成成功：  "+ obj.name + ".prefab");
            }
        }
        EditorPrefs.SetString("CreatingCs", csNameStr);

        EditorUtility.ClearProgressBar();

        UIPrefabProcess.UIPrefabAddComp();

        Selection.objects = null;
        AssetDatabase.Refresh();
    }

    static void CreateCS(string objName, UIAutoCreateConfig config)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("using System.Collections;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(config.Namespace))
        {
            sb.AppendLine($"namespace {config.Namespace}");
            sb.AppendLine("{");
        }

        var loadPath = config.PrefabPath;
        var tempIndex = config.PrefabPath.IndexOf("/Resources");
        if (tempIndex != -1)
        {
            loadPath = config.PrefabPath.Substring(tempIndex + "/Resources".Length + 1);
        }
        else if (config.IsSimplyName)
        {
            loadPath = "";
        }

        loadPath += objName;
        if (config.IsWithExtension) loadPath += ".prefab";

        sb.AppendLine($"[PanelInfo(UILayer.FullWindow, UILife.Permanent, \"{loadPath}\",false)]");
        sb.AppendLine($"public partial class {objName} : BasePanel");
        sb.AppendLine("{");
        sb.AppendLine("\tprivate void Awake() { }");
        sb.AppendLine("\tprivate void Start() { }");
        sb.AppendLine("\tprotected override void OnFocus() { base.OnFocus(); }");
        sb.AppendLine("\tprotected override void OnShowStart(bool immediate) { base.OnShowStart(immediate); }");
        sb.AppendLine("\tprotected override void OnShowFinish() { base.OnShowFinish(); }");
        sb.AppendLine("\tprotected override void OnLoseFocus() { base.OnLoseFocus(); }");
        sb.AppendLine("\tprotected override void OnHideStart(bool immediate) { base.OnHideStart(immediate); }");
        sb.AppendLine("\tprotected override void OnHideFinish() { base.OnHideFinish(); }");
        sb.AppendLine("}");

        if (!string.IsNullOrEmpty(config.Namespace))
            sb.AppendLine("}");

        string csPath = config.CodePath + objName + "/" + objName + ".cs";

        CreateOrWriteFile(csPath, sb.ToString());
    }

    static void CreateOrWriteFile(string path, string info)
    {
        //判断这个路径是否存在
        var tempPath = GetFilePathWithOutFileName(path);
        if (!Directory.Exists(tempPath))
        {
            Directory.CreateDirectory(tempPath);
        }

        //判断这个文件是否存在
        //写入文件
        if (File.Exists(path))
        {
            Debug.Log(path + "    文件已存在，将被替换");
        }
        else
        {
            Debug.Log(path + "    创建文件");
        }

        //补充 using(){} ()中的对象必须继承IDispose接口,在{}结束后会自动释放资源,也就是相当于帮你调用了Dispos()去释放资源
        using (FileStream fileStream = new FileStream(path, FileMode.Create, FileAccess.Write))
        {
            using (TextWriter textWriter = new StreamWriter(fileStream))
            {
                textWriter.Write(info);
            }
        }

        AssetDatabase.Refresh();
    }

    static string GetFilePathWithOutFileName(string path)
    {
        var tempIndex = path.LastIndexOf("/");
        var result = path.Substring(0, tempIndex);

        return result;
    }
}