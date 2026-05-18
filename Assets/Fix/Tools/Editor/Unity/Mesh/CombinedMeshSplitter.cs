using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fix.Editor;

/// <summary>
/// 合并网格还原工具
/// 通过 MeshRenderer.m_StaticBatchInfo 从合并网格中提取原始网格
/// </summary>
public class CombinedMeshRestoreTool : FixEditorWindow
{
    private const string Title = "合并网格还原工具";

    [FixEditor(FixRoot + nameof(Mesh) + "/" + Title)]
    public static void ShowWindow()
    {
        GetWindowWithRect<CombinedMeshRestoreTool>(new Rect(Screen.width / 2, Screen.height / 2, 500, 500))
                .titleContent =
            new GUIContent(Title + " --Claude");
    }

    private string outputFolder = "Assets/Mesh/Restored";

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("合并网格还原工具", EditorStyles.boldLabel);

        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "此工具将扫描所有场景，找到使用 Combined Mesh 的对象，\n" +
            "从合并网格中提取原始网格，替换回 MeshFilter，\n" +
            "取消 Static 标记，并修正顶点到本地空间。", MessageType.Info);

        EditorGUILayout.Space(15);

        outputFolder = EditorGUILayout.TextField("网格保存目录", outputFolder);
        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "只会处理 BuildSettings 的场景",
            MessageType.None);   
        EditorGUILayout.Space(30);
        EditorGUILayout.HelpBox(
            "操作步骤: 提取子网格 → 保存网格资源 → 替换引用 → 取消Static → 修正Transform → 保存场景",
            MessageType.None);
        EditorGUILayout.Space(20);

        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("还原所有合并网格", GUILayout.Height(40)))
        {
            if (EditorUtility.DisplayDialog("确认",
                "此操作将修改所有场景中使用合并网格的对象。\n\n" +
                "建议先备份项目。是否继续?",
                "开始还原", "取消"))
            {
                RestoreAllCombinedMeshes();
            }
        }

        GUI.backgroundColor = Color.white;
    }

    private void RestoreAllCombinedMeshes()
    {
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        // 获取所有场景路径
        var scenePaths = EditorBuildSettings
            .scenes
            .Select(e=>e.path)
            .Where(File.Exists).ToList();

        int totalRestored = 0;
        int totalFailed = 0;
        int scenesModified = 0;

        // 用于网格去重: (combinedMesh, firstSubMesh, subMeshCount) -> 新网格
        var meshCache = new Dictionary<string, Mesh>();

        // 开始批量资源编辑，暂停自动导入以提高性能
        AssetDatabase.StartAssetEditing();

        try
        {
            for (int sceneIndex = 0; sceneIndex < scenePaths.Count; sceneIndex++)
            {
                string scenePath = scenePaths[sceneIndex];
                string sceneName = Path.GetFileNameWithoutExtension(scenePath);

                EditorUtility.DisplayProgressBar("还原合并网格",
                    $"[{sceneIndex + 1}/{scenePaths.Count}] {sceneName}",
                    (float) sceneIndex / scenePaths.Count);

                try
                {
                    // 打开场景
                    var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

                    bool sceneModified = false;
                    var rootObjects = scene.GetRootGameObjects();

                    foreach (var root in rootObjects)
                    {
                        var results = ProcessGameObject(root, meshCache);
                        totalRestored += results.restored;
                        totalFailed += results.failed;
                        if (results.restored > 0) sceneModified = true;
                    }

                    // 保存场景
                    if (sceneModified)
                    {
                        EditorSceneManager.SaveScene(scene);
                        scenesModified++;
                        Debug.Log($"已保存场景: {sceneName}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"处理场景失败 {scenePath}: {e.Message}");
                }
            }
        }
        finally
        {
            // 结束批量编辑，恢复自动导入
            AssetDatabase.StopAssetEditing();
        }

        EditorUtility.ClearProgressBar();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 显示结果
        string msg = $"还原完成!\n\n" +
                     $"• 成功还原: {totalRestored} 个网格\n" +
                     $"• 修改场景: {scenesModified} 个\n" +
                     $"• 网格保存到: {outputFolder}";

        if (totalFailed > 0)
        {
            msg += $"\n• 失败: {totalFailed} 个 (见Console)";
        }

        EditorUtility.DisplayDialog("完成", msg, "确定");
    }

    private (int restored, int failed) ProcessGameObject(GameObject root, Dictionary<string, Mesh> meshCache)
    {
        int restored = 0;
        int failed = 0;

        // 获取所有 MeshFilter
        var meshFilters = root.GetComponentsInChildren<MeshFilter>(true);

        foreach (var mf in meshFilters)
        {
            var mesh = mf.sharedMesh;
            if (mesh == null) continue;

            // 检查是否是合并网格
            if (!IsCombinedMesh(mesh)) continue;

            var mr = mf.GetComponent<MeshRenderer>();
            if (mr == null) continue;

            try
            {
                if (RestoreMesh(mf, mr, mesh, meshCache))
                {
                    restored++;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"还原失败 [{mf.gameObject.name}]: {e.Message}\n{e.StackTrace}");
                failed++;
            }
        }

        return (restored, failed);
    }

    private bool IsCombinedMesh(Mesh mesh)
    {
        if (mesh == null) return false;
        return mesh.name.StartsWith("Combined Mesh");
    }

    private bool RestoreMesh(MeshFilter mf, MeshRenderer mr, Mesh combinedMesh, Dictionary<string, Mesh> meshCache)
    {
        var go = mf.gameObject;

        // 1. 首先检查是否有 MeshCollider 可以复用网格
        // 只有当对象上只有一个 MeshCollider 时才复用
        var meshColliders = go.GetComponents<MeshCollider>();
        if (meshColliders.Length == 1 && meshColliders[0].sharedMesh != null)
        {
            Mesh colliderMesh = meshColliders[0].sharedMesh;

            // 如果 MeshCollider 的网格不是合并网格，直接复用
            if (!IsCombinedMesh(colliderMesh))
            {
                // 记录撤销
                Undo.RecordObject(mf, "Restore Mesh from Collider");
                Undo.RecordObject(go, "Remove Static");

                // 替换网格
                mf.sharedMesh = colliderMesh;

                // 清除静态批处理信息
                var so = new SerializedObject(mr);
                var staticBatchInfo = so.FindProperty("m_StaticBatchInfo");
                if (staticBatchInfo != null)
                {
                    staticBatchInfo.FindPropertyRelative("firstSubMesh").intValue = 0;
                    staticBatchInfo.FindPropertyRelative("subMeshCount").intValue = 0;
                    so.ApplyModifiedProperties();
                }

                // 取消 Static 标记
                GameObjectUtility.SetStaticEditorFlags(go, 0);

                EditorUtility.SetDirty(mf);
                EditorUtility.SetDirty(go);

                Debug.Log($"[{go.name}] 从 MeshCollider 复用网格: {colliderMesh.name}");
                return true;
            }
        }

        // 2. 获取静态批处理信息
        var serializedObj = new SerializedObject(mr);
        var batchInfo = serializedObj.FindProperty("m_StaticBatchInfo");

        if (batchInfo == null)
        {
            Debug.LogWarning($"[{go.name}] 找不到 StaticBatchInfo");
            return false;
        }

        int firstSubMesh = batchInfo.FindPropertyRelative("firstSubMesh").intValue;
        int subMeshCount = batchInfo.FindPropertyRelative("subMeshCount").intValue;

        // 如果没有有效的批处理信息，可能需要通过其他方式处理
        if (subMeshCount <= 0)
        {
            // 尝试使用材质数量作为 subMeshCount
            subMeshCount = mr.sharedMaterials.Length;
            if (subMeshCount <= 0) subMeshCount = 1;
        }

        // 3. 生成缓存键
        string cacheKey = $"{combinedMesh.GetInstanceID()}_{firstSubMesh}_{subMeshCount}";

        Mesh newMesh;

        // 4. 检查缓存
        if (!meshCache.TryGetValue(cacheKey, out newMesh))
        {
            // 4. 从合并网格提取子网格
            newMesh = ExtractMesh(combinedMesh, firstSubMesh, subMeshCount, go.transform);

            if (newMesh == null)
            {
                Debug.LogWarning($"[{go.name}] 提取网格失败");
                return false;
            }

            // 5. 保存网格资源
            string meshName = SanitizeName(go.name);
            newMesh.name = meshName;

            string savePath = AssetDatabase.GenerateUniqueAssetPath($"{outputFolder}/{meshName}.asset");
            AssetDatabase.CreateAsset(newMesh, savePath);

            meshCache[cacheKey] = newMesh;
        }

        // 6. 记录撤销
        Undo.RecordObject(mf, "Restore Mesh");
        Undo.RecordObject(go, "Remove Static");

        // 7. 替换网格
        mf.sharedMesh = newMesh;

        // 8. 清除静态批处理信息
        batchInfo.FindPropertyRelative("firstSubMesh").intValue = 0;
        batchInfo.FindPropertyRelative("subMeshCount").intValue = 0;
        serializedObj.ApplyModifiedProperties();

        // 9. 取消 Static 标记
        GameObjectUtility.SetStaticEditorFlags(go, 0);

        // 10. 标记已修改
        EditorUtility.SetDirty(mf);
        EditorUtility.SetDirty(go);

        return true;
    }

    private Mesh ExtractMesh(Mesh combinedMesh, int firstSubMesh, int subMeshCount, Transform transform)
    {
        // 验证参数
        if (firstSubMesh < 0) firstSubMesh = 0;
        if (subMeshCount <= 0) subMeshCount = 1;

        int lastSubMesh = Mathf.Min(firstSubMesh + subMeshCount - 1, combinedMesh.subMeshCount - 1);

        if (firstSubMesh >= combinedMesh.subMeshCount)
        {
            Debug.LogWarning($"SubMesh索引越界: firstSubMesh={firstSubMesh}, subMeshCount={combinedMesh.subMeshCount}");
            return null;
        }

        // 收集原始数据
        var srcVertices = combinedMesh.vertices;
        var srcNormals = combinedMesh.normals;
        var srcTangents = combinedMesh.tangents;
        var srcUV = combinedMesh.uv;
        var srcUV2 = combinedMesh.uv2;
        var srcColors = combinedMesh.colors;
        var srcColors32 = combinedMesh.colors32;

        // 收集所有需要的三角形索引
        var allTriangles = new List<int>();
        for (int i = firstSubMesh; i <= lastSubMesh; i++)
        {
            allTriangles.AddRange(combinedMesh.GetTriangles(i));
        }

        if (allTriangles.Count == 0)
        {
            Debug.LogWarning("没有找到三角形数据");
            return null;
        }

        // 构建顶点映射 (旧索引 -> 新索引)
        var vertexMap = new Dictionary<int, int>();
        var newVertices = new List<Vector3>();
        var newNormals = new List<Vector3>();
        var newTangents = new List<Vector4>();
        var newUV = new List<Vector2>();
        var newUV2 = new List<Vector2>();
        var newColors = new List<Color>();
        var newColors32 = new List<Color32>();
        var newTriangles = new List<int>();

        // 世界空间到本地空间的变换矩阵
        Matrix4x4 worldToLocal = transform.worldToLocalMatrix;

        // 检查是否需要翻转三角形绕序
        // 当变换矩阵的行列式为负时（包含奇数个负缩放轴），需要翻转
        bool flipWinding = worldToLocal.determinant < 0;

        foreach (int oldIdx in allTriangles)
        {
            if (!vertexMap.ContainsKey(oldIdx))
            {
                int newIdx = newVertices.Count;
                vertexMap[oldIdx] = newIdx;

                // 变换顶点位置 (合并网格的顶点是世界空间的)
                Vector3 worldPos = srcVertices[oldIdx];
                Vector3 localPos = worldToLocal.MultiplyPoint3x4(worldPos);
                newVertices.Add(localPos);

                // 变换法线 - 使用逆转置矩阵
                if (srcNormals != null && srcNormals.Length > oldIdx)
                {
                    Vector3 worldNormal = srcNormals[oldIdx];
                    // 对于法线，使用 MultiplyVector 然后归一化
                    // 如果有负缩放，法线需要反转
                    Vector3 localNormal = worldToLocal.MultiplyVector(worldNormal);
                    if (flipWinding)
                    {
                        localNormal = -localNormal;
                    }

                    newNormals.Add(localNormal.normalized);
                }

                // 变换切线
                if (srcTangents != null && srcTangents.Length > oldIdx)
                {
                    Vector4 worldTangent = srcTangents[oldIdx];
                    Vector3 tangentDir = new Vector3(worldTangent.x, worldTangent.y, worldTangent.z);
                    Vector3 localTangent = worldToLocal.MultiplyVector(tangentDir);
                    newTangents.Add(new Vector4(localTangent.normalized.x, localTangent.normalized.y,
                        localTangent.normalized.z, worldTangent.w));
                }

                // UV不需要变换
                if (srcUV != null && srcUV.Length > oldIdx)
                    newUV.Add(srcUV[oldIdx]);

                if (srcUV2 != null && srcUV2.Length > oldIdx)
                    newUV2.Add(srcUV2[oldIdx]);

                // 颜色不需要变换
                if (srcColors != null && srcColors.Length > oldIdx)
                    newColors.Add(srcColors[oldIdx]);

                if (srcColors32 != null && srcColors32.Length > oldIdx)
                    newColors32.Add(srcColors32[oldIdx]);
            }

            newTriangles.Add(vertexMap[oldIdx]);
        }

        // 如果需要翻转绕序，反转每个三角形的顶点顺序
        if (flipWinding)
        {
            for (int i = 0; i < newTriangles.Count; i += 3)
            {
                // 交换第二和第三个顶点
                int temp = newTriangles[i + 1];
                newTriangles[i + 1] = newTriangles[i + 2];
                newTriangles[i + 2] = temp;
            }
        }

        // 创建新网格
        var newMesh = new Mesh();
        newMesh.SetVertices(newVertices);

        if (newNormals.Count > 0)
            newMesh.SetNormals(newNormals);

        if (newTangents.Count > 0)
            newMesh.SetTangents(newTangents);

        if (newUV.Count > 0)
            newMesh.SetUVs(0, newUV);

        if (newUV2.Count > 0)
            newMesh.SetUVs(1, newUV2);

        if (newColors.Count > 0)
            newMesh.SetColors(newColors);
        else if (newColors32.Count > 0)
            newMesh.SetColors(newColors32);

        newMesh.SetTriangles(newTriangles, 0);
        newMesh.RecalculateBounds();

        // 如果没有法线，重新计算
        if (newNormals.Count == 0)
            newMesh.RecalculateNormals();

        return newMesh;
    }

    private static string SanitizeName(string name)
    {
        // 移除无效字符
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }

        // 移除空格和括号
        name = name.Replace(" ", "_").Replace("(", "").Replace(")", "");
        // 限制长度
        if (name.Length > 50) name = name.Substring(0, 50);
        return name;
    }
}