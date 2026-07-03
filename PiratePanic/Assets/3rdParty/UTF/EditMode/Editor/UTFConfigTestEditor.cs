using UnityEditor;
using UnityEngine;
using System.Collections.Generic;


// UTF框架配置读取工具（编辑器测试专用）
public static class UTFConfigTestEditor
{
    private const string ConfigRootPath = "Assets/PiratePanic/ScriptableObjects";

    /// <summary>
    /// 读取单个指定名称的配置.asset
    /// </summary>
    public static T GetSingleConfig<T>(string rootFolder,string configName) where T : ScriptableObject
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        string fullTypeName = typeof(T).FullName;
        // 全局搜索该类型 + 文件名模糊匹配
        string filter = $"t:{fullTypeName} {configName}";
        Debug.Log($"搜索过滤器：{filter}");

        string[] allGuids = AssetDatabase.FindAssets(filter);
        if (allGuids.Length == 0)
        {
            Debug.LogError($"【UTF配置测试】未找到名称包含 [{configName}]、类型 [{fullTypeName}] 的配置");
            return null;
        }

        // 筛选出属于目标根目录的资源
        List<string> targetGuidList = new List<string>();
        foreach (string guid in allGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.StartsWith($"{rootFolder}/"))
            {
                targetGuidList.Add(guid);
            }
        }

        if (targetGuidList.Count == 0)
        {
            Debug.LogError($"【UTF配置测试】存在同名资源，但不在根目录 {rootFolder} 下");
            return null;
        }

        // 取第一个匹配项
        string assetPath = AssetDatabase.GUIDToAssetPath(targetGuidList[0]);
        T config = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        if (config == null)
        {
            Debug.LogError($"【UTF配置测试】路径加载失败：{assetPath}");
            return null;
        }

        Debug.Log($"成功加载单个配置：{assetPath}");
        return config;
    }

    /// <summary>
    /// 获取指定根目录下所有T类型ScriptableObject
    /// </summary>
    public static List<T> GetAllConfig<T>(string rootFolder) where T : ScriptableObject
    {
        List<T> result = new List<T>();
        // AssetDatabase.Refresh();//增量刷新
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);//全量重新导入`Library`下所有变更资源，无视缓存标记

        // 全局搜索该类型全部资源
        string fullTypeName = typeof(T).FullName;
        string[] guids = AssetDatabase.FindAssets($"t:{fullTypeName}");

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            // 判断路径是否属于目标根目录（必须拼接 / 防止前缀匹配错误）
            if (assetPath.StartsWith($"{rootFolder}/"))
            {
                T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (asset != null)
                {
                    result.Add(asset);
                }
            }
        }
        return result;
    }

}