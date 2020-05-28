﻿using UnityEngine;
using System.IO;
using System.Collections;
using ZFrame.Asset;

public class AssetsMgr : MonoSingleton<AssetsMgr>
{
    public const int VER = 0x3F7A0;

    public static AssetsMgr A { get { return Instance; } }

    public event System.Action<int, int> onResolutionChanged;

#if UNITY_EDITOR
    [SerializeField, HideInInspector]
    private bool m_PrintLoadedLuaStack = false;
    public bool printLoadedLuaStack { get { return m_PrintLoadedLuaStack; } }
    [SerializeField, HideInInspector]
    private bool m_UseLuaAssetBundle = false;
    public bool useLuaAssetBundle {
        get { return m_UseLuaAssetBundle; }
        set { m_UseLuaAssetBundle = value; }
    }
    [SerializeField, HideInInspector]
    private bool m_UseAssetBundleLoader = false;
    public bool useAssetBundleLoader {
        get { return m_UseLuaAssetBundle || m_UseAssetBundleLoader; }
        set { m_UseAssetBundleLoader = value; }
    }
#elif UNITY_STANDALONE
	public bool printLoadedLuaStack { get { return false; } }
    public bool useLuaAssetBundle { get; private set; }
	public bool useAssetBundleLoader { get { return true; } }
#else
    public bool printLoadedLuaStack { get { return false; } }
    public bool useLuaAssetBundle { get; private set; }
	public bool useAssetBundleLoader { get { return true; } }
#endif
    public int resHeight { get; private set; }
    
    [Description("原始分辨率")]
    public Resolution RawResolution { get; private set; }

    public AssetLoader Loader { get; private set; }

    private void SetQuality()
    {
        int quality = 6;
        string[] names = QualitySettings.names;
        if(quality >= names.Length){
            quality = names.Length - 1;
        }
        Debug.Log("Quality set to " + names[quality]);
        QualitySettings.SetQualityLevel(quality);
    }

    public void SetResolution(int height)
    {
        if (height == 0) height = RawResolution.height;
        if (resHeight == height) return;

        resHeight = height;
        
#if !UNITY_STANDALONE
        // 要设置的分辨率不能高于原始分辨率
        if (height > RawResolution.height) return;
#endif
        
        var width = (int)(height * (float)RawResolution.width / RawResolution.height);

#if UNITY_EDITOR

#else
        Screen.SetResolution(width, height, Screen.fullScreen);
#endif
        if (onResolutionChanged != null) onResolutionChanged.Invoke(width, height);

        Debug.LogFormat("Screen: {0} x {1}, fullScreen:{2}", width, height, Screen.fullScreen);
    }

    /*************************************************
     * 启动后加载Lua脚本
     *************************************************/
    public const string LUA_SCRIPT = "lua/script/";
    public const string LUA_CONFIG = "lua/config/";
    public const string KEY_MD5_STREAMING_LUA = "Streaming-Lua";
    public const string KEY_DATE_STREAMING_LUA = "Streaming-Lua-Date";
    public const string KEY_MD5_USING_LUA = "Using-Lua";
    public const string KEY_DATE_USING_LUA = "Using-Lua-Date";

    // 初始化Lua脚本
    private IEnumerator InitScriptsFromAssetBunles()
    {
        if (useAssetBundleLoader) {
            if (useLuaAssetBundle) {
                yield return AssetBundleLoader.I.LoadMD5();

                var md5 = AssetBundleLoader.I.md5;
                string streamingMD5 = PlayerPrefs.GetString(KEY_MD5_STREAMING_LUA);
                string streamingDate = PlayerPrefs.GetString(KEY_DATE_STREAMING_LUA);
                if (md5 != streamingMD5) {
                    streamingMD5 = md5;
                    streamingDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    PlayerPrefs.SetString(KEY_MD5_STREAMING_LUA, streamingMD5);
                    PlayerPrefs.SetString(KEY_DATE_STREAMING_LUA, streamingDate);
                    LogMgr.D("Update streaming lua to [{0}] at [{1}]", streamingMD5, streamingDate);
                }

                string bundleMD5 = PlayerPrefs.GetString(KEY_MD5_USING_LUA);
                string bundleDate = PlayerPrefs.GetString(KEY_DATE_USING_LUA);
                if (!string.IsNullOrEmpty(bundleMD5)) {
                    if (streamingMD5 != bundleMD5) {
                        // 已存在lua脚本在bundleRootPath, 比较时间           
                        var dtStreaming = System.DateTime.Parse(streamingDate);
                        var dtUsing = System.DateTime.Parse(bundleDate);
                        if (dtUsing < dtStreaming) {
                            // Streaming 的Lua脚本比较新，这是个新包。移除所有缓存的资源
                            if (Directory.Exists(AssetBundleLoader.bundleRootPath)) {
                                Directory.Delete(AssetBundleLoader.bundleRootPath, true);
                                Directory.CreateDirectory(AssetBundleLoader.bundleRootPath);
                            }

                            PlayerPrefs.SetString(KEY_MD5_USING_LUA, streamingMD5);
                            PlayerPrefs.SetString(KEY_DATE_USING_LUA, streamingDate);
                            LogMgr.D("Update using lua to [{0}] at [{1}]", streamingMD5, streamingDate);
                        } else {
                            LogMgr.D("Use bundle lua of [{0}] at [{1}]", bundleMD5, bundleDate);
                        }
                    } else {
                        LogMgr.D("Using origin lua of [{0}] at [{1}]", streamingMD5, streamingDate);
                    }
                } else {
                    // 首次启动游戏
                    PlayerPrefs.SetString(KEY_MD5_USING_LUA, streamingMD5);
                    PlayerPrefs.SetString(KEY_DATE_USING_LUA, streamingDate);
                    LogMgr.D("First using lua to [{0}] at [{1}]", streamingMD5, streamingDate);
                }

                yield return AssetBundleLoader.I.LoadFileList();
                yield return Loader.LoadingAsset(null, LUA_SCRIPT, LoadMethod.Forever);
                yield return Loader.LoadingAsset(null, LUA_CONFIG, LoadMethod.Forever);
            } else {
                yield return AssetBundleLoader.I.LoadFileList();
            }
        }
        

        if (ZFrame.UIManager.Instance == null) {
            yield return Loader.LoadingAsset(null, "Shaders/", LoadMethod.Always);
           
            yield return Loader.LoadingAsset(typeof(GameObject), "Launch/UIROOT", LoadMethod.Forever);
            GoTools.AddForever(Loader.loadedObj as GameObject);
        }
    }

    protected override void Awaking()
    {
        DontDestroyOnLoad(gameObject);
        VersionMgr.Reset();

#if !UNITY_EDITOR
        // 自定义lua代码位置
        if (File.Exists("lua.txt")) {
            useLuaAssetBundle = false;
            var path = File.ReadAllText("lua.txt").Trim();
            if (!string.IsNullOrEmpty(path)) {
                ChunkAPI.LuaROOT = path;
            }
        } else {
            useLuaAssetBundle = true;
        }
#endif

#if UNITY_5_5_OR_NEWER
        UnityEngine.Assertions.Assert.raiseExceptions = true;
#endif

        LogMgr.D("[Lua] {0}", useLuaAssetBundle ? "AssetBundle" : "Source Code");
        LogMgr.D("[Assets] {0}", useAssetBundleLoader ? "AssetBundle" : "Assets Folder");

        if (useAssetBundleLoader) {
            Loader = gameObject.AddComponent<AssetBundleLoader>();
        } else {
#if UNITY_EDITOR
            Loader = gameObject.AddComponent<AssetsSimulate>();
#else
            LogMgr.E("非编辑器模式不支持模拟使用AssetBundle。");
#endif
        }

        AssetLoader.CollectGarbage = GC;
        RawResolution = Screen.currentResolution;
        resHeight = 0;
        
        Vectrosity.VectorLine.layerName = "Default";
    }

    private void Start()
    {
#if UNITY_EDITOR
        SetQuality();
#endif
        StartCoroutine(InitScriptsFromAssetBunles());
    }

    public bool isQuiting { get; private set; }
    public event System.Action AppQuit;
    private void OnApplicationQuit()
    {
        isQuiting = true;
        ZFrame.Bundle.Unpacker.isQuiting = true;
        if (AppQuit != null) AppQuit.Invoke();
    }

#if UNITY_EDITOR || UNITY_STANDALONE
    // 监控分辨率发生变化
    private int m_LastResW, m_LastResH;
    private void OnGUI()
    {
        if (m_LastResW != Screen.width || m_LastResH != Screen.height) {
            m_LastResW = Screen.width;
            m_LastResH = Screen.height;

            if (onResolutionChanged != null) onResolutionChanged.Invoke(m_LastResW, m_LastResH);
        }
    }
#endif

    /// <summary>
    /// 同步加载资源
    /// </summary>
    /// <param name="type">资源类型</param>
    /// <param name="path">路径：AssetBundle/ObjectName</param>    
    /// <param name="warnIfMissing">找不到时是否需要警告</param>
    /// <returns>加载到的资源</returns>
    public Object Load(System.Type type, string path, bool warnIfMissing = true)
    {
        return Loader.Load(type, path, warnIfMissing);
    }
    public T Load<T>(string path, bool warnIfMissing = true) where T : Object
    {
        if (string.IsNullOrEmpty(path)) return default(T);

        return Load(typeof(T), path, warnIfMissing) as T;
    }

    /// <summary>
    /// 异步加载一个资源
    /// </summary>
    /// <param name="type">资源类型</param>
    /// <param name="path">路径：AssetBundle/ObjectName</param>
    /// <param name="onLoaded">加载结束后做啥</param>
    public bool LoadAsync(System.Type type, string path, LoadMethod method = LoadMethod.Default, DelegateObjectLoaded onLoaded = null, object param = null)
    {
        return Loader.LoadAsync(type, path, method, onLoaded, param);
    }

    public void LoadAsync(System.Type type, string path, LoadMethod method, ref bool loading)
    {
        loading = Loader.LoadAsync(type, path, method, null, null) | loading;
    }

    public void FinishLoadAsync(DelegateObjectLoaded onLoaded, object param = null)
    {
        Loader.LoadAsync(null, string.Empty, LoadMethod.Default, onLoaded, param);
    }

    /// <summary>
    /// 卸载一个资源
    /// </summary>
    /// <param name="assetPath">资源名称</param>
    public void Unload(string assetPath)
    {
        if (!string.IsNullOrEmpty(assetPath)) {
            Loader.Unload(assetPath, false);
        }
    }

    public static void GC()
    {
        ZFrame.LuaScriptMgr.Instance.L.GC(XLua.LuaGCOptions.LUA_GCCOLLECT, 0);
        //LuaEnv.Instance.L.GC(LuaInterface.LuaGCOptions.LUA_GCCOLLECT, 0);
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void AssignEditorShader(Material mat)
    {
#if !UNITY_STANDALONE
        if (A.useAssetBundleLoader) {
            if (mat && mat.shader) mat.shader = Shader.Find(mat.shader.name);
        }
#endif
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void AssignEditorShaders(GameObject go)
    {
#if !UNITY_STANDALONE
        if (A && A.useAssetBundleLoader) {
            var list = ZFrame.ListPool<Component>.Get();
            go.GetComponentsInChildren(typeof(Renderer), list, true);
            foreach (var rdr in list) {
                foreach (var m in ((Renderer)rdr).sharedMaterials) {
                    if (m && m.shader) m.shader = Shader.Find(m.shader.name);
                }
            }
            list.Clear();
            go.GetComponentsInChildren(typeof(Projector), list, true);
            foreach (var com in list) {
                var m = ((Projector)com).material;
                if (m && m.shader) m.shader = Shader.Find(m.shader.name);
            }
            ZFrame.ListPool<Component>.Release(list);
        }
#endif
    }
}