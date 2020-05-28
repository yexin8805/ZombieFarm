﻿using UnityEngine;
using System.IO;
using System.Collections;
#if false
using LuaInterface;
using ILuaState = System.IntPtr;

public class LuaEnv 
{
    public LuaState ls;
    public ILuaState L { get { return ls.L; } }
    public ObjectTranslator U { get { return ls.translator; } }
    
    public int metaRef_Deletage { get; private set; }
     
    private static LuaEnv s_MultiEnv;
    public static LuaEnv Instance {  get { return s_MultiEnv;  } }
    public static LuaEnv Get(ILuaState L)
    {
        return s_MultiEnv;
    }

    private static readonly string LuaROOT = string.Format("{0}/Essets/LuaRoot", 
        Path.GetDirectoryName(Application.dataPath.Replace("\\", "/")));

    public static string GetFilePath(string path)
    {
        return string.IsNullOrEmpty(path) ? LuaROOT : string.Format("{0}/{1}", LuaROOT, path);
    }
    public static byte[] LoadFile(string file)
    {

#if UNITY_EDITOR
        if (AssetsMgr.A && AssetsMgr.A.printLoadedLuaStack) {
            LogMgr.D("Lua Loading: {0}", file);
            using (var wr = File.AppendText("lualoading.tmp")) {
                wr.WriteLine(file);
            }
            
        }
#endif
        byte[] nbytes = null;
        if (AssetsMgr.A.useLuaAssetBundle) {
            string assetbundleName = file.OrdinalStartsWith("config") ? "lua/config" : "lua/script";
            string assetName = file.Replace('/', '%');
            var txtAsset = AssetsMgr.A.Load<TextAsset>(assetbundleName + "/" + assetName, false);
            if (txtAsset == null) return null;

            nbytes = txtAsset.bytes;
            CLZF2.Decrypt(nbytes, nbytes.Length);
            nbytes = CLZF2.DllDecompress(nbytes);
        } else {
            if (!file.OrdinalEndsWith(".lua")) file = file + ".lua";
            var luaPath = GetFilePath(file);
            if (!File.Exists(luaPath)) return null;

            nbytes = File.ReadAllBytes(luaPath);
        }

        if (nbytes[0] == 0xEF && nbytes[1] == 0xBB && nbytes[2] == 0xBF) {
            // 去掉BOM头
            System.Array.Copy(nbytes, 3, nbytes, 0, nbytes.Length - 3);
        }

        return nbytes;
    }
    
    /// <summary>
    /// 从lua调用设置UserData的元表
    /// </summary>
    [MonoPInvokeCallback(typeof(LuaCSFunction))]
	private static int SetUDataMetaTable(ILuaState L)
    {
        var udata = L.ToUserData(1);
        if (udata == null) {
            return L.L_ArgError(1, string.Format("userdata expected, got {0}", L.Type(1)));
        }
        if (L.Type(2) != LuaTypes.LUA_TTABLE) {
            return L.L_ArgError(2, string.Format("table expected, got {0}", L.Type(2)));
        }

        L.PushValue(1);
        L.PushValue(2);
        L.SetMetaTable(1);

        return 1;
    }

    public LuaEnv()
    {
        s_MultiEnv = this;
        LuaStatic.Load = LoadFile;

        ls = new LuaState();

        // UserData元表设置方法 (不依赖debug库)
        L.PushCSharpFunction(SetUDataMetaTable);
        L.SetField(LuaIndexes.LUA_GLOBALSINDEX, "setudatametatable");

        LuaDLL.luaopen_cjson(L);
        L.SetField(LuaIndexes.LUA_GLOBALSINDEX, "cjson");

        MetaMethods.RegTableCall(L);
        PreBinding();
    }
    
    private void PreBinding()
    {
        System_Object.Wrap(L);
        System_MonoType.Wrap(L);
        System_Array.Wrap(L);
        System_Delegate.Wrap(L);

        System_Collections_IList.Wrap(L);
        System_Collections_IEnumerator.Wrap(L);

        UnityEngine_Vector2.Wrap(L);
        UnityEngine_Vector3.Wrap(L);
        UnityEngine_Vector4.Wrap(L);
        UnityEngine_Quaternion.Wrap(L);
        UnityEngine_Color.Wrap(L);
        UnityEngine_Bounds.Wrap(L);
    }
   
}
#endif