using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class BasePanel : MonoBehaviour
{
    public float LifeTimer { get; set; } //存活时间
    public bool IsOpened { get; private set; }
    public bool IsShowed { get; private set; }
    public bool IsFocused { get; private set; }
    public bool IsShowInProgress { get; private set; }
    public bool IsHideInProgress { get; private set; }

    private bool _isInitialized;

    protected bool _forceNoImmediate = false;

    private CanvasGroup _canvasGroup;

    public CanvasGroup CanvasGroup
    {
        get
        {
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();
            return _canvasGroup;
        }
    }

    /// <summary>
    /// 显示之前的回调
    /// </summary>
    private Action<BasePanel> ShowStartAction { get; set; }

    /// <summary>
    /// 显示之后的回调
    /// </summary>
    private Action<BasePanel> ShowFinishAction { get; set; }

    /// <summary>
    /// 隐藏之前的回调
    /// </summary>
    private Action<BasePanel> HideStartAction { get; set; }

    /// <summary>
    /// 隐藏之后的回调
    /// </summary>
    private Action<BasePanel> HideFinishAction { get; set; }

    public GameObject SelfGameObject
    {
        get => gameObject;
    }

    /// <summary>
    /// 只在实例化后执行一次
    /// </summary>
    public void Initialize()
    {
        if (!_isInitialized)
        {
            OnInitialized();
            _isInitialized = true;
        }
    }

    public void Show(bool immediate = true, Action<BasePanel> startAction = null,
        Action<BasePanel> finishAction = null)
    {
        if (startAction != null)
        {
            ShowStartAction += startAction;
        }

        if (finishAction != null)
        {
            ShowFinishAction += finishAction;
        }

        OnShowStart(immediate);
    }

    public void Hide(bool immediate = true, Action<BasePanel> startAction = null,
        Action<BasePanel> finishAction = null)
    {
        if (startAction != null)
        {
            HideStartAction += startAction;
        }

        if (finishAction != null)
        {
            HideFinishAction += finishAction;
        }

        OnHideStart(immediate);
    }

    public void Focus()
    {
        OnFocus();
    }

    public void LoseFocus()
    {
        OnLoseFocus();
    }

    protected virtual void OnInitialized()
    {
    }

    protected virtual void OnShowStart(bool immediate)
    {
        // Debug.Log(string.Format("<color=#9EF0F0>{0}</color>", gameObject.name + "          OnShowStart"));
        IsOpened = true;
        IsShowInProgress = true;
        gameObject.SetActive(true);

        ShowStartAction?.Invoke(this);
        ShowStartAction = null;

        if (immediate && !_forceNoImmediate)
        {
            //立即显示
            // transform.localScale = Vector3.one;
            CanvasGroup.alpha = 1;
            OnShowFinish();
        }
        else
        {
            _forceNoImmediate = false;
            StartShowAnimation();
        }
    }

    protected virtual void OnShowFinish()
    {
        // Debug.Log(string.Format("<color=#24A148>{0}</color>", gameObject.name + "          OnShowFinish"));
        IsShowInProgress = false;
        IsShowed = true;

        ShowFinishAction?.Invoke(this);
        ShowFinishAction = null;
    }

    protected virtual void OnHideStart(bool immediate)
    {
        // Debug.Log(string.Format("<color=#F1C21B>{0}</color>", gameObject.name + "          OnHideStart"));
        IsHideInProgress = true;
        HideStartAction?.Invoke(this);
        HideStartAction = null;

        if (immediate && !_forceNoImmediate)
        {
            //立即隐藏
            // transform.localScale = Vector3.zero;
            CanvasGroup.alpha = 0;
            gameObject.SetActive(false);
            OnHideFinish();
        }
        else
        {
            _forceNoImmediate = false;
            StartHideAnimation();
        }
    }

    protected virtual void OnHideFinish()
    {
        // Debug.Log(string.Format("<color=#FF0066>{0}</color>", gameObject.name + "          OnHideFinish"));
        IsOpened = false;
        IsHideInProgress = false;
        IsShowed = false;

        HideFinishAction?.Invoke(this);
        HideFinishAction = null;
    }

    protected virtual void OnFocus()
    {
        IsFocused = true;
        // Debug.Log(string.Format("<color=#A7F0BA>{0}</color>", gameObject.name + "          OnFocus"));
    }

    protected virtual void OnLoseFocus()
    {
        IsFocused = false;
        // Debug.Log(string.Format("<color=#0F62FE>{0}</color>", gameObject.name + "          OnLoseFocus"));
    }

    protected virtual void StartShowAnimation()
    {
        var obj = CreateTempMask();

        CanvasGroup.alpha = 0;
        // transform.localScale = Vector3.one;
        gameObject.SetActive(true);
        CanvasGroup.DOFade(1, 0.2f).SetDelay(0.2f).OnComplete(() =>
        {
            // CanvasGroup.interactable = true;
            DestroyImmediate(obj);
            OnShowFinish();
        });
    }

    protected virtual void StartHideAnimation()
    {
        var obj = CreateTempMask();
        CanvasGroup.DOFade(0, 0.2f).SetDelay(0.2f).OnComplete(() =>
        {
            // transform.localScale = Vector3.zero;
            // CanvasGroup.interactable = true;
            DestroyImmediate(obj);
            OnHideFinish();

            gameObject.SetActive(false);
        });
    }

    protected GameObject CreateTempMask()
    {
        var obj = new GameObject("TempMask");
        obj.transform.SetParent(transform);
        var image = obj.AddComponent<Image>();
        image.color = new Color32(255, 255, 255, 1);
        image.rectTransform.anchorMin = Vector2.zero;
        image.rectTransform.anchorMax = Vector2.one;
        image.rectTransform.anchoredPosition = Vector2.zero;
        image.rectTransform.sizeDelta = Vector2.zero;
        image.rectTransform.localPosition = Vector3.zero;
        image.rectTransform.localScale = Vector3.one;

        return obj;
    }
}