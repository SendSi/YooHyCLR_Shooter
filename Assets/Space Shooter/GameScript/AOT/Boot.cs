﻿using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UniFramework.Event;
using YooAsset;
using System;
using HybridCLR;
using System.Linq;

public class Boot : MonoBehaviour
{
    /// <summary>
    /// 资源系统运行模式
    /// </summary>
    public EPlayMode PlayMode = EPlayMode.HostPlayMode;


    void Awake()
    {
        Debug.Log($"资源系统运行模式：{PlayMode}");
        Application.targetFrameRate = 60;
        Application.runInBackground = true;
        DontDestroyOnLoad(this.gameObject);
    }

    IEnumerator Start()
    {
        // 游戏管理器
        // CoreUtil.Instance.Behaviour = this;

        // 初始化事件系统
        UniEvent.Initalize();

        // 初始化资源系统
        YooAssets.Initialize();

        // 加载更新页面
        var go = Resources.Load<GameObject>("PatchWindow");
        GameObject.Instantiate(go);

        // 开始补丁更新流程
        PatchOperation operation = new PatchOperation("DefaultPackage",
            EDefaultBuildPipeline.BuiltinBuildPipeline.ToString(), PlayMode);
        YooAssets.StartOperation(operation);
        yield return operation;

        //todo  更新热更代码
        PatchOperation operation_hotFix = new PatchOperation("HotFixPackage",
            EDefaultBuildPipeline.RawFileBuildPipeline.ToString(), PlayMode);
        YooAssets.StartOperation(operation_hotFix);
        yield return operation_hotFix;

        //todo 加载元数据 和 热更代码
        yield return LoadHotFixRes();
        LoadMetadataForAOTAssemblies();


        // 设置默认的资源包
        var gamePackage = YooAssets.GetPackage("DefaultPackage");
        YooAssets.SetDefaultPackage(gamePackage);

        // 切换到主页面场景
        // SceneEventDefine.ChangeToHomeScene.SendEventMessage();

        //todo 反射调用入口 
        Type entryType = _hotUpdateAss.GetType("GameManager");
        entryType.GetMethod("LoadHomeScene").Invoke(null, null);
        Debug.LogError("反射调用入口 完成");
    }

    private IEnumerator LoadHotFixRes()
    {
        var hotfixPackage = YooAssets.GetPackage("HotFixPackage");
        foreach (var dll in AOTMetaAssemblyFiles)
        {
            var handle = hotfixPackage.LoadRawFileAsync($"Assets/Space Shooter/GameResHotFix/{dll}");
            yield return handle;
            var bytes = handle.GetRawFileData();
            s_assetDatas[dll] = bytes;
        }
    }

    private static Dictionary<string, byte[]> s_assetDatas = new Dictionary<string, byte[]>();
    private static Assembly _hotUpdateAss;

    private static List<string> AOTMetaAssemblyFiles { get; } = new List<string>()
    {
        "UniFramework.Event.dll.bytes",
        "UnityEngine.CoreModule.dll.bytes",
        "YooAsset.dll.bytes",
        "mscorlib.dll.bytes",

        "HotUpdate.dll.bytes",
    };

    /// <summary>
    /// 为aot assembly加载原始metadata， 这个代码放aot或者热更新都行。
    /// 一旦加载后，如果AOT泛型函数对应native实现不存在，则自动替换为解释模式执行
    /// </summary>
    private static void LoadMetadataForAOTAssemblies()
    {
        /// 注意，补充元数据是给AOT dll补充元数据，而不是给热更新dll补充元数据。
        /// 热更新dll不缺元数据，不需要补充，如果调用LoadMetadataForAOTAssembly会返回错误
        /// 
        HomologousImageMode mode = HomologousImageMode.SuperSet;
        foreach (var aotDllName in AOTMetaAssemblyFiles)
        {
            byte[] dllBytes = ReadBytesFromStreamingAssets(aotDllName);
            // 加载assembly对应的dll，会自动为它hook。一旦aot泛型函数的native函数不存在，用解释器版本代码
            LoadImageErrorCode err = RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, mode);
            Debug.Log($"LoadMetadataForAOTAssembly:{aotDllName}. mode:{mode} ret:{err}");
        }
#if !UNITY_EDITOR
         _hotUpdateAss = Assembly.Load(ReadBytesFromStreamingAssets("HotUpdate.dll.bytes"));
#else
        // Editor下无需加载，直接查找获得HotUpdate程序集
        _hotUpdateAss = System.AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "HotUpdate");
#endif
    }

    public static byte[] ReadBytesFromStreamingAssets(string dllName)
    {
        return s_assetDatas[dllName];
    }
}