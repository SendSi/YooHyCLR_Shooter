# YooHyCLR
YooAsset学习_hybridCLR学习

## https://hybridclr.doc.code-philosophy.com/docs/beginner/quickstart

## https://www.yooasset.com/docs/guide-editor/QuickStart

## https://www.bilibili.com/video/BV1rc411Q73Q/?spm_id_from=333.999.0.0&vd_source=20561b00f1debfa5611eef8023c64796

1.看官方文档
2.代码分离
 		1.增加AOT文件夹.AssemblyDefinition.引用程序集.YooAsset.UniFramework._,HyBridCLR.Runtime,,,增加脚本CoreUtil.cs.增加单例.把原PatchLogic.StreamingAssetsHelper....等引用拖到AOT中来Boot也要拖在AOT中去
		2.其余就作为HotUpdate咯.增加HotUpdate.AssemblyDefinition 引用程序集
		3.解决各报错
		4.Boot.cs增加代码
3.HybridCLR生成All与目标平台
4.YooAsset打出资源		