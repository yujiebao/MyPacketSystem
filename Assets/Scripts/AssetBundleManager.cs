using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Collections;
using UnityEngine.Networking;

public class AssetBundleManager : MonoBehaviour
{
    private static AssetBundleManager instance;
    public static AssetBundleManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("AssetBundleManager");
                instance = go.AddComponent<AssetBundleManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    private Dictionary<string, AssetBundle> loadedBundles = new Dictionary<string, AssetBundle>();
    private string assetBundlePath;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePath();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializePath()
    {
        #if UNITY_EDITOR
        assetBundlePath = Path.Combine(Application.streamingAssetsPath, "UI");
        #else
        assetBundlePath = Path.Combine(Application.persistentDataPath, "UI");
        #endif
    }

    public IEnumerator LoadAssetBundleFromNetwork(string url, string bundleName)
    {
        using (UnityWebRequest uwr = UnityWebRequestAssetBundle.GetAssetBundle(url))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error downloading AssetBundle: {uwr.error}");
            }
            else
            {
                AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(uwr);
                if (bundle != null)
                {
                    loadedBundles[bundleName] = bundle;
                }
                else
                {
                    Debug.LogError("Failed to load AssetBundle from network.");
                }
            }
        }
    }

    // 从本地加载主包
    public void LoadAssetBundleFromLocal(string bundleName)
    {
        if (!loadedBundles.ContainsKey(bundleName))
        {
            string path = Path.Combine(assetBundlePath, bundleName);
            var bundle = AssetBundle.LoadFromFile(path);
            if (bundle != null)
            {
                loadedBundles[bundleName] = bundle;
            }
            else
            {
                Debug.LogError($"Failed to load AssetBundle from local path: {path}");
            }
        }
    }

    public T LoadAsset<T>(string bundleName, string assetName) where T : UnityEngine.Object
    {
        if (loadedBundles.ContainsKey(bundleName))
        {
            return loadedBundles[bundleName].LoadAsset<T>(assetName);
        }
        else
        {
            Debug.LogError($"AssetBundle {bundleName} not loaded.");
            return null;
        }
    }

    public void UnloadBundle(string bundleName, bool unloadAllLoadedObjects = false)
    {
        if (loadedBundles.ContainsKey(bundleName))
        {
            loadedBundles[bundleName].Unload(unloadAllLoadedObjects);
            loadedBundles.Remove(bundleName);
        }
    }
}
