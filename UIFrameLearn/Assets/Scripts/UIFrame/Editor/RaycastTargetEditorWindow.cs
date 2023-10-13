using System.Collections;
using System.Collections.Generic;
using UnityEditor;

using UnityEngine;
using UnityEngine.UI;

public class RaycastTargetEditorWindow : EditorWindow
{
    static Vector3[] _fourCorners = new Vector3[4];
    private static Graphic[] _graphics;
    private static bool _inited = false;
    private Vector2 scrollPosition;
    private static Dictionary<GameObject, List<Graphic>> _rootToGraphics = new Dictionary<GameObject, List<Graphic>>();

    [MenuItem("Tools/工具/UI RaycastTarget编辑器")]
    static void ShowWindow()
    {
        var window = (RaycastTargetEditorWindow)EditorWindow.GetWindow(typeof(RaycastTargetEditorWindow), false, "UI RaycastTarget编辑器", true);
        window.Show();
    }

    /// <summary>
    /// 绘制编辑器
    /// </summary>
    private void OnGUI()
    {
        var selectGo = Selection.activeGameObject;
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        GUILayout.BeginVertical();
        foreach (var rootToGraphics in _rootToGraphics)
        {
            GUILayout.Label(rootToGraphics.Key.name, EditorStyles.boldLabel);
            foreach (var graphic in rootToGraphics.Value)
            {
                var currentGraphicGo = graphic.gameObject;
                var selectThis = currentGraphicGo == selectGo;

                if (selectThis)
                    GUILayout.BeginHorizontal(GUI.skin.box);
                else
                    GUILayout.BeginHorizontal();

                if (graphic.raycastTarget)
                    GUI.color = Color.white;
                else
                    GUI.color = Color.white * 0.7f;

                var newRaycastTargetState = GUILayout.Toggle(graphic.raycastTarget, "", GUILayout.Width(15));
                if (newRaycastTargetState != graphic.raycastTarget)
                {
                    Undo.RecordObject(graphic, "Graphic RaycastTarget");
                    graphic.raycastTarget = newRaycastTargetState;
                    EditorUtility.SetDirty(graphic);
                }

                var currentContent = $"{currentGraphicGo.name} ({graphic.GetType().Name})";
                if (selectThis)
                {
                    GUIStyle sty = new GUIStyle("ControlHighlight");
                    sty.alignment = TextAnchor.MiddleCenter;
                    sty.border.Add(new Rect(10,10,10,10));
                    sty.border.Add(new Rect(10,10,10,10));
                    sty.fixedHeight = 30;
                    if (GUILayout.Button(currentContent, sty))
                        Selection.activeGameObject = currentGraphicGo;
                }
                else
                {
                    if (GUILayout.Button(currentContent, EditorStyles.miniButton))
                        Selection.activeGameObject = currentGraphicGo;
                }

                GUILayout.EndHorizontal();
                GUILayout.Space(5);
            }
            GUILayout.Space(20);
        }
        GUILayout.EndVertical();
        GUILayout.EndScrollView();
        GUI.color = Color.white;
    }

    private void Update()
    {
        if (!_inited)
        {
            _inited = true;
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }
    }

    private void OnInspectorUpdate()
    {
        Repaint();
    }

    private void OnDestroy()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        _inited = false;
    }

    /// <summary>
    /// 获得当前场景的Graphics数据
    /// </summary>
    private static void OnSceneGUI(SceneView view)
    {
        var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null)
            _graphics = stage.prefabContentsRoot.transform.GetComponentsInChildren<Graphic>(true);
        else
            _graphics = GameObject.FindObjectsOfType<Graphic>();

        SortGraphics();
        DrawGraphics();
    }

    /// <summary>
    /// 在编辑器中绘制Graphics轮廓
    /// </summary>
    private static void DrawGraphics()
    {
        Handles.color = Color.red;
        foreach (Graphic g in _graphics)
        {
            if (g.raycastTarget)
            {
                var rectTransform = g.transform as RectTransform;
                rectTransform.GetWorldCorners(_fourCorners);
                for (int i = 0; i < 4; i++)
                    Handles.DrawLine(_fourCorners[i], _fourCorners[(i + 1) % 4]);
            }
        }
    }

    private static void SortGraphics()
    {
        _rootToGraphics.Clear();
        foreach (var graphic in _graphics)
        {
            var rootGo = graphic.transform.root.gameObject;
            if (!_rootToGraphics.ContainsKey(rootGo))
                _rootToGraphics.Add(rootGo, new List<Graphic>());
            _rootToGraphics[rootGo].Add(graphic);
        }
    }
}

