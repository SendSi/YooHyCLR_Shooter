// using System;
// using System.IO;
// using System.Linq;
// using System.Reflection;
// using UnityEngine;
//
// public class LoadDll : MonoBehaviour
// {
//     void Start()
//     {
//         // Editor环境下，HotUpdate.dll.bytes已经被自动加载，不需要加载，重复加载反而会出问题。
// #if !UNITY_EDITOR
//         Assembly hotUpdateAss =
//             Assembly.Load(File.ReadAllBytes($"{Application.streamingAssetsPath}/HotUpdate.dll.bytes"));
// #else
//         // Editor下无需加载，直接查找获得HotUpdate程序集
//         Assembly hotUpdateAss = System.AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "HotUpdate"); System.AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "HotUpdate");
// #endif
//
//         Type type = hotUpdateAss.GetType("Boot");
//         type.GetMethod("Run").Invoke(null, null);
//     }
// }


using HybridCLR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using YooAsset;

/// <summary>
/// 脚本工作流程：
/// 1.下载资源，用yooAsset资源框架进行下载
///    1.资源文件，ab包
///    2.热更新dll
///    3.AOT泛型补充元数据
/// 2.给AOT DLL补充元素据，通过RuntimeApi.LoadMetadataForAOTAssembly
/// 通过实例化prefab，运行热更代码
/// </summary>

// public class LoadDll : MonoBehaviour
// {
//     /// <summary>
//     /// 资源系统运行模式
//     /// </summary>
//     public EPlayMode PlayMode = EPlayMode.EditorSimulateMode;
//
//     void Start()
//     {
//         //StartCoroutine(DownLoadAssets(this.StartGame));
//         StartCoroutine(DownLoadAssetsByYooAssets(this.StartGame));
//     }
//
//     #region download assets
//
//     private static Dictionary<string, byte[]> s_assetDatas = new Dictionary<string, byte[]>();
//
//     public static byte[] ReadBytesFromStreamingAssets(string dllName)
//     {
//         return s_assetDatas[dllName];
//     }
//
//     private string GetWebRequestPath(string asset)
//     {
//         var path = $"{Application.streamingAssetsPath}/{asset}";
//         if (!path.Contains("://"))
//         {
//             path = "file://" + path;
//         }
//         return path;
//     }
//
//     //补充元数据dll的列表
//     //通过RuntimeApi.LoadMetadataForAOTAssembly()函数来补充AOT泛型的原始元数据
//     private static List<string> AOTMetaAssemblyFiles { get; } = new List<string>()
//     {
//         "mscorlib.dll",
//         "System.dll",
//         "System.Core.dll",
//     };
//
//     IEnumerator DownLoadAssetsByYooAssets(Action onDownloadComplete)
//     {
//         // 1.初始化资源系统
//         YooAssets.Initialize();
//
//         string packageName = "DefaultPackage";
//         var package = YooAssets.TryGetPackage(packageName);
//         if (package == null)
//         {
//             package = YooAssets.CreatePackage(packageName);
//             YooAssets.SetDefaultPackage(package);
//         }
//         if (PlayMode == EPlayMode.EditorSimulateMode)
//         {
//             //编辑器模拟模式
//             var initParameters = new EditorSimulateModeParameters();
//             initParameters.SimulateManifestFilePath = EditorSimulateModeHelper.SimulateBuild("DefaultPackage",string.Empty);
//             yield return package.InitializeAsync(initParameters);
//         }
//         else if (PlayMode == EPlayMode.HostPlayMode)
//         {
//             //联机运行模式
//             string defaultHostServer = GetHostServerURL();
//             string fallbackHostServer = GetHostServerURL();
//             var initParameters = new HostPlayModeParameters();
//             initParameters.BuildinQueryServices = new GameQueryServices(); //太空战机DEMO的脚本类，详细见StreamingAssetsHelper
//             //initParameters.DecryptionServices = new GameDecryptionServices();//这里的代码和官网上的代码有差别，官网的代码可能是旧版本的代码会报错已这里的代码为主
//             initParameters.DeliveryQueryServices = new DefaultDeliveryQueryServices();
//             initParameters.RemoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
//             var initOperation = package.InitializeAsync(initParameters);
//           
//             yield return initOperation;
//
//             if (initOperation.Status == EOperationStatus.Succeed)
//             {
//                 Debug.Log("资源包初始化成功！");
//             }
//             else
//             {
//                 Debug.LogError($"资源包初始化失败：{initOperation.Error}");
//             }
//  }
//         else if (PlayMode == EPlayMode.OfflinePlayMode)
//         {
//             //单机模式
//             var initParameters = new OfflinePlayModeParameters();
//             yield return package.InitializeAsync(initParameters);
//         }
//         else
//         {
//            // WebGL运行模式
//             string defaultHostServer = "http://127.0.0.1/CDN/WebGL/v1.0";
//             string fallbackHostServer = "http://127.0.0.1/CDN/WebGL/v1.0";
//             var initParameters = new WebPlayModeParameters();
//             initParameters.BuildinQueryServices = new GameQueryServices(); //太空战机DEMO的脚本类，详细见StreamingAssetsHelper
//             initParameters.RemoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
//             var initOperation = package.InitializeAsync(initParameters);
//             yield return initOperation;
//
//             if (initOperation.Status == EOperationStatus.Succeed)
//             {
//                 Debug.Log("资源包初始化成功！");
//             }
//             else
//             {
//                 Debug.LogError($"资源包初始化失败：{initOperation.Error}");
//             }
//         }
//         //2.获取资源版本
//         var operation = package.UpdatePackageVersionAsync();
//         yield return operation;
//
//         if (operation.Status != EOperationStatus.Succeed)
//         {
//             //更新失败
//             Debug.LogError(operation.Error);
//             yield break;
//         }
//         string packageVersion = operation.PackageVersion;
//         Debug.Log($"Updated package Version : {packageVersion}");
//
//         //3.更新补丁清单
//         // 更新成功后自动保存版本号，作为下次初始化的版本。
//         // 也可以通过operation.SavePackageVersion()方法保存。
//         bool savePackageVersion = true;
//         var operation2 = package.UpdatePackageManifestAsync(packageVersion, savePackageVersion);
//         yield return operation2;
//
//         if (operation2.Status != EOperationStatus.Succeed)
//         {
//             //更新失败
//             Debug.LogError(operation2.Error);
//             yield break;
//         }
//
//         //4.下载补丁包
//         yield return Download();
//         //判断是否下载成功
//         var assets = new List<string>
//         {
//             "HotUpdate.dll"
//         }.Concat(AOTMetaAssemblyFiles);
//         foreach (var asset in assets)
//         {
//             //加载原生文件
//             var handle = package.LoadRawFileAsync(asset);
//             yield return handle;
//             byte[] fileData = handle.GetRawFileData();
//             //string fileText = handle4.GetRawFileText();
//             //string filePath = handle4.GetRawFilePath();
//             s_assetDatas[asset] = fileData;
//             Debug.Log($"dll:{asset} size:{fileData.Length}");
//         }
//         onDownloadComplete();
//     }
//
//     IEnumerator DownLoadAssets(Action onDownloadComplete)
//     {
//         var assets = new List<string>
//         {
//             "prefabs",
//             "HotUpdate.dll.bytes",
//         }.Concat(AOTMetaAssemblyFiles);
//
//         foreach (var asset in assets)
//         {
//             string dllPath = GetWebRequestPath(asset);
//             Debug.Log($"start download asset:{dllPath}");
//             UnityWebRequest www = UnityWebRequest.Get(dllPath);
//             yield return www.SendWebRequest();
//
// #if UNITY_2020_1_OR_NEWER
//             if (www.result != UnityWebRequest.Result.Success)
//             {
//                 Debug.Log(www.error);
//             }
// #else
//             if (www.isHttpError || www.isNetworkError)
//             {
//                 Debug.Log(www.error);
//             }
// #endif
//             else
//             {
//                 // Or retrieve results as binary data
//                 byte[] assetData = www.downloadHandler.data;
//                 Debug.Log($"dll:{asset}  size:{assetData.Length}");
//                 s_assetDatas[asset] = assetData;
//             }
//         }
//
//         onDownloadComplete();
//     }
//
//     IEnumerator Download()
//     {
//         int downloadingMaxNum = 10;
//         int failedTryAgain = 3;
//         var package = YooAssets.GetPackage("DefaultPackage");
//         var downloader = package.CreateResourceDownloader(downloadingMaxNum, failedTryAgain);
//
//         //没有需要下载的资源
//         if (downloader.TotalDownloadCount == 0)
//         {
//             yield break;
//         }
//
//         //需要下载的文件总数和总大小
//         int totalDownloadCount = downloader.TotalDownloadCount;
//         long totalDownloadBytes = downloader.TotalDownloadBytes;
//
//         //注册回调方法
//         downloader.OnDownloadErrorCallback = OnDownloadErrorFunction;
//         downloader.OnDownloadProgressCallback = OnDownloadProgressUpdateFunction;
//         downloader.OnDownloadOverCallback = OnDownloadOverFunction;
//         downloader.OnStartDownloadFileCallback = OnStartDownloadFileFunction;
//
//         //开启下载
//         downloader.BeginDownload();
//         yield return downloader;
//
//         //检测下载结果
//         if (downloader.Status == EOperationStatus.Succeed)
//         {
//             //下载成功
//             Debug.Log("更新完成");
//         }
//         else
//         {
//             //下载失败
//             Debug.Log("更新失败");
//         }
//     }
//
//     /// <summary>
//     /// 开始下载
//     /// </summary>
//     /// <param name="fileName"></param>
//     /// <param name="sizeBytes"></param>
//     private void OnStartDownloadFileFunction(string fileName, long sizeBytes)
//     {
//         Debug.Log(string.Format("开始下载：文件名：{0}，文件大小：{1}", fileName, sizeBytes));
//     }
//
//     /// <summary>
//     /// 下载完成
//     /// </summary>
//     /// <param name="isSucceed"></param>
//     private void OnDownloadOverFunction(bool isSucceed)
//     {
//         Debug.Log("下载" + (isSucceed ? "成功" : "失败"));
//     }
//
//     /// <summary>
//     /// 更新中
//     /// </summary>
//     /// <param name="totalDownloadCount"></param>
//     /// <param name="currentDownloadCount"></param>
//     /// <param name="totalDownloadBytes"></param>
//     /// <param name="currentDownloadBytes"></param>
//     private void OnDownloadProgressUpdateFunction(int totalDownloadCount, int currentDownloadCount, long totalDownloadBytes, long currentDownloadBytes)
//     {
//         Debug.Log(string.Format("文件总数：{0}，已下载文件数：{1}，下载总大小：{2}，已下载大小{3}", totalDownloadCount, currentDownloadCount, totalDownloadBytes, currentDownloadBytes));
//     }
//
//     /// <summary>
//     /// 下载出错
//     /// </summary>
//     /// <param name="fileName"></param>
//     /// <param name="error"></param>
//     private void OnDownloadErrorFunction(string fileName, string error)
//     {
//         Debug.Log(string.Format("下载出错：文件名：{0}，错误信息：{1}", fileName, error));
//     }
//
//     private string GetHostServerURL()
//     {
//         //string hostServerIP = "http://10.0.2.2"; //安卓模拟器地址
//         string hostServerIP = "http://127.0.0.1";
//         string appVersion = "v1.5";
// //资源地址自行修改
// #if UNITY_EDITOR
//         if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.Android)
//             return $"{hostServerIP}/CDN/Android/{appVersion}";
//         else if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.iOS)
//             return $"{hostServerIP}/CDN/IPhone/{appVersion}";
//         else if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.WebGL)
//             return $"{hostServerIP}/CDN/WebGL/{appVersion}";
//         else
//             return $"{hostServerIP}/CDN/PC/Package";
// #else
// 		if (Application.platform == RuntimePlatform.Android)
// 			return $"{hostServerIP}/CDN/Android/{appVersion}";
// 		else if (Application.platform == RuntimePlatform.IPhonePlayer)
// 			return $"{hostServerIP}/CDN/IPhone/{appVersion}";
// 		else if (Application.platform == RuntimePlatform.WebGLPlayer)
// 			return $"{hostServerIP}/CDN/WebGL/{appVersion}";
// 		else
// 			return $"{hostServerIP}/CDN/PC/Package";
// #endif
//     }
//
//     /// <summary>
//     /// 远端资源地址查询服务类
//     /// </summary>
//     private class RemoteServices : IRemoteServices
//     {
//         private readonly string _defaultHostServer;
//         private readonly string _fallbackHostServer;
//
//         public RemoteServices(string defaultHostServer, string fallbackHostServer)
//         {
//             _defaultHostServer = defaultHostServer;
//             _fallbackHostServer = fallbackHostServer;
//         }
//         string IRemoteServices.GetRemoteMainURL(string fileName)
//         {
//             return $"{_defaultHostServer}/{fileName}";
//         }
//         string IRemoteServices.GetRemoteFallbackURL(string fileName)
//         {
//             return $"{_fallbackHostServer}/{fileName}";
//         }
//     }
//
//
//     #endregion
//
//     private static Assembly _hotUpdateAss;
//
//     /// <summary>
//     /// 为aot assembly加载原始metadata， 这个代码放aot或者热更新都行。
//     /// 一旦加载后，如果AOT泛型函数对应native实现不存在，则自动替换为解释模式执行
//     /// </summary>
//     private static void LoadMetadataForAOTAssemblies()
//     {
//         /// 注意，补充元数据是给AOT dll补充元数据，而不是给热更新dll补充元数据。
//         /// 热更新dll不缺元数据，不需要补充，如果调用LoadMetadataForAOTAssembly会返回错误
//         /// 
//         HomologousImageMode mode = HomologousImageMode.SuperSet;
//         foreach (var aotDllName in AOTMetaAssemblyFiles)
//         {
//             byte[] dllBytes = ReadBytesFromStreamingAssets(aotDllName);
//             // 加载assembly对应的dll，会自动为它hook。一旦aot泛型函数的native函数不存在，用解释器版本代码
//             LoadImageErrorCode err = RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, mode);
//             Debug.Log($"LoadMetadataForAOTAssembly:{aotDllName}. mode:{mode} ret:{err}");
//         }
//     }
//
//     void StartGame()
//     {
//         LoadMetadataForAOTAssemblies();
// #if !UNITY_EDITOR
//         _hotUpdateAss = Assembly.Load(ReadBytesFromStreamingAssets("HotUpdate.dll"));
// #else
//         _hotUpdateAss = System.AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "HotUpdate");
// #endif
//         //Type entryType = _hotUpdateAss.GetType("Entry");
//         //entryType.GetMethod("Start").Invoke(null, null);
//
//         Run_InstantiateComponentByAsset();
//     }
//
//     private void Run_InstantiateComponentByAsset()
//     {
//         // 通过实例化assetbundle中的资源，还原资源上的热更新脚本
//         //AssetBundle ab = AssetBundle.LoadFromMemory(LoadDll.ReadBytesFromStreamingAssets("prefabs"));
//         var package = YooAssets.GetPackage("DefaultPackage");
//         //GameObject cube = ab.LoadAsset<GameObject>("Cube");
//         var handle = package.LoadAssetAsync<GameObject>("Cube");
//         handle.Completed += Handle_Completed;
//         //GameObject.Instantiate(cube);
//     }
//     private void Handle_Completed(AssetHandle obj)
//     {
//         GameObject go = obj.InstantiateSync();
//         Debug.Log($"Prefab name is {go.name}");
//     }
//     /// <summary>
//     /// 默认的分发资源查询服务类
//     /// </summary>
//     private class DefaultDeliveryQueryServices : IDeliveryQueryServices
//     {
//         public bool Query(string packageName, string fileName)
//         {
//             return false;
//         }
//         public string GetFilePath(string packageName, string fileName)
//         {
//             return string.Empty;
//         }
//     }
// }
//代码自行整理
//PS:版本不同可能有一些类名发生变化，请参照现阶段版本自行修改，官网可能更新不及时。
