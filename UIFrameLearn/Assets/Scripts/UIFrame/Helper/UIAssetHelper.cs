using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if ADDRESSABLE
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

public class UIAssetHelper
{
    public static void LoadPanelPrefab(string path, Action<GameObject> finishCallback)
    {
        var prefab = Resources.Load<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogError($"不存在此预制 {path}");
            return;
        }

        finishCallback?.Invoke(prefab);
    }

    public static void ReleasePanelPrefab(GameObject prefab, string path)
    {
        Resources.UnloadAsset(prefab);
    }

#if ADDRESSABLE

    public static void LoadPanelPrefab(string path, Action<GameObject, AsyncOperationHandle> finishCallback)
    {
        var handle = Addressables.LoadAssetAsync<GameObject>(path);
        handle.Completed += (res) =>
        {
            var prefab = res.Result;
            if (prefab == null)
            {
                Debug.LogError($"不存在此预制 {path}");
                return;
            }

            finishCallback?.Invoke(prefab, handle);
        };
    }

    public static void ReleasePanelPrefab(GameObject prefab, string path, AsyncOperationHandle handle)
    {
        Addressables.Release(handle);
    }

#endif

}
