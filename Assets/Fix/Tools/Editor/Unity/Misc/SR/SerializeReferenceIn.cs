using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Fix.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>
/// SerializeReference 导入功能 - 编辑器环境
/// </summary>
public partial class SRIO
{
    [InitializeOnLoadMethod]
    private static void InitEditor()
    {
        var scriptPath = AssetDatabase.FindAssets("t:MonoScript SerializeReferenceOut")
            .Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault();
        if (string.IsNullOrWhiteSpace(scriptPath)) return;

        var text = File.ReadAllText(scriptPath);
        var folder = "SR".GetWorkspaceFolder();
        var s = Regex.Replace(text, @"(?<=private const string OutputPath \= \@\"")(.*?)(?=""\;)",
            Path.GetFullPath(folder));
        if (text == s) return;
        folder.Mkdir();
        File.WriteAllText(scriptPath, s);
    }

    private class FixBridge : FixEditorBase
    {
        [FixEditor(FixEditorConst.FixRoot + "SerializeReference/导入序列化引用")]
        public static void Bridge()
        {
            SRIO.Import();
        }
    }

    #region Import Constants

    private const string RefPrefix = "@ref:";
    private const string TypePrefix = "$type";

    #endregion

    #region Import Data

    private static Dictionary<string, Type> locateToType;
    private static Dictionary<string, Object> locateToObject;
    private static Dictionary<string, string> refIdToLocate;

    #endregion


    /// <summary>
    /// 导入入口点 - 编辑器菜单
    /// </summary>
    public static void Import()
    {
        string inputPath = Path.Combine(OutputPath, SRName);

        if (!File.Exists(inputPath))
        {
            Debug.LogError($"[SerializeReferenceIn] 文件不存在: {inputPath}");
            return;
        }

        try
        {
            string content = File.ReadAllText(inputPath);
            ImportFromContent(content);
            Debug.Log("[SerializeReferenceIn] 导入完成!");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    /// <summary>
    /// 从内容字符串导入
    /// </summary>
    private static void ImportFromContent(string content)
    {
        locateToType = new Dictionary<string, Type>();
        locateToObject = new Dictionary<string, Object>();
        refIdToLocate = new Dictionary<string, string>();

        var lines = content.Split(new[] {"\r\n", "\n"}, StringSplitOptions.None);
        int lineIndex = 0;


        while (lineIndex < lines.Length)
        {
            string line = lines[lineIndex].Trim();

            if (line.StartsWith(ScenePrefix))
            {
                string sceneLocate = line.Substring(ScenePrefix.Length).Trim();
                lineIndex++;
                ProcessSceneSection(lines, ref lineIndex, sceneLocate);
            }
            else if (line.StartsWith(PrefabPrefix))
            {
                string prefabLocate = line.Substring(PrefabPrefix.Length).Trim();
                lineIndex++;
                ProcessPrefabSection(lines, ref lineIndex, prefabLocate);
            }
            else if (line.StartsWith(ScriptableObjectPrefix))
            {
                string soLocate = line.Substring(ScriptableObjectPrefix.Length).Trim();
                lineIndex++;
                ProcessScriptableObjectSection(lines, ref lineIndex, soLocate);
            }
            else if (line.StartsWith(CodecPrefix))
            {
                // 解析 Codec 部分
                lineIndex++;
                ParseCodecSection(lines, ref lineIndex);
            }
            else
            {
                lineIndex++;
            }
        }

        // 保存所有修改
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// 解析 Codec 部分
    /// </summary>
    private static void ParseCodecSection(string[] lines, ref int lineIndex)
    {
        // 解析类型映射
        while (lineIndex < lines.Length)
        {
            string line = lines[lineIndex].Trim();
            if (line == EndMark)
            {
                lineIndex++;
                break;
            }

            // 兼容两种格式:
            // 旧格式: locate|typeName, assemblyName
            // 新格式: id|locate|typeName, assemblyName
            var parts = line.Split(new[] {'|'}, 3);
            if (parts.Length == 2)
            {
                RegisterCodec(parts[0], parts[1]);
            }
            else if (parts.Length == 3)
            {
                string refId = parts[0];
                string locate = parts[1];
                string typeInfo = parts[2];

                if (!string.IsNullOrEmpty(refId) && !string.IsNullOrEmpty(locate))
                    refIdToLocate[refId] = locate;
                RegisterCodec(locate, typeInfo);
            }

            lineIndex++;
        }
    }

    private static void RegisterCodec(string locate, string typeInfo)
    {
        if (string.IsNullOrEmpty(locate) || string.IsNullOrEmpty(typeInfo))
            return;

        Type type = Type.GetType(typeInfo) ?? ResolveType(typeInfo);
        if (type != null)
        {
            locateToType[locate] = type;
        }
        else
        {
            Debug.LogError($"[SerializeReferenceIn] 无法解析类型: {typeInfo}");
        }
    }

    /// <summary>
    /// 处理场景部分
    /// </summary>
    private static void ProcessSceneSection(string[] lines, ref int lineIndex, string sceneLocate)
    {
        // 现在 sceneLocate 直接就是 path，或者包含 @buildIndex (旧格式兼容)
        // 但根据新的 Output，它只是 source.path
        string scenePath = sceneLocate;
        int atIndex = sceneLocate.LastIndexOf('@');
        if (atIndex > 0) // 如果还是旧格式或者是带有 @buildIndex 的形式
        {
            // 检查是否真的是 index (简单的判断)
            if (int.TryParse(sceneLocate.Substring(atIndex + 1), out _))
                scenePath = sceneLocate.Substring(0, atIndex);
        }

        // 打开场景
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError($"[SerializeReferenceIn] 无法打开场景: {scenePath}");
            SkipToEndMark(lines, ref lineIndex);
            return;
        }

        // 缓存场景中的对象
        CacheSceneObjects(scene);

        // 处理对象条目
        ProcessObjectEntries(lines, ref lineIndex);

        // 保存场景
        EditorSceneManager.SaveScene(scene);
    }

    /// <summary>
    /// 处理 Prefab 部分
    /// </summary>
    private static void ProcessPrefabSection(string[] lines, ref int lineIndex, string prefabLocate)
    {
        // Prefab 的 locate 可能是层级路径，需要特殊处理
        // 这里简化处理：跳过 Prefab 导入（Prefab 通常需要通过 AssetDatabase 加载）
        SkipToEndMark(lines, ref lineIndex);
    }

    /// <summary>
    /// 处理 ScriptableObject 部分
    /// </summary>
    private static void ProcessScriptableObjectSection(string[] lines, ref int lineIndex, string soLocate)
    {
        if (TryResolveScriptableObjectReference(soLocate, out Object resolved) && resolved is ScriptableObject so)
        {
            locateToObject[soLocate] = so;
            ProcessObjectEntries(lines, ref lineIndex);
            return;
        }

        Debug.LogError($"[SerializeReferenceIn] 无法找到 ScriptableObject: {soLocate}, 跳过处理。");
        SkipToEndMark(lines, ref lineIndex);
    }

    /// <summary>
    /// 缓存场景中的对象用于快速查找
    /// </summary>
    private static void CacheSceneObjects(Scene scene)
    {
        locateToObject.Clear();

        var rootObjects = scene.GetRootGameObjects();
        var rootNameCounts = new Dictionary<string, int>();

        foreach (var root in rootObjects)
        {
            string rootName = root.name;
            if (rootNameCounts.ContainsKey(rootName))
            {
                rootNameCounts[rootName]++;
                rootName = $"{rootName}:{rootNameCounts[rootName]}";
            }
            else
            {
                rootNameCounts[rootName] = 0;
            }

            CacheGameObjectHierarchy(root, rootName);
        }
    }

    /// <summary>
    /// 递归缓存 GameObject 层级
    /// </summary>
    private static void CacheGameObjectHierarchy(GameObject go, string path)
    {
        locateToObject[path] = go;

        // 缓存组件
        // Locate格式: path@ComponentType:Index (如果Index>0)
        var components = go.GetComponents<Component>();
        var fullTypeCounts = new Dictionary<string, int>(); // 用于计算总数以决定是否需要Index后缀

        // 预计算每种类型的数量
        foreach (var c in components)
        {
            if (c == null) continue;
            var typeName = c.GetType().FullName;
            if (!fullTypeCounts.ContainsKey(typeName)) fullTypeCounts[typeName] = 0;
            fullTypeCounts[typeName]++;
        }

        var currentIndexes = new Dictionary<string, int>();

        foreach (var comp in components)
        {
            if (comp == null) continue;

            var compType = comp.GetType();
            var typeName = compType.FullName;

            if (!currentIndexes.ContainsKey(typeName)) currentIndexes[typeName] = 0;
            int myIndex = currentIndexes[typeName]++;
            int total = fullTypeCounts[typeName];

            string compLocate;
            if (total > 1)
                compLocate = $"{path}@{typeName}:{myIndex}";
            else
                compLocate = $"{path}@{typeName}";

            locateToObject[compLocate] = comp;
        }

        // 递归处理子对象
        // 需要计算子对象的同名索引
        var childNameCounts = new Dictionary<string, int>();
        for (int i = 0; i < go.transform.childCount; i++)
        {
            var child = go.transform.GetChild(i).gameObject;
            string childName = child.name;

            // 检查之前有多少个同名的兄弟
            // 注意：这里需要准确复刻 GetLocate 的逻辑。
            // GetLocate中：根据 child 在所有子物体中同名的位置来确定索引。

            // 为了简化，我们按顺序遍历，并计数
            if (childNameCounts.ContainsKey(childName))
            {
                childNameCounts[childName]++;
                childName = $"{childName}:{childNameCounts[childName]}";
            }
            else
            {
                childNameCounts[childName] = 0;
            }

            CacheGameObjectHierarchy(child, $"{path}/{childName}");
        }
    }

    /// <summary>
    /// 处理对象条目
    /// </summary>
    private static void ProcessObjectEntries(string[] lines, ref int lineIndex)
    {
        var pendingEntryLines = new List<string>();

        while (lineIndex < lines.Length)
        {
            string line = lines[lineIndex];
            string trimmed = line.Trim();

            if (trimmed == EndMark)
            {
                ApplyPendingObjectEntries(pendingEntryLines);
                lineIndex++;
                break;
            }

            if (trimmed.StartsWith(CodecPrefix))
            {
                // 先解析 codec，拿到 id -> locate 映射后再处理前面缓存的条目
                lineIndex++;
                ParseCodecSection(lines, ref lineIndex);
                ApplyPendingObjectEntries(pendingEntryLines);
                break;
            }

            pendingEntryLines.Add(line);
            lineIndex++;
        }

        // 容错：当段末没有 codec 时也尝试处理
        if (pendingEntryLines.Count > 0 && (lineIndex >= lines.Length || lines[lineIndex - 1].Trim() != EndMark))
            ApplyPendingObjectEntries(pendingEntryLines);
    }

    private static void ApplyPendingObjectEntries(List<string> pendingEntryLines)
    {
        foreach (var rawLine in pendingEntryLines)
            ProcessSingleObjectEntry(rawLine);
        pendingEntryLines.Clear();
    }

    private static void ProcessSingleObjectEntry(string rawLine)
    {
        // 还原转义的换行符
        string line = rawLine.Replace("\\r", "\r").Replace("\\n", "\n");

        // 格式: locate#json
        int separatorIndex = line.IndexOf('#');
        if (separatorIndex <= 0) return;

        string objLocate = line.Substring(0, separatorIndex);
        string jsonData = line.Substring(separatorIndex + 1);

        // 查找对象
        if (locateToObject.TryGetValue(objLocate, out Object targetObj))
        {
            DeserializeToObject(targetObj, jsonData);
        }
        else
        {
            if (TryResolveReferenceObject(objLocate, out targetObj))
            {
                locateToObject[objLocate] = targetObj;
                DeserializeToObject(targetObj, jsonData);
            }
            else
            {
                Debug.LogError($"[SerializeReferenceIn] 找不到对象: {objLocate}");
            }
        }
    }

    /// <summary>
    /// 反序列化 JSON 到对象
    /// </summary>
    private static void DeserializeToObject(Object targetObj, string jsonData)
    {
        if (string.IsNullOrEmpty(jsonData) || jsonData == "null" || jsonData == "{}")
            return;

        try
        {
            var jsonDict = ParseSimpleJson(jsonData);
            foreach (var kvp in jsonDict)
            {
                string propertyPath = kvp.Key;
                object value = kvp.Value;

                Type fieldType = GetFieldType(targetObj, propertyPath);
                if (fieldType == null) continue;

                object deserializedValue = DeserializeValue(value, fieldType);
                var fieldInfo = new SRFieldInfo(propertyPath);
                fieldInfo.SetValue(targetObj, deserializedValue);
            }

            if (targetObj is ISerializationCallbackReceiver callbackReceiver)
            {
                callbackReceiver.OnAfterDeserialize();
            }

            EditorUtility.SetDirty(targetObj);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SerializeReferenceIn] 反序列化失败: {e.Message}");
        }
    }

    /// <summary>
    /// 获取字段类型
    /// </summary>
    private static Type GetFieldType(Object obj, string propertyPath)
    {
        var type = obj.GetType();
        var parts = propertyPath.Split('.');

        foreach (var part in parts)
        {
            if (part == "Array") continue;

            if (part.StartsWith("data["))
            {
                if (type.IsArray)
                {
                    type = type.GetElementType();
                }
                else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    type = type.GetGenericArguments()[0];
                }

                continue;
            }

            var field = GetFieldRecursive(type, part);
            if (field != null)
            {
                type = field.FieldType;
            }
            else
            {
                return null;
            }
        }

        return type;
    }

    /// <summary>
    /// 反序列化值
    /// </summary>
    private static object DeserializeValue(object value, Type targetType)
    {
        if (value == null) return null;
        if (targetType == null) return value;

        // 处理引用
        if (value is string strValue)
        {
            if (strValue.StartsWith(RefPrefix))
            {
                string refToken = strValue.Substring(RefPrefix.Length);
                string locate = refToken;

                if (refIdToLocate != null && refIdToLocate.TryGetValue(refToken, out string mappedLocate))
                    locate = mappedLocate;

                // 检查无效引用 (如 -1@...)
                if (locate.StartsWith("-1@") || locate.StartsWith("-1/"))
                {
                    return null;
                }

                if (TryResolveReferenceObject(locate, out Object refObj))
                {
                    locateToObject[locate] = refObj;
                    return refObj;
                }

                Debug.LogError($"[SerializeReferenceIn] 找不到引用对象: {locate}");
                return null;
            }

            // 处理字符串类型
            if (targetType == typeof(string)) return strValue;

            // 处理枚举
            if (targetType.IsEnum)
            {
                try
                {
                    return Enum.Parse(targetType, strValue);
                }
                catch
                {
                    Debug.LogWarning($"[SerializeReferenceIn] 无法解析枚举: {strValue} -> {targetType}");
                    return Activator.CreateInstance(targetType);
                }
            }
        }

        // 处理基本类型
        if (targetType == typeof(string)) return value?.ToString();
        if (targetType == typeof(int)) return Convert.ToInt32(value);
        if (targetType == typeof(float)) return Convert.ToSingle(value);
        if (targetType == typeof(double)) return Convert.ToDouble(value);
        if (targetType == typeof(bool)) return Convert.ToBoolean(value);
        if (targetType == typeof(long)) return Convert.ToInt64(value);
        if (targetType == typeof(short)) return Convert.ToInt16(value);
        if (targetType == typeof(byte)) return Convert.ToByte(value);
        if (targetType == typeof(uint)) return Convert.ToUInt32(value);
        if (targetType == typeof(ulong)) return Convert.ToUInt64(value);

        // 处理数组/列表
        if (value is List<object> list)
        {
            if (targetType.IsArray)
            {
                var elementType = targetType.GetElementType();
                var array = Array.CreateInstance(elementType, list.Count);
                for (int i = 0; i < list.Count; i++)
                {
                    var element = DeserializeValue(list[i], elementType);
                    array.SetValue(element, i);
                }

                return array;
            }
            else if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var elementType = targetType.GetGenericArguments()[0];
                var newList = (System.Collections.IList) Activator.CreateInstance(targetType);
                foreach (var item in list)
                {
                    var element = DeserializeValue(item, elementType);
                    newList.Add(element);
                }

                return newList;
            }
            else
            {
                var projected = new List<object>(list.Count);
                foreach (var item in list)
                {
                    projected.Add(DeserializeValue(item, targetType));
                }

                return projected;
            }
        }

        // 处理复杂对象 (Dictionary)
        if (value is Dictionary<string, object> dict)
        {
            Type actualType = targetType;

            // 检查是否有 $type 字段 (多态类型)
            if (dict.TryGetValue(TypePrefix, out object typeNameObj) && typeNameObj is string typeName)
            {
                var resolvedType = ResolveType(typeName);
                if (resolvedType != null)
                {
                    actualType = resolvedType;
                }
                else
                {
                    Debug.LogError($"[SerializeReferenceIn] 无法解析类型: {typeName}");
                }
            }

            // 处理特殊 Unity 类型
            if (typeof(Vector2) == actualType || actualType.FullName == "UnityEngine.Vector2")
            {
                return DeserializeVector2(dict);
            }

            if (typeof(Vector3) == actualType || actualType.FullName == "UnityEngine.Vector3")
            {
                return DeserializeVector3(dict);
            }

            if (typeof(Vector4) == actualType || actualType.FullName == "UnityEngine.Vector4")
            {
                return DeserializeVector4(dict);
            }

            if (typeof(Color) == actualType || actualType.FullName == "UnityEngine.Color")
            {
                return DeserializeColor(dict);
            }

            if (typeof(AnimationCurve) == actualType || actualType.FullName == "UnityEngine.AnimationCurve")
            {
                // AnimationCurve 较复杂，返回空曲线
                return new AnimationCurve();
            }

            // 处理接口或抽象类型
            if (actualType.IsInterface || actualType.IsAbstract)
            {
                if (!dict.TryGetValue(TypePrefix, out _))
                {
                    Debug.LogWarning($"[SerializeReferenceIn] 抽象类型无 $type: {actualType}");
                    return null;
                }
            }

            // 尝试创建实例
            try
            {
                var instance = CreateInstance(actualType);
                if (instance == null) return null;

                foreach (var kvp in dict)
                {
                    if (kvp.Key == TypePrefix) continue;

                    var field = GetFieldRecursive(actualType, kvp.Key);
                    if (field != null)
                    {
                        try
                        {
                            var fieldValue = DeserializeValue(kvp.Value, field.FieldType);
                            field.SetValue(instance, fieldValue);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[SerializeReferenceIn] 设置字段失败 {kvp.Key}: {ex.Message}");
                        }
                    }
                }

                if (instance is ISerializationCallbackReceiver callbackReceiver)
                {
                    callbackReceiver.OnAfterDeserialize();
                }

                return instance;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SerializeReferenceIn] 创建实例失败 {actualType}: {ex.Message}");
                return null;
            }
        }

        return value;
    }

    /// <summary>
    /// 解析类型字符串
    /// </summary>
    private static Type ResolveType(string typeName)
    {
        if (string.IsNullOrEmpty(typeName)) return null;

        // 尝试直接解析
        var type = Type.GetType(typeName);
        if (type != null) return type;

        // 尝试从所有程序集中查找
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic) continue;

            try
            {
                // 尝试完整类型名
                type = assembly.GetType(typeName.Split(',')[0].Trim());
                if (type != null) return type;
            }
            catch
            {
            }
        }

        return null;
    }

    private static bool TryResolveReferenceObject(string locate, out Object resolved)
    {
        resolved = null;
        if (string.IsNullOrEmpty(locate)) return false;

        var candidates = EnumerateLocateCandidates(locate);

        foreach (var candidate in candidates)
        {
            if (locateToObject.TryGetValue(candidate, out resolved) && resolved != null)
                return true;
        }

        foreach (var candidate in candidates)
        {
            if (TryResolveSceneObjectReference(candidate, out resolved) ||
                TryResolveScriptableObjectReference(candidate, out resolved) ||
                TryResolveGenericObjectReference(candidate, out resolved) ||
                TryResolvePrefabObjectReference(candidate, out resolved))
            {
                locateToObject[locate] = resolved;
                locateToObject[candidate] = resolved;
                return true;
            }
        }

        return false;
    }

    private static List<string> EnumerateLocateCandidates(string locate)
    {
        var results = new List<string>();

        void AddCandidate(string candidate)
        {
            if (string.IsNullOrEmpty(candidate)) return;
            if (!results.Contains(candidate))
                results.Add(candidate);
        }

        AddCandidate(locate);
        AddCandidate(StripZeroComponentIndex(locate));

        var noPathZeroIndex = StripZeroIndexFromObjectPath(locate);
        AddCandidate(noPathZeroIndex);
        AddCandidate(StripZeroComponentIndex(noPathZeroIndex));

        return results;
    }

    private static string StripZeroIndexFromObjectPath(string locate)
    {
        if (string.IsNullOrEmpty(locate)) return locate;
        if (locate.StartsWith("@")) return locate;

        int atIndex = locate.LastIndexOf('@');
        string objectPath = atIndex > 0 ? locate.Substring(0, atIndex) : locate;
        string suffix = atIndex > 0 ? locate.Substring(atIndex) : string.Empty;

        var parts = objectPath.Split('/');
        bool changed = false;
        for (int i = 0; i < parts.Length; i++)
        {
            string segName = ParseLocateNameSegment(parts[i], out bool hasIndex, out int segIndex);
            if (hasIndex && segIndex == 0)
            {
                parts[i] = segName;
                changed = true;
            }
        }

        if (!changed) return locate;
        return $"{string.Join("/", parts)}{suffix}";
    }

    private static string StripZeroComponentIndex(string locate)
    {
        if (string.IsNullOrEmpty(locate)) return locate;
        if (locate.StartsWith("@")) return locate;

        int atIndex = locate.LastIndexOf('@');
        if (atIndex <= 0 || atIndex >= locate.Length - 1) return locate;

        string metaPart = locate.Substring(atIndex + 1);
        int tildeIndex = metaPart.IndexOf('~');
        if (tildeIndex > 0 && int.TryParse(metaPart.Substring(tildeIndex + 1), out int tildeCompIndex) &&
            tildeCompIndex == 0)
        {
            return $"{locate.Substring(0, atIndex + 1)}{metaPart.Substring(0, tildeIndex)}";
        }

        int colonIndex = metaPart.LastIndexOf(':');
        if (colonIndex > 0 && int.TryParse(metaPart.Substring(colonIndex + 1), out int compIndex) &&
            compIndex == 0)
        {
            return $"{locate.Substring(0, atIndex + 1)}{metaPart.Substring(0, colonIndex)}";
        }

        return locate;
    }

    private static bool TryResolveSceneObjectReference(string locate, out Object resolved)
    {
        resolved = null;
        if (string.IsNullOrEmpty(locate)) return false;
        if (locate.StartsWith("@")) return false;

        string objectPath = locate;
        string componentType = null;
        int compIndex = 0;
        bool hasComponent = false;

        int atIndex = locate.LastIndexOf('@');
        if (atIndex > 0)
        {
            hasComponent = true;
            objectPath = locate.Substring(0, atIndex);
            string metaPart = locate.Substring(atIndex + 1);
            componentType = metaPart;

            int tildeIndex = metaPart.IndexOf('~');
            int colonIndex = metaPart.LastIndexOf(':');
            if (tildeIndex > 0)
            {
                componentType = metaPart.Substring(0, tildeIndex);
                int.TryParse(metaPart.Substring(tildeIndex + 1), out compIndex);
            }
            else if (colonIndex > 0 && int.TryParse(metaPart.Substring(colonIndex + 1), out int parsedIndex))
            {
                componentType = metaPart.Substring(0, colonIndex);
                compIndex = parsedIndex;
            }
        }

        if (!TryFindSceneTransformByLocate(objectPath, out var targetTrans))
            return false;

        if (!hasComponent)
        {
            resolved = targetTrans.gameObject;
            return true;
        }

        Type searchType = null;
        if (locateToType.TryGetValue(locate, out var cachedType) && typeof(Component).IsAssignableFrom(cachedType))
            searchType = cachedType;

        if (searchType == null)
        {
            string normalizedLocate = StripZeroComponentIndex(locate);
            if (locateToType.TryGetValue(normalizedLocate, out var normalizedType) &&
                typeof(Component).IsAssignableFrom(normalizedType))
            {
                searchType = normalizedType;
            }
        }

        if (searchType == null && !string.IsNullOrEmpty(componentType))
        {
            var resolvedType = Type.GetType(componentType) ?? ResolveType(componentType);
            if (resolvedType != null && typeof(Component).IsAssignableFrom(resolvedType))
                searchType = resolvedType;
        }

        if (searchType == null) searchType = typeof(Component);

        var components = targetTrans.GetComponents(searchType);
        if (components == null || components.Length == 0) return false;

        if (compIndex >= 0 && compIndex < components.Length)
        {
            resolved = components[compIndex];
            return true;
        }

        return false;
    }

    private static bool TryFindSceneTransformByLocate(string objectPath, out Transform target)
    {
        target = null;
        if (string.IsNullOrEmpty(objectPath)) return false;

        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid()) return false;

        var segments = objectPath.Split('/');
        if (segments.Length == 0) return false;

        string rootName = ParseLocateNameSegment(segments[0], out bool hasRootIndex, out int rootIndex);
        int currentIndex = 0;
        GameObject root = null;

        var roots = scene.GetRootGameObjects();
        foreach (var rootObj in roots)
        {
            if (rootObj.name != rootName) continue;

            if (!hasRootIndex || currentIndex == rootIndex)
            {
                root = rootObj;
                break;
            }

            currentIndex++;
        }

        if (root == null && hasRootIndex && rootIndex == 0)
        {
            root = roots.FirstOrDefault(r => r.name == rootName);
        }

        if (root == null) return false;

        Transform current = root.transform;
        for (int i = 1; i < segments.Length; i++)
        {
            string segName = ParseLocateNameSegment(segments[i], out bool hasIndex, out int segIndex);
            var matched = FindChildByNameAndIndex(current, segName, hasIndex, segIndex);
            if (matched == null && hasIndex && segIndex == 0)
                matched = FindChildByNameAndIndex(current, segName, false, 0);

            if (matched == null) return false;
            current = matched;
        }

        target = current;
        return true;
    }

    private static Transform FindChildByNameAndIndex(Transform parent, string childName, bool hasIndex, int targetIndex)
    {
        int index = 0;
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child.name != childName) continue;

            if (!hasIndex || index == targetIndex)
                return child;

            index++;
        }

        return null;
    }

    private static bool TryResolveScriptableObjectReference(string locate, out Object resolved)
    {
        resolved = null;
        if (string.IsNullOrEmpty(locate) || !locate.StartsWith("@so:")) return false;

        ExtractTypeAndName(locate.Substring(4), out string typeName, out string objectName);

        Type expectedType = ResolveType(typeName);
        foreach (var obj in FindAssetsByTypeAndName(typeName, objectName))
        {
            if (!(obj is ScriptableObject)) continue;
            if (!string.IsNullOrEmpty(objectName) && obj.name != objectName) continue;
            if (!IsTypeMatch(obj, expectedType, typeName)) continue;

            resolved = obj;
            return true;
        }

        return false;
    }

    private static bool TryResolveGenericObjectReference(string locate, out Object resolved)
    {
        resolved = null;
        if (string.IsNullOrEmpty(locate) || !locate.StartsWith("@obj:")) return false;

        ExtractTypeAndName(locate.Substring(5), out string typeName, out string objectName);

        Type expectedType = ResolveType(typeName);
        foreach (var obj in FindAssetsByTypeAndName(typeName, objectName))
        {
            if (!string.IsNullOrEmpty(objectName) && obj.name != objectName) continue;
            if (!IsTypeMatch(obj, expectedType, typeName)) continue;

            resolved = obj;
            return true;
        }

        return false;
    }

    private static void ExtractTypeAndName(string content, out string typeName, out string objectName)
    {
        typeName = null;
        objectName = null;
        if (string.IsNullOrEmpty(content)) return;

        int colonIndex = content.IndexOf(':');
        if (colonIndex > 0)
        {
            typeName = content.Substring(0, colonIndex);
            objectName = content.Substring(colonIndex + 1);
            return;
        }

        int pipeIndex = content.IndexOf('|');
        if (pipeIndex > 0)
        {
            objectName = content.Substring(pipeIndex + 1);
            return;
        }

        objectName = content;
    }

    private static IEnumerable<Object> FindAssetsByTypeAndName(string typeName, string objectName)
    {
        var guids = new HashSet<string>();
        string shortTypeName = string.IsNullOrEmpty(typeName) ? null : typeName.Split('.').Last();

        if (!string.IsNullOrEmpty(shortTypeName) && !string.IsNullOrEmpty(objectName))
            foreach (var guid in AssetDatabase.FindAssets($"{objectName} t:{shortTypeName}"))
                guids.Add(guid);

        if (!string.IsNullOrEmpty(shortTypeName))
            foreach (var guid in AssetDatabase.FindAssets($"t:{shortTypeName}"))
                guids.Add(guid);

        if (!string.IsNullOrEmpty(objectName))
            foreach (var guid in AssetDatabase.FindAssets(objectName))
                guids.Add(guid);

        var visited = new HashSet<int>();
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) continue;

            var mainAsset = AssetDatabase.LoadMainAssetAtPath(path);
            if (mainAsset != null && visited.Add(mainAsset.GetInstanceID()))
                yield return mainAsset;

            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var asset in assets)
            {
                if (asset == null) continue;
                if (visited.Add(asset.GetInstanceID()))
                    yield return asset;
            }
        }
    }

    private static bool IsTypeMatch(Object obj, Type expectedType, string typeName)
    {
        if (obj == null) return false;

        if (expectedType != null)
            return expectedType.IsAssignableFrom(obj.GetType());

        if (!string.IsNullOrEmpty(typeName))
        {
            if (obj.GetType().FullName == typeName) return true;
            string shortType = typeName.Split('.').Last();
            return obj.GetType().Name == shortType;
        }

        return true;
    }

    private static bool TryResolvePrefabObjectReference(string locate, out Object resolved)
    {
        resolved = null;
        if (string.IsNullOrEmpty(locate)) return false;
        if (locate.StartsWith("@")) return false;

        string objectPath = locate;
        string componentType = null;
        int compIndex = 0;
        bool hasComponent = false;

        int atIndex = locate.LastIndexOf('@');
        if (atIndex > 0)
        {
            hasComponent = true;
            objectPath = locate.Substring(0, atIndex);
            string metaPart = locate.Substring(atIndex + 1);

            componentType = metaPart;
            int tildeIndex = metaPart.IndexOf('~'); // 兼容旧格式
            int colonIndex = metaPart.IndexOf(':'); // 新格式

            if (tildeIndex > 0)
            {
                componentType = metaPart.Substring(0, tildeIndex);
                int.TryParse(metaPart.Substring(tildeIndex + 1), out compIndex);
            }
            else if (colonIndex > 0)
            {
                componentType = metaPart.Substring(0, colonIndex);
                int.TryParse(metaPart.Substring(colonIndex + 1), out compIndex);
            }
        }

        var segments = objectPath.Split('/');
        if (segments.Length == 0 || string.IsNullOrEmpty(segments[0])) return false;

        string rootName = ParseLocateNameSegment(segments[0], out _, out _);
        if (string.IsNullOrEmpty(rootName)) return false;

        string[] guids = AssetDatabase.FindAssets($"{rootName} t:Prefab");
        foreach (var guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null) continue;

            if (!TryFindTransformByLocate(prefab.transform, objectPath, out var targetTrans))
                continue;

            if (!hasComponent)
            {
                resolved = targetTrans.gameObject;
                return true;
            }

            Type searchType = null;
            if (locateToType.TryGetValue(locate, out var cachedType) && typeof(Component).IsAssignableFrom(cachedType))
            {
                searchType = cachedType;
            }
            else if (!string.IsNullOrEmpty(componentType))
            {
                var resolvedType = Type.GetType(componentType) ?? ResolveType(componentType);
                if (resolvedType != null && typeof(Component).IsAssignableFrom(resolvedType))
                    searchType = resolvedType;
            }

            if (searchType == null) searchType = typeof(Component);
            var components = targetTrans.GetComponents(searchType);
            if (compIndex >= 0 && compIndex < components.Length)
            {
                resolved = components[compIndex];
                return true;
            }
        }

        return false;
    }

    private static bool TryFindTransformByLocate(Transform prefabRoot, string objectPath, out Transform target)
    {
        target = null;
        if (prefabRoot == null || string.IsNullOrEmpty(objectPath)) return false;

        var segments = objectPath.Split('/');
        if (segments.Length == 0) return false;

        string rootName = ParseLocateNameSegment(segments[0], out bool hasRootIndex, out int rootIndex);
        if (prefabRoot.name != rootName) return false;
        if (hasRootIndex && rootIndex != 0) return false; // prefab 资源只存在单 root

        var current = prefabRoot;
        for (int i = 1; i < segments.Length; i++)
        {
            string segName = ParseLocateNameSegment(segments[i], out bool hasIndex, out int segIndex);
            int matchIdx = 0;
            Transform matched = null;

            for (int c = 0; c < current.childCount; c++)
            {
                var child = current.GetChild(c);
                if (child.name != segName) continue;

                if (!hasIndex)
                {
                    matched = child;
                    break;
                }

                if (matchIdx == segIndex)
                {
                    matched = child;
                    break;
                }

                matchIdx++;
            }

            if (matched == null) return false;
            current = matched;
        }

        target = current;
        return true;
    }

    private static string ParseLocateNameSegment(string segment, out bool hasIndex, out int index)
    {
        hasIndex = false;
        index = 0;
        if (string.IsNullOrEmpty(segment)) return segment;

        int colonIndex = segment.LastIndexOf(':');
        if (colonIndex > 0 && colonIndex < segment.Length - 1 &&
            int.TryParse(segment.Substring(colonIndex + 1), out index))
        {
            hasIndex = true;
            return segment.Substring(0, colonIndex);
        }

        return segment;
    }

    /// <summary>
    /// 创建实例
    /// </summary>
    private static object CreateInstance(Type type)
    {
        if (type == null) return null;

        try
        {
            // 尝试无参构造函数
            return Activator.CreateInstance(type, true);
        }
        catch
        {
            try
            {
                // 尝试使用 FormatterServices (不调用构造函数)
                return System.Runtime.Serialization.FormatterServices.GetUninitializedObject(type);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SerializeReferenceIn] 无法创建实例 {type}: {ex.Message}");
                return null;
            }
        }
    }

    /// <summary>
    /// 递归获取字段 (包括基类)
    /// </summary>
    private static FieldInfo GetFieldRecursive(Type type, string fieldName)
    {
        while (type != null && type != typeof(object))
        {
            var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null) return field;
            type = type.BaseType;
        }

        return null;
    }

    /// <summary>
    /// 反序列化 Vector2
    /// </summary>
    private static Vector2 DeserializeVector2(Dictionary<string, object> dict)
    {
        float x = dict.TryGetValue("x", out var xVal) ? Convert.ToSingle(xVal) : 0;
        float y = dict.TryGetValue("y", out var yVal) ? Convert.ToSingle(yVal) : 0;
        return new Vector2(x, y);
    }

    /// <summary>
    /// 反序列化 Vector3
    /// </summary>
    private static Vector3 DeserializeVector3(Dictionary<string, object> dict)
    {
        float x = dict.TryGetValue("x", out var xVal) ? Convert.ToSingle(xVal) : 0;
        float y = dict.TryGetValue("y", out var yVal) ? Convert.ToSingle(yVal) : 0;
        float z = dict.TryGetValue("z", out var zVal) ? Convert.ToSingle(zVal) : 0;
        return new Vector3(x, y, z);
    }

    /// <summary>
    /// 反序列化 Vector4
    /// </summary>
    private static Vector4 DeserializeVector4(Dictionary<string, object> dict)
    {
        float x = dict.TryGetValue("x", out var xVal) ? Convert.ToSingle(xVal) : 0;
        float y = dict.TryGetValue("y", out var yVal) ? Convert.ToSingle(yVal) : 0;
        float z = dict.TryGetValue("z", out var zVal) ? Convert.ToSingle(zVal) : 0;
        float w = dict.TryGetValue("w", out var wVal) ? Convert.ToSingle(wVal) : 0;
        return new Vector4(x, y, z, w);
    }

    /// <summary>
    /// 反序列化 Color
    /// </summary>
    private static Color DeserializeColor(Dictionary<string, object> dict)
    {
        float r = dict.TryGetValue("r", out var rVal) ? Convert.ToSingle(rVal) : 0;
        float g = dict.TryGetValue("g", out var gVal) ? Convert.ToSingle(gVal) : 0;
        float b = dict.TryGetValue("b", out var bVal) ? Convert.ToSingle(bVal) : 0;
        float a = dict.TryGetValue("a", out var aVal) ? Convert.ToSingle(aVal) : 1;
        return new Color(r, g, b, a);
    }

    /// <summary>
    /// 简单 JSON 解析器
    /// </summary>
    private static Dictionary<string, object> ParseSimpleJson(string json)
    {
        var result = new Dictionary<string, object>();
        if (string.IsNullOrEmpty(json)) return result;

        json = json.Trim();
        if (!json.StartsWith("{") || !json.EndsWith("}")) return result;

        json = json.Substring(1, json.Length - 2);

        int pos = 0;
        while (pos < json.Length)
        {
            // 跳过空白
            while (pos < json.Length && char.IsWhiteSpace(json[pos])) pos++;
            if (pos >= json.Length) break;

            // 读取键
            if (json[pos] != '"')
            {
                pos++;
                continue;
            }

            pos++;
            int keyStart = pos;
            while (pos < json.Length && json[pos] != '"') pos++;
            string key = json.Substring(keyStart, pos - keyStart);
            pos++;

            // 跳过冒号
            while (pos < json.Length && json[pos] != ':') pos++;
            pos++;

            // 跳过空白
            while (pos < json.Length && char.IsWhiteSpace(json[pos])) pos++;

            // 读取值
            object value = ParseJsonValue(json, ref pos);
            result[key] = value;

            // 跳过逗号
            while (pos < json.Length && (char.IsWhiteSpace(json[pos]) || json[pos] == ',')) pos++;
        }

        return result;
    }

    /// <summary>
    /// 解析 JSON 值
    /// </summary>
    private static object ParseJsonValue(string json, ref int pos)
    {
        while (pos < json.Length && char.IsWhiteSpace(json[pos])) pos++;
        if (pos >= json.Length) return null;

        char c = json[pos];

        // 字符串
        if (c == '"')
        {
            pos++;
            var sb = new StringBuilder();
            while (pos < json.Length)
            {
                c = json[pos];
                if (c == '"')
                {
                    pos++;
                    break;
                }

                if (c == '\\' && pos + 1 < json.Length)
                {
                    pos++;
                    char escaped = json[pos];
                    switch (escaped)
                    {
                        case 'n':
                            sb.Append('\n');
                            break;
                        case 'r':
                            sb.Append('\r');
                            break;
                        case 't':
                            sb.Append('\t');
                            break;
                        case '"':
                            sb.Append('"');
                            break;
                        case '\\':
                            sb.Append('\\');
                            break;
                        default:
                            sb.Append(escaped);
                            break;
                    }
                }
                else
                {
                    sb.Append(c);
                }

                pos++;
            }

            return sb.ToString();
        }

        // 数组
        if (c == '[')
        {
            pos++;
            var list = new List<object>();
            while (pos < json.Length)
            {
                while (pos < json.Length && char.IsWhiteSpace(json[pos])) pos++;
                if (pos >= json.Length || json[pos] == ']')
                {
                    pos++;
                    break;
                }

                list.Add(ParseJsonValue(json, ref pos));
                while (pos < json.Length && (char.IsWhiteSpace(json[pos]) || json[pos] == ',')) pos++;
            }

            return list;
        }

        // 对象
        if (c == '{')
        {
            pos++;
            var dict = new Dictionary<string, object>();
            while (pos < json.Length)
            {
                while (pos < json.Length && char.IsWhiteSpace(json[pos])) pos++;
                if (pos >= json.Length || json[pos] == '}')
                {
                    pos++;
                    break;
                }

                if (json[pos] == '"')
                {
                    pos++;
                    int keyStart = pos;
                    while (pos < json.Length && json[pos] != '"') pos++;
                    string key = json.Substring(keyStart, pos - keyStart);
                    pos++;
                    while (pos < json.Length && json[pos] != ':') pos++;
                    pos++;
                    dict[key] = ParseJsonValue(json, ref pos);
                }

                while (pos < json.Length && (char.IsWhiteSpace(json[pos]) || json[pos] == ',')) pos++;
            }

            return dict;
        }

        // null
        if (pos + 4 <= json.Length && json.Substring(pos, 4) == "null")
        {
            pos += 4;
            return null;
        }

        // true/false
        if (pos + 4 <= json.Length && json.Substring(pos, 4) == "true")
        {
            pos += 4;
            return true;
        }

        if (pos + 5 <= json.Length && json.Substring(pos, 5) == "false")
        {
            pos += 5;
            return false;
        }

        // 数字
        int numStart = pos;
        while (pos < json.Length && (char.IsDigit(json[pos]) || json[pos] == '.' || json[pos] == '-' ||
                                     json[pos] == '+' || json[pos] == 'e' || json[pos] == 'E'))
        {
            pos++;
        }

        string numStr = json.Substring(numStart, pos - numStart);
        if (numStr.Contains('.') || numStr.Contains('e') || numStr.Contains('E'))
        {
            if (double.TryParse(numStr, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double d))
                return d;
        }
        else
        {
            if (long.TryParse(numStr, out long l))
                return l;
        }

        return null;
    }

    /// <summary>
    /// 跳过到 EndMark
    /// </summary>
    private static void SkipToEndMark(string[] lines, ref int lineIndex)
    {
        while (lineIndex < lines.Length)
        {
            if (lines[lineIndex].Trim() == EndMark)
            {
                lineIndex++;
                break;
            }

            lineIndex++;
        }
    }
}
