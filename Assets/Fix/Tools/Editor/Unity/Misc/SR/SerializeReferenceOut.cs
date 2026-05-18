using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

/// <summary>
/// <para>使用流程：</para>
/// <para>1.使用dnspy将这个代码编译到游戏的Assembly-CSharp.dll或者其他可以被调用的地方，编译并保存</para>
/// <para>2.主动调用  SRIO.Output();  越早调用越稳定，编译并保存 <see cref="Output"/></para>
/// <para>3.运行游戏  </para>
/// <para>4.导入  </para>
/// </summary>
public partial class SRIO : MonoBehaviour
{
    #region const

    private const string OutputPath = @"E:\_HWTest\SmashHitMuseum\Library\Fix\SR";
    private const string InfoName = "Info.txt";
    private const string SRName = "SerializeReference.txt";

    private const string PrefabPrefix = "#PREFAB#";
    private const string ScenePrefix = "#SCENE#";
    private const string ScriptableObjectPrefix = "#SO#";
    private const string CodecPrefix = "#OBJECT_CODEC#";
    private const string EndMark = "#EOF#";

    #endregion

    #region singleton

    private static SRIO instance;

    private static SRIO Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("[SerializeReferenceInOut]");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<SRIO>();
            }

            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    #endregion

    #region info

    private static StreamWriter infoStream;

    #endregion

    #region args

    private static bool started;
    private Coroutine runningCoroutine;

    #endregion

    #region data

    private static StringBuilder mainBuilder;
    private static StringBuilder tempBuilder;
    private static HashSet<GameObject> goSet;
    private static HashSet<ScriptableObject> soSet;
    private static int exportedContextCount;
    private static int exportedEntryCount;
    private static int exportedFieldCount;
    private static int exportedNullFieldCount;
    private static int exportedReferenceCount;
    private static int exportedCodecCount;

    #endregion

    /// <summary>
    /// 入口点 - 启动协程
    /// </summary>
    public static void Output()
    {
        if (started) return;
        // 确保只有一个协程在运行
        if (Instance.runningCoroutine != null)
            throw new InvalidOperationException("非法行为");

        Instance.runningCoroutine = Instance.StartCoroutine(Instance.OutputCoroutine());
    }

    /// <summary>
    /// 协程实现 - 不受 Time.timeScale 影响
    /// </summary>
    private IEnumerator OutputCoroutine()
    {
        if (started) yield break;
        started = true;

        string path = OutputPath;
        string infoPath = Path.Combine(path, InfoName);
        string outputPath = Path.Combine(path, SRName);

        Exception caughtException = null;

        // 初始化
        try
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            infoStream = new StreamWriter(File.Open(infoPath, FileMode.Create));
            infoStream.WriteLine(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"));

            mainBuilder = new StringBuilder();
            tempBuilder = new StringBuilder();
            goSet = new HashSet<GameObject>();
            soSet = new HashSet<ScriptableObject>();
            ResetExportStats();

            infoStream.WriteLine(SceneManager.sceneCountInBuildSettings.ToString());
            Init();
        }
        catch (Exception e)
        {
            caughtException = e;
        }

        // 如果初始化失败，提前退出
        if (caughtException != null)
        {
            HandleException(caughtException);
            Cleanup();
            yield break;
        }

        string root = Application.streamingAssetsPath;
        if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
        {
            infoStream.WriteLine("Without StreamingAssets");
        }
        else
        {
            foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            {
                try
                {
                    AssetBundle.LoadFromFile(file);
                }
                catch
                {
                }
            }
        }


        // 使用协程遍历所有场景 (yield 在 try-catch 外部)
        IEnumerator sceneEnumerator = ProcessAllScenes();
        while (true)
        {
            bool moveNext = false;
            try
            {
                moveNext = sceneEnumerator.MoveNext();
            }
            catch (Exception e)
            {
                caughtException = e;
                break;
            }

            if (!moveNext) break;
            yield return sceneEnumerator.Current;
        }

        // 如果场景处理出错
        if (caughtException != null)
        {
            HandleException(caughtException);
            Cleanup();
            yield break;
        }

        // 后续处理
        try
        {
            // 处理 Prefabs 和 ScriptableObjects
            ProcessPrefabsAndScriptableObjects();

            // 写入文件
            File.WriteAllText(outputPath, mainBuilder.ToString());
            WriteExportSummary();
            infoStream.WriteLine("Output completed successfully.");
        }
        catch (Exception e)
        {
            HandleException(e);
        }

        Cleanup();
        ShowInExplorer();
    }

    private void HandleException(Exception e)
    {
        if (infoStream != null)
        {
            try
            {
                infoStream.WriteLine(e.ToString());
            }
            catch
            {
            }
        }

        Debug.LogException(e);
    }

    private void Cleanup()
    {
        if (infoStream != null)
        {
            try
            {
                infoStream.Flush();
                infoStream.Dispose();
            }
            catch
            {
            }

            infoStream = null;
        }

        runningCoroutine = null;
    }

    private static void Init()
    {
        SRType.InitType();
    }

    private static void ResetExportStats()
    {
        exportedContextCount = 0;
        exportedEntryCount = 0;
        exportedFieldCount = 0;
        exportedNullFieldCount = 0;
        exportedReferenceCount = 0;
        exportedCodecCount = 0;
    }

    private static void WriteExportSummary()
    {
        if (infoStream == null) return;
        infoStream.WriteLine(
            $"Summary contexts={exportedContextCount}, entries={exportedEntryCount}, fields={exportedFieldCount}, nullFields={exportedNullFieldCount}, refs={exportedReferenceCount}, codec={exportedCodecCount}");
    }

    /// <summary>
    /// 协程：遍历所有场景
    /// </summary>
    private static IEnumerator ProcessAllScenes()
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            // 异步加载场景
            var asyncOp = SceneManager.LoadSceneAsync(i, LoadSceneMode.Single);
            asyncOp.completed += op =>
            {
                // 场景加载完成后处理
                var scene = SceneManager.GetActiveScene();
                infoStream.WriteLine($"Handling {scene.path}");

                // 处理场景中的对象
                var sceneContext = new SceneContext()
                {
                    target = scene,
                    prefix = $"{ScenePrefix} {SRLocate.GetLocate(scene)}"
                };
                ProcessContext(sceneContext);

                // 收集非场景的 GameObject 和 ScriptableObject
                goSet.RemoveWhere(e => e == null || e.scene.IsValid());
                soSet.RemoveWhere(e => e == null);
                SRExt.AddRange(goSet, Resources.FindObjectsOfTypeAll<GameObject>());
                SRExt.AddRange(soSet, Resources.FindObjectsOfTypeAll<ScriptableObject>());
            };
            while (!asyncOp.isDone)
                yield return null;
        }

        {
            var scene = SceneManager.CreateScene(EndMark);
            SceneManager.SetActiveScene(scene);
            var asyncOp =
                SceneManager.UnloadSceneAsync(SceneManager.sceneCountInBuildSettings - 1, UnloadSceneOptions.None);
            asyncOp.completed += op =>
            {
                goSet.RemoveWhere(e => e == null || e.scene.IsValid());
                soSet.RemoveWhere(e => e == null);
            };
            while (!asyncOp.isDone)
                yield return null;
        }
    }

    /// <summary>
    /// 处理 Prefabs 和 ScriptableObjects
    /// </summary>
    private static void ProcessPrefabsAndScriptableObjects()
    {
        LoadFromResources();
        LoadFromAssetsBundles();

        // 处理 Prefabs
        foreach (var prefab in goSet
            .Where(e => e != null && e.transform.parent == null)
            .OrderBy(SRLocate.GetLocate, StringComparer.Ordinal))
        {
            var context = new RootGameObjectContext()
            {
                target = prefab,
                prefix = $"{PrefabPrefix} {SRLocate.GetLocate(prefab)}"
            };
            ProcessContext(context);
        }

        // 处理 ScriptableObjects
        foreach (var so in soSet
            .Where(e => e != null)
            .OrderBy(SRLocate.GetLocate, StringComparer.Ordinal))
        {
            var context = new ScriptableObjectContext()
            {
                target = so,
                prefix = $"{ScriptableObjectPrefix} {SRLocate.GetLocate(so)}"
            };
            ProcessContext(context);
        }
    }

    private static void LoadFromResources()
    {
        SRExt.AddRange(goSet, Resources.LoadAll<GameObject>(""));
        SRExt.AddRange(soSet, Resources.LoadAll<ScriptableObject>(""));
    }

    private static void LoadFromAssetsBundles()
    {
        foreach (var bundle in Resources.FindObjectsOfTypeAll<AssetBundle>())
        {
            infoStream.WriteLine($"Handling bundle {bundle.name}");
            SRExt.AddRange(goSet, bundle.LoadAllAssets<GameObject>());
            SRExt.AddRange(soSet, bundle.LoadAllAssets<ScriptableObject>());
        }
    }

    /// <summary>
    /// 处理单个上下文并写入到主 builder
    /// </summary>
    private static void ProcessContext(SRContext context)
    {
        SRSerialize.InitRef();
        tempBuilder.Clear();
        tempBuilder.AppendLine(context.prefix);
        bool anyEntry = false;

        foreach (var entry in context.FetchObjectWithSR())
        {
            anyEntry = true;
            exportedEntryCount++;
            tempBuilder.AppendLine(
                $"{SRExt.FormatAsLine(entry.locate)}#{SRExt.FormatAsLine(SRSerialize.Serialize(entry.obj))}");
        }

        tempBuilder.AppendLine(CodecPrefix);
        SRSerialize.SerializeRef(tempBuilder);
        tempBuilder.AppendLine(EndMark);
        if (anyEntry)
        {
            exportedContextCount++;
            mainBuilder.Append(tempBuilder);
        }
    }

    internal static void ReportFieldSerialized(bool isNull)
    {
        exportedFieldCount++;
        if (isNull) exportedNullFieldCount++;
    }

    internal static void ReportUnityObjectReference()
    {
        exportedReferenceCount++;
    }

    internal static void ReportCodecEntryCount(int count)
    {
        exportedCodecCount += count;
    }

    private static void ShowInExplorer()
    {
        using (Process.Start("Explorer.exe", OutputPath.Replace("/", "\\")))
        {
        }
    }

    #region Inner Classes

    #region struct

    [Serializable]
    internal class SREntry
    {
        public Object obj;
        public string locate;

        public SREntry(Object obj, string locate)
        {
            this.obj = obj;
            this.locate = locate;
        }
    }

    [Serializable]
    internal class SRFieldInfo
    {
        //SerializedProperty Style
        public string propertyPath;

        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public SRFieldInfo(string propertyPath)
        {
            this.propertyPath = propertyPath;
        }

        /// <summary>
        /// 根据 propertyPath 获取对象的字段值
        /// propertyPath 格式类似于 SerializedProperty.propertyPath，例如："fieldName.Array.data[0].subField"
        /// </summary>
        public object GetValue(Object root)
        {
            if (root == null) return null;

            string[] pathParts = ParsePropertyPath(propertyPath);
            var currentNodes = new List<object> {root};
            bool hasWildcard = false;

            foreach (string part in pathParts)
            {
                if (part == "Array" || part == "data")
                {
                    // 跳过 Unity 的 Array.data 标记
                    continue;
                }

                var nextNodes = new List<object>();
                string token;
                if (TryParseIndexToken(part, out token))
                {
                    if (token == "*")
                    {
                        hasWildcard = true;
                        foreach (var node in currentNodes)
                        {
                            AppendAllElements(node, nextNodes);
                        }
                    }
                    else
                    {
                        int index;
                        if (!int.TryParse(token, out index))
                            return null;

                        foreach (var node in currentNodes)
                        {
                            object element;
                            if (TryGetElementAt(node, index, out element))
                                nextNodes.Add(element);
                        }
                    }
                }
                else
                {
                    foreach (var node in currentNodes)
                    {
                        if (node == null) continue;
                        var field = FindField(node.GetType(), part);
                        if (field != null)
                            nextNodes.Add(field.GetValue(node));
                    }
                }

                if (nextNodes.Count == 0) return null;
                currentNodes = nextNodes;
            }

            if (currentNodes.Count == 1 && !hasWildcard)
                return currentNodes[0];
            return currentNodes;
        }

        /// <summary>
        /// 根据 propertyPath 设置对象的字段值
        /// </summary>
        public void SetValue(Object root, object value)
        {
            if (root == null) return;

            string[] pathParts = ParsePropertyPath(propertyPath);
            if (pathParts.Length == 0) return;

            // 获取父对象和最后一个路径部分
            var parentNodes = new List<object> {root};
            for (int i = 0; i < pathParts.Length - 1; i++)
            {
                string part = pathParts[i];
                if (part == "Array" || part == "data")
                {
                    continue;
                }

                var nextParents = new List<object>();
                string token;
                if (TryParseIndexToken(part, out token))
                {
                    if (token == "*")
                    {
                        foreach (var parent in parentNodes)
                        {
                            AppendAllElements(parent, nextParents);
                        }
                    }
                    else
                    {
                        int index;
                        if (!int.TryParse(token, out index))
                            return;

                        foreach (var parent in parentNodes)
                        {
                            object element;
                            if (TryGetElementAt(parent, index, out element))
                                nextParents.Add(element);
                        }
                    }
                }
                else
                {
                    foreach (var parent in parentNodes)
                    {
                        if (parent == null) continue;
                        var field = FindField(parent.GetType(), part);
                        if (field != null)
                            nextParents.Add(field.GetValue(parent));
                    }
                }

                if (nextParents.Count == 0) return;
                parentNodes = nextParents;
            }

            // 设置最后一个字段的值
            string lastPart = pathParts[pathParts.Length - 1];
            string lastToken;
            if (TryParseIndexToken(lastPart, out lastToken))
            {
                if (lastToken == "*")
                {
                    foreach (var parent in parentNodes)
                    {
                        if (parent is System.Collections.IList list)
                        {
                            if (value is System.Collections.IList valueList)
                            {
                                int len = Math.Min(list.Count, valueList.Count);
                                for (int i = 0; i < len; i++)
                                    list[i] = valueList[i];
                                continue;
                            }

                            for (int i = 0; i < list.Count; i++)
                                list[i] = value;
                        }
                        else if (parent is Array arr)
                        {
                            if (value is System.Collections.IList valueList)
                            {
                                int len = Math.Min(arr.Length, valueList.Count);
                                for (int i = 0; i < len; i++)
                                    arr.SetValue(valueList[i], i);
                                continue;
                            }

                            for (int i = 0; i < arr.Length; i++)
                                arr.SetValue(value, i);
                        }
                    }
                }
                else
                {
                    int index;
                    if (!int.TryParse(lastToken, out index))
                        return;

                    foreach (var parent in parentNodes)
                    {
                        if (parent is System.Collections.IList list)
                        {
                            if (index >= 0 && index < list.Count)
                                list[index] = value;
                        }
                        else if (parent is Array arr)
                        {
                            if (index >= 0 && index < arr.Length)
                                arr.SetValue(value, index);
                        }
                    }
                }
            }
            else if (lastPart != "Array" && lastPart != "data")
            {
                if (parentNodes.Count > 1 && value is System.Collections.IList distributeValues)
                {
                    for (int i = 0; i < parentNodes.Count; i++)
                    {
                        var parent = parentNodes[i];
                        if (parent == null) continue;

                        var field = FindField(parent.GetType(), lastPart);
                        if (field == null) continue;

                        object itemValue = i < distributeValues.Count ? distributeValues[i] : null;
                        field.SetValue(parent, itemValue);
                    }

                    return;
                }

                foreach (var parent in parentNodes)
                {
                    if (parent == null) continue;
                    var field = FindField(parent.GetType(), lastPart);
                    field?.SetValue(parent, value);
                }
            }
        }

        /// <summary>
        /// 解析属性路径为各个部分
        /// </summary>
        private string[] ParsePropertyPath(string path)
        {
            var parts = new List<string>();
            var current = new StringBuilder();

            for (int i = 0; i < path.Length; i++)
            {
                char c = path[i];
                if (c == '.')
                {
                    if (current.Length > 0)
                    {
                        parts.Add(current.ToString());
                        current.Clear();
                    }
                }
                else if (c == '[')
                {
                    if (current.Length > 0)
                    {
                        parts.Add(current.ToString());
                        current.Clear();
                    }

                    // 提取数组索引
                    int endIndex = path.IndexOf(']', i);
                    if (endIndex > i)
                    {
                        parts.Add(path.Substring(i, endIndex - i + 1));
                        i = endIndex;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }

            if (current.Length > 0)
                parts.Add(current.ToString());

            return parts.ToArray();
        }

        private static FieldInfo FindField(Type type, string fieldName)
        {
            FieldInfo field = null;
            while (type != null && field == null)
            {
                field = type.GetField(fieldName, Flags);
                type = type.BaseType;
            }

            return field;
        }

        private static bool TryParseIndexToken(string part, out string token)
        {
            token = null;
            if (string.IsNullOrEmpty(part)) return false;
            if (part.Length < 3) return false;
            if (part[0] != '[' || part[part.Length - 1] != ']') return false;

            token = part.Substring(1, part.Length - 2);
            return true;
        }

        private static void AppendAllElements(object source, List<object> results)
        {
            if (source == null) return;

            if (source is System.Collections.IList list)
            {
                for (int i = 0; i < list.Count; i++)
                    results.Add(list[i]);
                return;
            }

            if (source is Array arr)
            {
                for (int i = 0; i < arr.Length; i++)
                    results.Add(arr.GetValue(i));
            }
        }

        private static bool TryGetElementAt(object source, int index, out object element)
        {
            element = null;
            if (index < 0 || source == null) return false;

            if (source is System.Collections.IList list)
            {
                if (index >= list.Count) return false;
                element = list[index];
                return true;
            }

            if (source is Array arr)
            {
                if (index >= arr.Length) return false;
                element = arr.GetValue(index);
                return true;
            }

            return false;
        }
    }

    #endregion

    #region context

    internal abstract class SRContext
    {
        public string prefix;
        public abstract IEnumerable<SREntry> FetchObjectWithSR();
    }

    internal abstract class SRContext<T> : SRContext
    {
        public T target;
    }

    internal class ScriptableObjectContext : SRContext<ScriptableObject>
    {
        public override IEnumerable<SREntry> FetchObjectWithSR()
        {
            if (SRType.HasSRField(target))
                yield return new SREntry(target, SRLocate.GetLocate(target));
        }
    }

    internal class SceneContext : SRContext<Scene>
    {
        public override IEnumerable<SREntry> FetchObjectWithSR()
        {
            var goContext = new RootGameObjectContext();
            foreach (var root in target.GetRootGameObjects())
            {
                goContext.target = root;
                foreach (var tuple in goContext.FetchObjectWithSR())
                    yield return tuple;
            }
        }
    }

    internal class RootGameObjectContext : SRContext<GameObject>
    {
        public override IEnumerable<SREntry> FetchObjectWithSR()
        {
            foreach (var component in target.GetComponentsInChildren<Component>(true))
                if (SRType.HasSRField(component))
                    yield return new SREntry(component, SRLocate.GetLocate(component));
        }
    }

    #endregion

    #region SRType

    internal static class SRType
    {
        private static readonly IDictionary<Type, SRFieldInfo[]> UnityTypeWithFields =
            new Dictionary<Type, SRFieldInfo[]>();

        private static readonly IDictionary<Type, bool> UnityTypeHasSR =
            new Dictionary<Type, bool>();

        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        /// <summary>
        /// 初始化类型缓存，扫描所有继承自 UnityEngine.Object 的类型
        /// 检查它们是否包含带有 [SerializeReference] 特性的字段
        /// </summary>
        public static void InitType()
        {
            UnityTypeWithFields.Clear();
            UnityTypeHasSR.Clear();

            // 遍历所有程序集中继承自 UnityEngine.Object 的类型
            foreach (var type in AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(a => !a.IsDynamic)
                .SelectMany(a =>
                {
                    try
                    {
                        return a.GetTypes();
                    }
                    catch
                    {
                        return Type.EmptyTypes;
                    }
                })
                .Where(t => typeof(Object).IsAssignableFrom(t) && !t.IsAbstract))
            {
                var hasSR = ContainsSerializeReferenceField(type, new HashSet<Type>());
                UnityTypeHasSR[type] = hasSR;

                if (hasSR)
                {
                    UnityTypeWithFields[type] = CollectExportFields(type);
                }
            }
        }

        /// <summary>
        /// 收集对象的所有可序列化顶层字段（严格字段路径）
        /// </summary>
        private static SRFieldInfo[] CollectExportFields(Type type)
        {
            var exists = new HashSet<string>();
            var result = new List<SRFieldInfo>();
            var currentType = type;
            while (currentType != null && currentType != typeof(object))
            {
                foreach (var field in currentType.GetFields(Flags | BindingFlags.DeclaredOnly))
                {
                    if (!IsSerializableField(field)) continue;
                    if (exists.Add(field.Name))
                    {
                        result.Add(new SRFieldInfo(field.Name));
                    }
                }

                currentType = currentType.BaseType;
            }

            return result.ToArray();
        }

        /// <summary>
        /// 检查类型及其嵌套可序列化类型中是否存在 [SerializeReference] 字段
        /// </summary>
        private static bool ContainsSerializeReferenceField(Type type, HashSet<Type> visited)
        {
            if (type == null || visited.Contains(type)) return false;
            visited.Add(type);

            var currentType = type;
            while (currentType != null && currentType != typeof(object))
            {
                foreach (var field in currentType.GetFields(Flags | BindingFlags.DeclaredOnly))
                {
                    if (!IsSerializableField(field)) continue;

                    if (field.GetCustomAttribute<SerializeReference>() != null)
                        return true;

                    var fieldType = field.FieldType;
                    var nestedType = GetNestedSerializableType(fieldType);
                    if (nestedType != null && ContainsSerializeReferenceField(nestedType, new HashSet<Type>(visited)))
                        return true;
                }

                currentType = currentType.BaseType;
            }

            return false;
        }

        private static Type GetNestedSerializableType(Type fieldType)
        {
            if (fieldType == null) return null;

            if (fieldType.IsArray)
                fieldType = fieldType.GetElementType();
            else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
                fieldType = fieldType.GetGenericArguments()[0];

            if (fieldType == null) return null;
            if (fieldType.IsPrimitive || fieldType == typeof(string) || fieldType.IsEnum) return null;
            if (typeof(Object).IsAssignableFrom(fieldType)) return null;
            if (fieldType.GetCustomAttribute<SerializableAttribute>() != null) return fieldType;
            return null;
        }

        /// <summary>
        /// 检查字段是否可序列化
        /// </summary>
        private static bool IsSerializableField(FieldInfo field)
        {
            // Unity 序列化字段判定:
            // public，或 [SerializeField]，或 [SerializeReference]
            var hasSerializeReference = field.GetCustomAttribute<SerializeReference>() != null;
            if (field.IsPublic || field.GetCustomAttribute<SerializeField>() != null || hasSerializeReference)
            {
                // 排除带有 NonSerialized 标记的字段
                if (field.GetCustomAttribute<NonSerializedAttribute>() != null)
                    return false;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 检查类型是否可序列化
        /// </summary>
        private static bool IsSerializableType(Type type)
        {
            if (type == null) return false;
            if (type.IsPrimitive || type == typeof(string) || type.IsEnum) return true;
            if (typeof(Object).IsAssignableFrom(type)) return true;
            if (type.GetCustomAttribute<SerializableAttribute>() != null) return true;
            return false;
        }

        /// <summary>
        /// 检查 UnityEngine.Object 是否包含 SerializeReference 字段
        /// </summary>
        public static bool HasSRField(Object uObj)
        {
            if (uObj == null) return false;

            var type = uObj.GetType();

            if (UnityTypeHasSR.TryGetValue(type, out var hasSR))
                return hasSR;

            hasSR = ContainsSerializeReferenceField(type, new HashSet<Type>());
            UnityTypeHasSR[type] = hasSR;
            if (hasSR && !UnityTypeWithFields.ContainsKey(type))
                UnityTypeWithFields[type] = CollectExportFields(type);
            return hasSR;
        }

        /// <summary>
        /// 获取对象的所有 SerializeReference 字段信息
        /// </summary>
        public static SRFieldInfo[] GetSRFields(Object uObj)
        {
            if (uObj == null) return Array.Empty<SRFieldInfo>();

            var type = uObj.GetType();
            if (UnityTypeWithFields.TryGetValue(type, out var fields))
                return fields;

            fields = CollectExportFields(type);
            UnityTypeWithFields[type] = fields;
            return fields;
        }
    }

    #endregion

    #region SRSerialize

    internal static class SRSerialize
    {
        private static readonly IDictionary<Object, int> obj2Id = new Dictionary<Object, int>();
        private static readonly SortedDictionary<int, Object> id2Obj = new SortedDictionary<int, Object>();
        private static int nextRefId = 1;
        private const int MaxSerializeDepth = 256;
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private sealed class RefEqComparer : IEqualityComparer<object>
        {
            public static readonly RefEqComparer Instance = new RefEqComparer();

            public new bool Equals(object x, object y) => ReferenceEquals(x, y);
            public int GetHashCode(object obj) => obj == null ? 0 : RuntimeHelpers.GetHashCode(obj);
        }

        /// <summary>
        /// 序列化 UnityEngine.Object 对象
        /// 对于 SerializeReference 字段中的 UnityEngine.Object 引用，将其序列化为可定位的字符串
        /// </summary>
        public static string Serialize(Object obj)
        {
            if (obj == null) return "null";


            var sb = new StringBuilder();
            sb.Append("{");

            var srFields = SRType.GetSRFields(obj);
            bool first = true;
            var serializeStack = new HashSet<object>(RefEqComparer.Instance);

            foreach (var fieldInfo in srFields)
            {
                var value = fieldInfo.GetValue(obj);

                if (!first) sb.Append(",");
                first = false;

                sb.Append($"\"{fieldInfo.propertyPath}\":");
                SRIO.ReportFieldSerialized(value == null);
                SerializeValue(sb, value, obj, serializeStack, 0);
            }

            sb.Append("}");
            return sb.ToString();
        }

        /// <summary>
        /// 序列化单个值
        /// </summary>
        private static void SerializeValue(StringBuilder sb, object value, Object rootContext,
            HashSet<object> serializeStack, int depth)
        {
            if (value == null)
            {
                sb.Append("null");
                return;
            }

            if (depth > MaxSerializeDepth)
            {
                sb.Append("null");
                return;
            }

            var type = value.GetType();

            // 处理 UnityEngine.Object 引用
            if (value is Object unityObj)
            {
                if (unityObj == null)
                {
                    sb.Append("null");
                    return;
                }

                int refId;
                if (!obj2Id.TryGetValue(unityObj, out refId))
                {
                    refId = nextRefId++;
                    obj2Id[unityObj] = refId;
                    id2Obj[refId] = unityObj;
                }

                SRIO.ReportUnityObjectReference();
                sb.Append($"\"@ref:{refId}\"");
                return;
            }

            bool pushed = false;
            if (!type.IsValueType)
            {
                if (!serializeStack.Add(value))
                {
                    sb.Append("null");
                    return;
                }

                pushed = true;
            }

            try
            {
                // 处理基本类型
                if (type == typeof(string))
                {
                    sb.Append($"\"{EscapeString((string) value)}\"");
                    return;
                }

                if (type == typeof(bool))
                {
                    sb.Append((bool) value ? "true" : "false");
                    return;
                }

                if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte))
                {
                    sb.Append(value.ToString());
                    return;
                }

                if (type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort) || type == typeof(sbyte))
                {
                    sb.Append(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture));
                    return;
                }

                if (type == typeof(float))
                {
                    var f = (float) value;
                    if (float.IsNaN(f) || float.IsInfinity(f))
                        sb.Append($"\"{f.ToString(System.Globalization.CultureInfo.InvariantCulture)}\"");
                    else
                        sb.Append(f.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    return;
                }

                if (type == typeof(double))
                {
                    var d = (double) value;
                    if (double.IsNaN(d) || double.IsInfinity(d))
                        sb.Append($"\"{d.ToString(System.Globalization.CultureInfo.InvariantCulture)}\"");
                    else
                        sb.Append(d.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    return;
                }

                if (type == typeof(decimal))
                {
                    sb.Append(((decimal) value).ToString(System.Globalization.CultureInfo.InvariantCulture));
                    return;
                }

                if (type == typeof(char))
                {
                    sb.Append($"\"{EscapeString(value.ToString())}\"");
                    return;
                }

                if (type.IsEnum)
                {
                    sb.Append($"\"{value}\"");
                    return;
                }

                // 处理数组
                if (type.IsArray)
                {
                    var arr = (Array) value;
                    sb.Append("[");
                    for (int i = 0; i < arr.Length; i++)
                    {
                        if (i > 0) sb.Append(",");
                        SerializeValue(sb, arr.GetValue(i), rootContext, serializeStack, depth + 1);
                    }

                    sb.Append("]");
                    return;
                }

                // 处理 List
                if (value is System.Collections.IList list)
                {
                    sb.Append("[");
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (i > 0) sb.Append(",");
                        SerializeValue(sb, list[i], rootContext, serializeStack, depth + 1);
                    }

                    sb.Append("]");
                    return;
                }

                // 处理复杂对象
                sb.Append("{");
                sb.Append($"\"$type\":\"{type.FullName}, {type.Assembly.GetName().Name}\"");

                var currentType = type;
                while (currentType != null && currentType != typeof(object))
                {
                    foreach (var field in currentType.GetFields(Flags | BindingFlags.DeclaredOnly))
                    {
                        if (!IsSerializableField(field)) continue;

                        var fieldValue = field.GetValue(value);
                        sb.Append($",\"{field.Name}\":");
                        SerializeValue(sb, fieldValue, rootContext, serializeStack, depth + 1);
                    }

                    currentType = currentType.BaseType;
                }

                sb.Append("}");
            }
            finally
            {
                if (pushed)
                    serializeStack.Remove(value);
            }
        }

        /// <summary>
        /// 检查字段是否可序列化
        /// </summary>
        private static bool IsSerializableField(FieldInfo field)
        {
            var hasSerializeReference = field.GetCustomAttribute<SerializeReference>() != null;
            if (field.IsPublic || field.GetCustomAttribute<SerializeField>() != null || hasSerializeReference)
            {
                if (field.GetCustomAttribute<NonSerializedAttribute>() != null)
                    return false;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 转义 JSON 字符串
        /// </summary>
        private static string EscapeString(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;

            var sb = new StringBuilder();
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"':
                        sb.Append("\\\"");
                        break;
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }

            return sb.ToString();
        }

        public static void InitRef()
        {
            obj2Id.Clear();
            id2Obj.Clear();
            nextRefId = 1;
        }

        /// <summary>
        /// 序列化引用映射到输出
        /// 新格式: id|locate|typeName, assemblyName
        /// </summary>
        public static void SerializeRef(StringBuilder builder)
        {
            int count = 0;
            foreach (var kvp in id2Obj)
            {
                if (kvp.Value == null) continue;

                string locate = SRLocate.GetLocate(kvp.Value);
                string typeName = kvp.Value.GetType().FullName;
                string assemblyName = kvp.Value.GetType().Assembly.GetName().Name;
                builder.AppendLine($"{kvp.Key}|{locate}|{typeName}, {assemblyName}");
                count++;
            }

            SRIO.ReportCodecEntryCount(count);
        }
    }

    #endregion

    #region SRLocate

    internal static class SRLocate
    {
        public static string GetLocate(Scene source) => source.IsValid() ? $"{source.path}" : "InvalidScene";

        /// <summary>
        /// 获取 UnityEngine.Object 的定位路径
        /// </summary>
        public static string GetLocate(Object source)
        {
            if (source == null) return string.Empty;

            if (source is GameObject go)
                return GetLocate(go);
            if (source is Component comp)
                return GetLocate(comp);
            if (source is ScriptableObject so)
                return GetLocate(so);

            // 其他类型
            return $"@obj:{source.GetType().FullName}:{source.name}";
        }

        /// <summary>
        /// 获取 GameObject 的层级定位路径
        /// 格式: RootName/ChildName... (如果同级有重名，则为 Name:Index)
        /// </summary>
        public static string GetLocate(GameObject source)
        {
            if (source == null) return string.Empty;

            var pathParts = new List<string>();
            var current = source.transform;

            while (current != null)
            {
                var parent = current.parent;
                string namePart = GetNameWithIndex(current, parent);
                pathParts.Add(namePart);
                current = parent;
            }

            pathParts.Reverse();
            return string.Join("/", pathParts);
        }

        private static string GetNameWithIndex(Transform current, Transform parent)
        {
            string name = current.name;
            int siblingCount = 0;
            int myListIndex = 0;

            if (parent == null)
            {
                // Root objects
                var scene = current.gameObject.scene;
                if (!scene.IsValid()) return name; // Should not happen for valid GOs

                var roots = scene.GetRootGameObjects();
                foreach (var root in roots)
                {
                    if (root.name == name)
                    {
                        if (root == current.gameObject) myListIndex = siblingCount;
                        siblingCount++;
                    }
                }
            }
            else
            {
                // Child objects
                for (int i = 0; i < parent.childCount; i++)
                {
                    var child = parent.GetChild(i);
                    if (child.name == name)
                    {
                        if (child == current) myListIndex = siblingCount;
                        siblingCount++;
                    }
                }
            }

            if (siblingCount > 1)
            {
                return $"{name}:{myListIndex}";
            }

            return name;
        }

        /// <summary>
        /// 获取 Component 的定位路径
        /// 格式: GameObjectPath[ComponentType] 或 GameObjectPath[ComponentType:Index]
        /// </summary>
        public static string GetLocate(Component source)
        {
            if (source == null) return string.Empty;

            var goLocate = GetLocate(source.gameObject);
            // Keeping FullName to match previous style roughly, but simplifying separators.
            var componentType = source.GetType().FullName;

            int compIndex = 0;
            int totalComp = 0;
            var components = source.gameObject.GetComponents(source.GetType());
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == source) compIndex = i;
            }

            totalComp = components.Length;

            if (totalComp > 1)
                return $"{goLocate}@{componentType}:{compIndex}";
            else
                return $"{goLocate}@{componentType}";
        }

        /// <summary>
        /// 获取 ScriptableObject 的定位路径
        /// </summary>
        public static string GetLocate(ScriptableObject source)
        {
            if (source == null) return string.Empty;

            // Runtime 环境下无法获取 AssetPath，只能依赖 Name。
            // 格式: @so:TypeName:ObjectName
            return $"@so:{source.GetType().FullName}:{source.name}";
        }
    }

    #endregion

    #region SRExt

    internal static class SRExt
    {
        public static void AddRange<T>(ICollection<T> collection, IEnumerable<T> enumerable)
        {
            foreach (var value in enumerable)
                collection.Add(value);
        }

        public static string FormatAsLine(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Replace("\r", "\\r").Replace("\n", "\\n");
        }
    }

    #endregion

    #endregion
}