using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
#if ADDRESSABLE
using UnityEngine.ResourceManagement.AsyncOperations;

#endif

public class UIManager : MonoBehaviour
{
    public static UIManager Instance = null;

    #region 组件

    private Canvas _mainCanvas;
    private RectTransform _rectTransform;

    public Canvas MainCanvas => _mainCanvas;

    private Camera _uiCamera;

    public Camera UICamera
    {
        get { return _uiCamera; }
    }

    [HideInInspector] public Vector2 CanvasSize = Vector2.zero;

    private Dictionary<UILayer, RectTransform> _layerRootDict = new Dictionary<UILayer, RectTransform>();

    #endregion

    #region 数据

    private const string DefaultViewPrefabPath = "Prefabs/UI/Panel/";

    private Dictionary<string, PanelInfoAttribute> _panelInfoDict = new Dictionary<string, PanelInfoAttribute>(); //panel的信息
    private Dictionary<string, GameObject> _panelPrefabDict = new Dictionary<string, GameObject>(); //panel的预制
    private Dictionary<string, bool> _panelIsCreatingDict = new Dictionary<string, bool>(); //panel的创建状态
    private Dictionary<string, BasePanel> _panelDict = new Dictionary<string, BasePanel>(); //panel的实例

    private PanelInfoAttribute _curPanelInfo = null;
    private LinkedList<PanelInfoAttribute> _panelLinkedList = new LinkedList<PanelInfoAttribute>(); //打开的界面
    private Dictionary<UILayer, int> _layerPanelNumDict = new Dictionary<UILayer, int>(); //记录每个层级中有多少个界面
    private Dictionary<string, Dictionary<string, int>> _panelCanvasOriginalValDict = new Dictionary<string, Dictionary<string, int>>();

#if ADDRESSABLE
    private Dictionary<string, AsyncOperationHandle> _addressableHandleDict = new Dictionary<string, AsyncOperationHandle>();
#endif

    public PanelInfoAttribute CurPanelNameInfo
    {
        get => _curPanelInfo;
    }

    public BasePanel CurPanel
    {
        get => _curPanelInfo == null ? null : GetPanel(_curPanelInfo.Name);
    }

    #endregion

    #region 初始化

    private void Awake()
    {
        Instance = this;
        _uiCamera = transform.Find("Camera").GetComponent<Camera>();
        _mainCanvas = transform.Find("Canvas").GetComponent<Canvas>();
        _rectTransform = _mainCanvas.GetComponent<RectTransform>();
        CanvasSize = _rectTransform.sizeDelta;
        Init();
    }

    public void Init()
    {
        AnalysisAllPanelInfo();

        CheckIsPad();
        Initialize();
    }

    private void CheckIsPad()
    {
        if (GetIsPad())
        {
            var canvasScaler = _mainCanvas.GetComponent<CanvasScaler>();
            canvasScaler.matchWidthOrHeight = 1;
        }
    }

    private void AnalysisAllPanelInfo()
    {
        var assembly = Assembly.GetAssembly(typeof(PanelInfoAttribute));
        Type[] types = assembly.GetTypes();

        foreach (var t in types)
        {
            var attr = t.GetCustomAttribute(typeof(PanelInfoAttribute));
            {
                var info = attr as PanelInfoAttribute;

                if (info != null && t.BaseType.Name == typeof(BasePanel).Name)
                {
                    var tName = t.ToString();
                    info.SetName(tName, DefaultViewPrefabPath + tName);
                    _panelInfoDict.Add(tName, info);

                    // Debug.Log($"读取到{tName}: {info.Layer} {info.Path}");
                }
            }
        }
    }

    private void Initialize()
    {
        foreach (var layer in Enum.GetNames(typeof(UILayer)))
        {
            var layerRoot = new GameObject(layer);
            layerRoot.layer = LayerMask.NameToLayer("UI");
            var rectTransform = layerRoot.AddComponent<RectTransform>();
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
            layerRoot.transform.SetParent(_mainCanvas.transform, false);

            var can = layerRoot.AddComponent<Canvas>();
            layerRoot.AddComponent<GraphicRaycaster>();
            can.overrideSorting = true;
            if (Enum.TryParse<UILayer>(layer, out UILayer uil))
            {
                can.sortingOrder = (int) uil;
            }
        }
    }

    public IEnumerator InitPanelPrefab()
    {
        foreach (var item in _panelInfoDict)
        {
            var prefab = Resources.Load<GameObject>(item.Value.Path);
            if (prefab == null)
            {
                Debug.LogError($"不存在此预制{item.Value.Name} {item.Value.Path}");
                continue;
            }

            _panelPrefabDict.Add(item.Value.Name, prefab);

            var panelObj = Instantiate(prefab);
            SetPanelParent(panelObj, item.Value.Layer);
            var panel = panelObj.GetComponent(item.Key) as BasePanel;
            if (panel == null)
                Debug.LogError($"无法获取 {item.Key} 脚本，检查是否挂载");
            panel.Initialize();
            _panelDict.Add(item.Value.Name, panel);
            panelObj.gameObject.SetActive(false);

            yield return null;
        }
    }

    #endregion

    #region 界面操作

    #region 公开

    /// <summary>
    /// 打开界面
    /// </summary>
    /// <param name="startAction">打开前回调</param>
    /// <param name="finishAction">打开后回调</param>
    /// <typeparam name="T"></typeparam>
    public void OpenPanel<T>(Action<BasePanel> startAction, Action<BasePanel> finishAction = null) where T : BasePanel
    {
        OpenPanel<T>(false, startAction, finishAction);
    }

    /// <summary>
    /// 打开界面
    /// </summary>
    /// <param name="immediate">是否跳过动画立即打开</param>
    /// <param name="startAction">打开前回调</param>
    /// <param name="finishAction">打开后回调</param>
    /// <typeparam name="T"></typeparam>
    public void OpenPanel<T>(bool immediate = true, Action<BasePanel> startAction = null,
        Action<BasePanel> finishAction = null)
        where T : BasePanel
    {
        var tName = typeof(T).ToString();

        OpenPanel(tName, immediate, startAction, finishAction);
    }

    /// <summary>
    /// 打开界面
    /// </summary>
    /// <param name="tName">类型名称</param>
    /// <param name="startAction">打开前回调</param>
    /// <param name="finishAction">打开后回调</param>
    public void OpenPanel(string tName, Action<BasePanel> startAction = null, Action<BasePanel> finishAction = null)
    {
        OpenPanel(tName, false, startAction, finishAction);
    }

    /// <summary>
    /// 打开界面
    /// </summary>
    /// <param name="tName">类型名称</param>
    /// <param name="immediate">是否跳过动画立即打开</param>
    /// <param name="startAction">打开前回调</param>
    /// <param name="finishAction">打开后回调</param>
    public void OpenPanel(string tName, bool immediate = true, Action<BasePanel> startAction = null,
        Action<BasePanel> finishAction = null)
    {
        // Debug.LogError("打开界面 " + tName);

        if (_panelInfoDict.ContainsKey(tName))
        {
            var panelInfo = _panelInfoDict[tName];

            if (_panelDict.ContainsKey(tName))
            {
                if (_panelDict[tName] is BasePanel panel)
                {
                    if (panel.IsOpened)
                    {
                        Debug.Log($"当前界面已经打开 {tName}");
                        return;
                    }
                    else
                    {
                        ShowPanel(panelInfo, panel, immediate, startAction, finishAction);
                    }
                }
            }
            else
            {
                CreatePanel(panelInfo, immediate, startAction, finishAction);
            }
        }
        else
        {
            Debug.LogError($"未注册{tName} 检查你的Panel是否继承PanelBase，是否添加PanelInfo属性");
        }
    }

    /// <summary>
    /// 关闭界面
    /// </summary>
    /// <param name="startAction">关闭前回调</param>
    /// <param name="finishAction">关闭后回调</param>
    /// <typeparam name="T"></typeparam>
    public void ClosePanel<T>(Action<BasePanel> startAction, Action<BasePanel> finishAction = null) where T : BasePanel
    {
        ClosePanel<T>(false, startAction, finishAction);
    }

    /// <summary>
    /// 关闭界面
    /// </summary>
    /// <param name="immediate">是否跳过动画立即关闭</param>
    /// <param name="startAction">关闭前回调</param>
    /// <param name="finishAction">关闭后回调</param>
    /// <typeparam name="T"></typeparam>
    public void ClosePanel<T>(bool immediate = true, Action<BasePanel> startAction = null,
        Action<BasePanel> finishAction = null)
        where T : BasePanel
    {
        var tName = typeof(T).ToString();

        ClosePanel(tName, immediate, startAction, finishAction);
    }

    public void ClosePanel(string tName, Action<BasePanel> startAction = null,
        Action<BasePanel> finishAction = null)
    {
        ClosePanel(tName, false, startAction, finishAction);
    }

    /// <summary>
    /// 关闭界面
    /// </summary>
    /// <param name="tName">类型名称</param>
    /// <param name="immediate">是否跳过动画立即关闭</param>
    /// <param name="startAction">关闭前回调</param>
    /// <param name="finishAction">关闭后回调</param>
    public void ClosePanel(string tName, bool immediate = true, Action<BasePanel> startAction = null,
        Action<BasePanel> finishAction = null)
    {
        if (_panelDict.ContainsKey(tName))
        {
            if (_panelDict[tName] is BasePanel panel)
            {
                var panelInfo = _panelInfoDict[tName];
                HidePanel(panelInfo, panel, immediate, startAction, finishAction);
            }
        }
        else
        {
            Debug.Log($"关闭失败 {tName} 尚未打开");
        }
    }

    /// <summary>
    /// 获取界面
    /// </summary>
    /// <typeparam name="T">界面type</typeparam>
    /// <returns></returns>
    public T GetPanel<T>() where T : BasePanel
    {
        var typeName = typeof(T).ToString();
        if (_panelDict.ContainsKey(typeName))
        {
            return _panelDict[typeName] as T;
        }
        else
        {
            // Debug.LogError($"不存在此Panel 检查 {typeName} 是否声明 {typeof(T).ToString()}");
        }

        return null;
    }

    public BasePanel GetPanel(string typeName)
    {
        if (_panelDict.ContainsKey(typeName))
        {
            return _panelDict[typeName];
        }
        else
        {
            // Debug.LogError($"不存在此Panel 检查 {typeName} 是否声明 {typeof(T).ToString()}");
        }

        return null;
    }

    #endregion

    #region 私有

    private void CreatePanel(PanelInfoAttribute panelInfo, bool immediate = true,
        Action<BasePanel> startAction = null,
        Action<BasePanel> finishAction = null)
    {
        if (_panelPrefabDict.ContainsKey(panelInfo.Name))
        {
            var prefab = _panelPrefabDict[panelInfo.Name];
            CreatePanel(prefab, panelInfo, immediate, startAction, finishAction);
        }
        else
        {
            if (_panelIsCreatingDict.TryGetValue(panelInfo.Name, out bool isLoading))
            {
                if (isLoading)
                {
                    Debug.Log($"此Panel {panelInfo.Name} 正在加载中");
                    return;
                }
                else
                {
                    _panelIsCreatingDict[panelInfo.Name] = true;
                }
            }
            else
            {
                _panelIsCreatingDict.Add(panelInfo.Name, true);
            }

#if ADDRESSABLE
            UIAssetHelper.LoadPanelPrefab(panelInfo.Path, (prefab, handle) =>
            {
                _panelPrefabDict.Add(panelInfo.Name, prefab);
                CreatePanel(prefab, panelInfo, immediate, startAction, finishAction);
                _panelIsCreatingDict[panelInfo.Name] = false;

                _addressableHandleDict.Add(panelInfo.Name, handle);
            });
#else
            UIAssetHelper.LoadPanelPrefab(panelInfo.Path, (prefab) =>
            {
                _panelPrefabDict.Add(panelInfo.Name, prefab);
                CreatePanel(prefab, panelInfo, immediate, startAction, finishAction);
                _panelIsCreatingDict[panelInfo.Name] = false;
            });
#endif
        }
    }

    private void CreatePanel(GameObject prefab, PanelInfoAttribute panelInfo, bool immediate = true,
        Action<BasePanel> startAction = null,
        Action<BasePanel> finishAction = null)
    {
        var panelObj = Instantiate(prefab);
        SetPanelParent(panelObj, panelInfo.Layer);
        var panel = panelObj.GetComponent(panelInfo.Name) as BasePanel;
        if (panel == null)
            Debug.LogError($"无法获取 {panelInfo.Name} 脚本，检查是否挂载");
        panel.Initialize();
        _panelDict.Add(panelInfo.Name, panel);
        panel.gameObject.SetActiveEx(false);

        ShowPanel(panelInfo, panel, immediate, startAction, finishAction);
    }

    private void ShowPanel(PanelInfoAttribute panelInfo, BasePanel panel, bool immediate = true,
        Action<BasePanel> startAction = null,
        Action<BasePanel> finishAction = null)
    {
        if (!CheckIsHaveHigherLayerInStack(panelInfo.Layer))
        {
            panel.Focus();
            if (_panelLinkedList.Count > 0)
            {
                var stackTopPanelInfo = _panelLinkedList.Last.Value;
                if (panelInfo.Name != "UICurrencyPanel")
                {
                    GetPanel(stackTopPanelInfo.Name)?.LoseFocus();
                }
            }
        }

        panel.Show(immediate, startAction, finishAction);
        panel.SelfGameObject.transform.SetAsLastSibling();

        if (panelInfo.Name != "UICurrencyPanel")
        {
            AddToLinkedList(panelInfo);
        }

        if (_panelLinkedList.Count > 0)
            _curPanelInfo = _panelLinkedList.Last.Value;

        ReSetCanvasOrder(panelInfo);
    }

    private void HidePanel(PanelInfoAttribute panelInfo, BasePanel panel, bool immediate = true,
        Action<BasePanel> startAction = null,
        Action<BasePanel> finishAction = null)
    {
        var panelIsInStackTop = _panelLinkedList.Last.Value.Name == panelInfo.Name;

        RemoveFromLinkedList(panelInfo.Name);
        if (_panelLinkedList.Count > 0)
            _curPanelInfo = _panelLinkedList.Last.Value;
        else
            _curPanelInfo = null;

        if (!CheckIsHaveHigherLayerInStack(panelInfo.Layer) && panelIsInStackTop)
        {
            panel.LoseFocus();

            if (_panelLinkedList.Count > 0)
            {
                var stackTopPanelInfo = _panelLinkedList.Last.Value;

                GetPanel(stackTopPanelInfo.Name)?.Focus();
            }
        }

        panel.Hide(immediate, startAction, (p) =>
        {
            finishAction?.Invoke(p);

            if (CheckHaveNextWaitPanel())
            {
                OpenPanelInWaitQueue();
            }
        });
    }

    /// <summary>
    /// 重新计算渲染层级
    /// </summary>
    /// <param name="panelInfo"></param>
    private void ReSetCanvasOrder(PanelInfoAttribute panelInfo)
    {
        if (_layerPanelNumDict.ContainsKey(panelInfo.Layer))
        {
            _layerPanelNumDict[panelInfo.Layer] += 1;
        }
        else
        {
            _layerPanelNumDict.Add(panelInfo.Layer, 1);
        }


        int index = 0;
        foreach (var info in _panelLinkedList)
        {
            if (info.Layer == panelInfo.Layer)
            {
                ResetPanelOrder(info, (int) panelInfo.Layer, index);
                index++;
            }
        }
    }

    private void ResetPanelOrder(PanelInfoAttribute info, int orderOffset, int index)
    {
        int increment = 100;

        var panel = GetPanel(info.Name);

        var canvas = panel.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = panel.gameObject.AddComponent<Canvas>();
        }

        var gr = panel.GetComponent<GraphicRaycaster>();
        if (gr == null)
        {
            panel.gameObject.AddComponent<GraphicRaycaster>();
        }

        canvas.overrideSorting = true;
        var startOrder = orderOffset + index * increment;
        canvas.sortingOrder = startOrder;

        var canvasArr = panel.GetComponentsInChildren<Canvas>(true);
        foreach (var ca in canvasArr)
        {
            if (object.ReferenceEquals(ca, canvas)) continue;

            ca.overrideSorting = true;

            int originalVal = 0;
            var routePath = GetRoute(ca.transform, canvas.transform);
            if (_panelCanvasOriginalValDict.ContainsKey(info.Name))
            {
                if (_panelCanvasOriginalValDict[info.Name].ContainsKey(routePath))
                {
                    originalVal = _panelCanvasOriginalValDict[info.Name][routePath];
                }
                else
                {
                    originalVal = ca.sortingOrder;
                    _panelCanvasOriginalValDict[info.Name].Add(routePath, originalVal);
                }
            }
            else
            {
                originalVal = ca.sortingOrder;
                _panelCanvasOriginalValDict.Add(info.Name, new Dictionary<string, int>() {{routePath, originalVal}});
            }

            ca.sortingOrder = startOrder + originalVal;
        }

        var particleArr = panel.GetComponentsInChildren<ParticleSystemRenderer>(true);
        foreach (var pa in particleArr)
        {
            int originalVal = 0;
            var routePath = GetRoute(pa.transform, canvas.transform);
            if (_panelCanvasOriginalValDict.ContainsKey(info.Name))
            {
                if (_panelCanvasOriginalValDict[info.Name].ContainsKey(routePath))
                {
                    originalVal = _panelCanvasOriginalValDict[info.Name][routePath];
                }
                else
                {
                    originalVal = pa.sortingOrder;
                    _panelCanvasOriginalValDict[info.Name].Add(routePath, originalVal);
                }
            }
            else
            {
                originalVal = pa.sortingOrder;
                _panelCanvasOriginalValDict.Add(info.Name, new Dictionary<string, int>() {{routePath, originalVal}});
            }

            pa.sortingOrder = startOrder + originalVal;
        }
    }

    private void SetPanelParent(GameObject go, UILayer layer)
    {
        var parent = _mainCanvas.transform.Find(layer.ToString());
        go.transform.SetParent(parent, false);
    }

    private void DestroyPanel(string viewName)
    {
        var view = _panelDict[viewName];
        if (!(view is null)) Destroy(view.SelfGameObject);

        _panelDict.Remove(viewName);

        ReleaseAsset(viewName);
    }

    private void ReleaseAsset(string viewName)
    {
        var viewInfo = _panelInfoDict[viewName];
        if (viewInfo.ReleaseAssetWhenDestory)
        {
            var prefab = _panelPrefabDict[viewInfo.Name];
            _panelPrefabDict.Remove(viewInfo.Name);
#if ADDRESSABLE
            var handle = _addressableHandleDict[viewInfo.Name];
            _addressableHandleDict.Remove(viewInfo.Name);
            UIAssetHelper.ReleasePanelPrefab(prefab, viewInfo.Path, handle);
#else
            UIAssetHelper.ReleasePanelPrefab(prefab, viewInfo.Path);
#endif
        }
    }

    private void AddToLinkedList(PanelInfoAttribute panelInfo)
    {
        if (_panelLinkedList.Count <= 0)
        {
            _panelLinkedList.AddLast(panelInfo);
            return;
        }

        var node = _panelLinkedList.First;
        while (node != null)
        {
            var nodeNext = node.Next;
            if (nodeNext == null)
            {
                _panelLinkedList.AddAfter(node, panelInfo);
                return;
            }
            else
            {
                if (panelInfo.Layer >= node.Value.Layer && panelInfo.Layer < nodeNext.Value.Layer)
                {
                    _panelLinkedList.AddAfter(node, panelInfo);
                    return;
                }
                else
                    node = node.Next;
            }
        }
    }

    private void RemoveFromLinkedList(string panelName)
    {
        var node = _panelLinkedList.First;
        while (node != null)
        {
            if (node.Value.Name.Equals(panelName))
            {
                _panelLinkedList.Remove(node.Value);
                return;
            }
            else
                node = node.Next;
        }
    }

    #endregion

    #endregion

    #region 针对弹窗的界面队列

    private Queue<WaitPanelInfo> _panelWaitQueue = new Queue<WaitPanelInfo>();

    private class WaitPanelInfo
    {
        public string tName = "";
        public bool immediate = true;
        public Action<BasePanel> startAction = null;
        public Action<BasePanel> finishAction = null;
    }

    public void AddPanelToWaitQueue<T>(bool immediate = true, Action<BasePanel> startAction = null,
        Action<BasePanel> finishAction = null)
        where T : BasePanel
    {
        WaitPanelInfo info = new WaitPanelInfo();
        info.tName = typeof(T).ToString();
        info.immediate = immediate;
        info.startAction = startAction;
        info.finishAction = finishAction;

        _panelWaitQueue.Enqueue(info);
    }

    public void OpenPanelInWaitQueue()
    {
        if (_panelWaitQueue.Count <= 0) return;

        var info = _panelWaitQueue.Dequeue();

        OpenPanel(info.tName, info.immediate, info.startAction, info.finishAction);
    }

    public bool CheckHaveNextWaitPanel()
    {
        return _panelWaitQueue.Count > 0;
    }

    #endregion

    #region 生命周期

    private int _frame = 0;
    private int _frameMax = 2; //几帧更新一次

    private void Update()
    {
        _frame++;
        if (_frame >= _frameMax)
        {
            _frame = 0;

            var panelNameList = _panelDict.Keys.ToList();

            for (int i = 0; i < panelNameList.Count; i++)
            {
                var panelName = panelNameList[i];

                var panel = _panelDict[panelName];
                if (panel == null)
                {
                    Debug.LogError($"界面 {panelName} 为空");
                    continue;
                }

                var panelInfo = _panelInfoDict[panelName];
                if ((int) panelInfo.Life <= -1) continue;

                if (panel.IsShowed || panel.IsShowInProgress) continue;

                panel.LifeTimer += Time.deltaTime * _frameMax;

                if (panel.LifeTimer > (int) panelInfo.Life)
                {
                    DestroyPanel(panelName);
                }
            }
        }
    }

    #endregion

    #region 其他UI内容

    public RectTransform GetUILayerRoot(UILayer layer)
    {
        if (_layerRootDict.ContainsKey(layer))
            return _layerRootDict[layer];
        else
        {
            var rect = transform.Find($"Canvas/{layer.ToString()}") as RectTransform;
            _layerRootDict.Add(layer, rect);
            return rect;
        }
    }

    private bool CheckIsHaveHigherLayerInStack(UILayer curLayer)
    {
        foreach (var item in _panelLinkedList)
        {
            var order = (int) item.Layer;
            if (order != -1 && order > (int) curLayer)
            {
                return true;
            }
        }

        return false;
    }

    #endregion

    #region Helper

    public Vector2 GetWorldToUIPoint(Camera cam, Vector3 worldPos)
    {
        Vector2 uiPos;
        var screenPos = cam.WorldToScreenPoint(worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(GetUILayerRoot(UILayer.Guide), screenPos, UICamera, out uiPos);
        return uiPos;
    }

    public Vector2 GetUIWorldToUIPoint(Vector3 worldPos)
    {
        Vector2 uiPos;
        var screenPos = RectTransformUtility.WorldToScreenPoint(UICamera, worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(GetUILayerRoot(UILayer.Guide), screenPos, UICamera, out uiPos);
        return uiPos;
    }
    
    public bool GetIsPad()
    {
        return 1f * Screen.height / Screen.width < 1.77f;
    }

    public string GetRoute(Transform childTrans, Transform rootTrans = null, string splitter = ".")
    {
        var result = childTrans.name;
        var parent = childTrans.parent;
        while (null == rootTrans ? parent != null : !ReferenceEquals(rootTrans, parent))
        {
            result = $"{parent.name}{splitter}{result}";
            parent = parent.parent;
        }

        return result;
    }

    #endregion

    #region Toast

    private UIToastPanel _uiToastPanel = null;

    public void ShowToast(string str)
    {
        if (_uiToastPanel == null)
        {
            var prefab = Resources.Load<GameObject>("Toast/UIToastPanel");
            var go = Instantiate(prefab);
            go.transform.SetParent(GetUILayerRoot(UILayer.Tips), false);
            _uiToastPanel = go.GetComponent<UIToastPanel>();
        }

        _uiToastPanel.ShowToast(str);
    }

    #endregion

    #region Banner

    public async void ShowBanner()
    {
        var bannerObj = await LoadAssetService.Instance.InstantiateByPrefab("Assets/AddressablesResources/Prefabs/UI/Item/BannerBottom.prefab");
        var bannerBg = bannerObj.GetComponent<BannerBg>();
        bannerBg.transform.SetParent(GetUILayerRoot(UILayer.Top), false);
        bannerBg.ShowBanner();
    }

    #endregion

    #region 加载图集

    public Dictionary<string, SpriteAtlas> _atlasDict = new Dictionary<string, SpriteAtlas>();
    public Dictionary<string, Task<SpriteAtlas>> _atlasTaskDict = new Dictionary<string, Task<SpriteAtlas>>();

    public async void PreLoadAtlas(string atlasName)
    {
        if (_atlasDict.ContainsKey(atlasName)) return;
        if (_atlasTaskDict.ContainsKey(atlasName)) return;

        var atlas = await LoadAssetService.Instance.LoadAssetAsync<SpriteAtlas>(gameObject.name, atlasName);

        if (!_atlasDict.ContainsKey(atlasName))
        {
            _atlasDict.Add(atlasName, atlas);
        }
    }

    public async void LoadSpriteInAtlas(string atlasName, string spriteName, Action<Sprite> callBack)
    {
        if (_atlasDict.ContainsKey(atlasName))
        {
            callBack?.Invoke(_atlasDict[atlasName].GetSprite(spriteName));
            return;
        }

        Task<SpriteAtlas> task = null;

        if (_atlasTaskDict.ContainsKey(atlasName))
        {
            task = _atlasTaskDict[atlasName];
        }
        else
        {
            task = LoadAssetService.Instance.LoadAssetAsync<SpriteAtlas>(gameObject.name, atlasName);
            _atlasTaskDict.Add(atlasName, task);
        }

        var atlas = await task;

        if (!_atlasDict.ContainsKey(atlasName))
        {
            _atlasDict.Add(atlasName, atlas);
        }

        callBack?.Invoke(atlas.GetSprite(spriteName));
    }

    public void ReleaseAtlas(string atlasName)
    {
        LoadAssetService.Instance.ReleaseAssetSingle(atlasName);

        if (_atlasDict.ContainsKey(atlasName))
            _atlasDict.Remove(atlasName);
        if (_atlasTaskDict.ContainsKey(atlasName))
            _atlasTaskDict.Remove(atlasName);
    }

    #endregion
}