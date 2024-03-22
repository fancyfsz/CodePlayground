using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class PrefabLabelComponentFinder : EditorWindow
{
    private static string csvFilePath = "Assets/label_contentsizefitter.csv"; // CSV 文件路径
    
    [MenuItem("Tools/Find Prefabs with Label and ContentSizeFitter")]
    private static void FindPrefabsWithLabelAndContentSizeFitter()
    {
        var guids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (PrefabContainsLabelWithContentSizeFitter(prefab))
            {
                WriteToCSV($"{path}");
            }
        }
    }

    private static void WriteToCSV(string logText)
    {
        // 如果 CSV 文件不存在，则创建文件并写入标题行
        if (!File.Exists(csvFilePath))
        {
            using (StreamWriter sw = File.CreateText(csvFilePath))
            {
                sw.WriteLine("Message"); // 标题行
            }
        }

        // 追加日志到 CSV 文件
        using (StreamWriter sw = File.AppendText(csvFilePath))
        {
            sw.WriteLine(logText); // 写入消息内容
        }
    }
    
    private static bool PrefabContainsLabelWithContentSizeFitter(GameObject prefab)
    {
        if (prefab == null)
            return false;

        var labels = prefab.GetComponentsInChildren<Text>(true)
            .Concat(prefab.GetComponentsInChildren<LFTextMeshProUGUI>(true)
                .Concat(prefab.GetComponentsInChildren<TextMeshProUGUI>(true))
                .Concat(prefab.GetComponentsInChildren<TextPlus>(true)
                    .Cast<Component>()));

        return labels.Any(label => label.GetComponent<ContentSizeFitter>() != null);
    }
}