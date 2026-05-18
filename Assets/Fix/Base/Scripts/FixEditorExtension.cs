using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Fix.Editor
{
    public static class FixEditorExtension
    {
        public static T Log<T>(this T t)
        {
            Debug.Log(t);
            return t;
        }

        public static T LogError<T>(this T t)
        {
            Debug.LogError(t);
            return t;
        }

        public static T LogException<T>(this T t)
        {
            Debug.LogException(new Exception(t?.ToString()));
            return t;
        }

        public static string ParentPath(this string propertyPath)
        {
            int indexOf;
            return (indexOf = propertyPath.LastIndexOf('.')) != -1 ? propertyPath.Substring(0, indexOf) : string.Empty;
        }

        public static object Find(this object obj, string actions) =>
            actions.Split('.').Aggregate(obj, (o, action) => o.InternalFind(action));

        private static readonly Regex Indexer = new Regex(@"(?<=data\[).*(?=\])");

        private static object InternalFind(this object obj, string action)
        {
            if (string.IsNullOrWhiteSpace(action)) return obj;
            switch (Indexer.IsMatch(action))
            {
                case true:
                    var index = int.Parse(Indexer.Match(action).Value);
                    var array = (Array) obj;
                    return array.GetValue(index);
                case false:
                    switch (action)
                    {
                        case "Array":
                            return ((IEnumerable) obj).OfType<object>().ToArray();
                        default:
                            var type = obj.GetType();
                        {
                            var value = type.GetField(action,
                                BindingFlags.Instance
                                | BindingFlags.Public
                                | BindingFlags.NonPublic
                                | BindingFlags.Default
                                | BindingFlags.GetField)?.GetValue(obj);
                            if (value != null) return value;
                        }
                        {
                            var value = type.GetProperty(action,
                                BindingFlags.Instance
                                | BindingFlags.Public
                                | BindingFlags.NonPublic
                                | BindingFlags.Default
                                | BindingFlags.GetProperty)?.GetValue(obj);
                            if (value != null) return value;
                        }
                            return null;
                    }
                default: return null;
            }
        }

        public static bool TryGet<T>(this IEnumerable<T> source, Predicate<T> predicate, out T result)
        {
            foreach (var t in source)
            {
                if (!predicate(t)) continue;
                result = t;
                return true;
            }

            result = default;
            return false;
        }

        public static void Mkdir(this string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }

        public static void Rmdir(this string path)
        {
            if (Directory.Exists(path)) Directory.Delete(path, true);
        }

        public static void CopyTo(this string source, string target)
        {
            InnerCopyTo(source, target);
        }

        private static void InnerCopyTo(string source, string target)
        {
            target.Mkdir();
            var info = new DirectoryInfo(source);
            foreach (var file in info.GetFiles()) file.CopyTo($"{target}/{file.Name}", true);
            foreach (var dir in info.GetDirectories())
                InnerCopyTo(dir.FullName, $"{target}/{dir.Name}");
        }

        public static int BinaryLE<T>(this IList<T> list, T value, IComparer<T> cmp)
        {
            int r = list.Count - 1;
            if (r < 0 || cmp.Compare(list[r], value) <= 0) return r;
            int l = 0;
            while (l < r)
            {
                int mid = ((r - l) >> 1) + l;
                if (cmp.Compare(list[mid], value) <= 0) l = mid + 1;
                else r = mid;
            }

            return l - 1;
        }

        public static string FormatPath(this string path) => path.Replace("\\", "/");
        public static string ReFormatPath(this string path) => path.Replace("/", "\\");
        public static string GetCurrentUnityPath() => Process.GetCurrentProcess().Modules[0].FileName;
        private static readonly int AssetsNameLenght = "Assets".Length;

        public static string GetWorkspaceFolder(this string workspaceName)
        {
            return $"{Path.GetFullPath("./").FormatPath()}Library/Fix/{workspaceName}";
        }

        public static void ForEachSceneAndPrefab(Action action, bool save = false)
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var path in EditorBuildSettings.scenes.Select(e =>
                    e.path))
                {
                    try
                    {
                        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                        action.Invoke();
                        if (save)
                        {
                            EditorSceneManager.SaveScene(scene);
                        }

                        EditorSceneManager.CloseScene(scene, true);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }

                foreach (var path in AssetDatabase.FindAssets("t:Prefab", new string[] {"Assets"})
                    .Select(AssetDatabase.GUIDToAssetPath))
                {
                    try
                    {
                        var o = PrefabUtility.LoadPrefabContents(path);
                        action.Invoke();
                        if (save)
                        {
                            foreach (var go in o.GetComponentsInChildren<Transform>(true)
                                .Select(e => e.gameObject))
                                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                            PrefabUtility.SaveAsPrefabAsset(o, path);
                        }

                        PrefabUtility.UnloadPrefabContents(o);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        public static void ForEachSceneAndPrefab<T>(Action<T> action) where T : Component
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var scene in EditorBuildSettings.scenes.Select(e =>
                    EditorSceneManager.OpenScene(e.path, OpenSceneMode.Additive)))
                {
                    foreach (var t in scene.GetRootGameObjects()
                        .SelectMany(e => e.GetComponentsInChildren<T>(true)))
                        action.Invoke(t);

                    EditorSceneManager.SaveScene(scene);
                    EditorSceneManager.CloseScene(scene, true);
                }

                foreach (var path in AssetDatabase.FindAssets("t:Prefab", new string[] {"Assets"})
                    .Select(AssetDatabase.GUIDToAssetPath))
                {
                    var o = PrefabUtility.LoadPrefabContents(path);
                    try
                    {
                        foreach (var t in o.GetComponentsInChildren<T>(true))
                            action.Invoke(t);
                        foreach (var go in o.GetComponentsInChildren<Transform>(true)
                            .Select(e => e.gameObject))
                            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                        PrefabUtility.SaveAsPrefabAsset(o, path);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                    finally
                    {
                        PrefabUtility.UnloadPrefabContents(o);
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        public static void SetupForceTextMode()
        {
            if (EditorSettings.serializationMode != SerializationMode.ForceText)
                EditorSettings.serializationMode = SerializationMode.ForceText;
        }

        private static readonly string[] IgnoreExtensions = new string[]
        {
            ".ogg",
            ".wav",
            ".mp3",

            ".mp4",

            ".exe",

            ".png",
            ".jpg",
            ".jpeg",
            ".bmp",

            ".cs",
            ".dll",
            ".txt",

            ".ttf",
            
            ".unitypackage",
            
            ".zip",
        };

        /// <summary>
        /// old to new
        /// </summary>
        /// <param name="replace"></param>
        /// <param name="predicate"></param>
        public static void BatchReplaceRefWithPredicate(IEnumerable<KeyValuePair<string, string>> replace,
            Predicate<string> predicate)
        {
            SetupForceTextMode();
            var array = replace.Distinct().ToArray();
            if (array.Length == 0) return;
            var paths = AssetDatabase
                .GetAllAssetPaths()
                .Distinct()
                .Where(path => path.StartsWith("Assets"))
                .Where(e => predicate?.Invoke(e) ?? true)
                .Where(File.Exists)
                .ToArray();
            if (paths.Length == 0) return;
            AssetDatabase.StartAssetEditing();
            try
            {
                var tasks = new EditorTasks(paths.Select(path =>
                    new Action(() =>
                    {
                        var content = File.ReadAllText(path);
                        if (ReplaceRef(ref content, array)) File.WriteAllText(path, content);
                    })
                )).Start();
                while (!tasks.IsCompleted)
                {
                    EditorUtility.DisplayProgressBar(
                        "正在替换",
                        $"{tasks.CompletedCount}/{tasks.Count}",
                        tasks.Progress);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        /// <summary>
        /// old to new
        /// </summary>
        /// <param name="replace"></param>
        /// <param name="ignoreExtensions"></param>
        public static void BatchReplaceRefWithIgnoreExtensions(IEnumerable<KeyValuePair<string, string>> replace,
            IEnumerable<string> ignoreExtensions)
        {
            var set = new HashSet<string>(ignoreExtensions.Select(e => e.ToLower()));
            BatchReplaceRefWithPredicate(replace, e => !set.Contains(Path.GetExtension(e).ToLower()));
        }

        /// <summary>
        /// old to new
        /// Default Style Of <see cref="BatchReplaceRefWithIgnoreExtensions"/>
        /// </summary>
        /// <param name="replace"></param>
        public static void BatchReplaceRef(IEnumerable<KeyValuePair<string, string>> replace)
        {
            BatchReplaceRefWithIgnoreExtensions(replace, IgnoreExtensions);
        }

        /// <summary>
        /// old to new
        /// </summary>
        /// <param name="replace"></param>
        /// <param name="extensions"></param>
        public static void BatchReplaceRefWithExtensions(IEnumerable<KeyValuePair<string, string>> replace,
            IEnumerable<string> extensions)
        {
            var set = new HashSet<string>(extensions.Select(e => e.ToLower()));
            BatchReplaceRefWithPredicate(replace, e => set.Contains(Path.GetExtension(e).ToLower()));
        }

        public static bool ReplaceRef(ref string content, KeyValuePair<string, string>[] rep)
        {
            bool flag = false;
            foreach (var pair in rep)
            {
                if (!content.Contains(pair.Key)) continue;
                flag = true;
                content = content.Replace(pair.Key, pair.Value);
            }

            return flag;
        }


        public static bool TryGetType(this string typeString, out Type type)
        {
            type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(e => e.GetType(typeString))
                .FirstOrDefault(e => e != null);
            return type != null;
        }

        public static bool TryGetRefString(this Object obj, out string s)
        {
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var guid, out long localId))
            {
                s = $"{{fileID: {localId}, guid: {guid}, type: {obj.GetObjectTypeId()}}}";
                return true;
            }

            s = default;
            return false;
        }

        public static int GetObjectTypeId(this Object obj)
        {
            if (AssetDatabase.IsNativeAsset(obj)) return 2;
            var assetPath = AssetDatabase.GetAssetPath(obj);
            if (assetPath == "Resources/unity_builtin_extra"
                || assetPath == "Library/unity default resources"
                || assetPath == "Resources/tuanjie_builtin_extra"
                || assetPath == "Library/tuanjie default resources"
            ) return 0;
            return 3;
        }

        public static void Clear(this RenderTexture rt)
        {
            if (rt == null) return;
            var active = RenderTexture.active;
            try
            {
                RenderTexture.active = rt;
                GL.Clear(true, true, Color.clear);
            }
            finally
            {
                RenderTexture.active = active;
            }
        }

        public static void DeleteAssets(string[] assetPaths, List<string> failAssets)
        {
#if UNITY_2020_OR_NEWER
            AssetDatabase.DeleteAssets(assetPaths, failAssets);
#else
            failAssets.Clear();
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var assetPath in assetPaths)
                {
                    if (!AssetDatabase.DeleteAsset(assetPath))
                    {
                        failAssets.Add(assetPath);
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
#endif
        }
        public static void DeleteAssets(IEnumerable<string> assetPaths)
        {
#if UNITY_2020_OR_NEWER
            AssetDatabase.DeleteAssets(assetPaths, failAssets);
#else
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var assetPath in assetPaths)
                {
                    AssetDatabase.DeleteAsset(assetPath);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
#endif
        }
        public static IEnumerable<string> GetTotalFiles(this IEnumerable<string> paths)
        {
            var files = new List<string>();
            foreach (var path in paths)
            {
                if (File.Exists(path)) files.Add(path);
                else if (Directory.Exists(path))
                    files.AddRange(Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories));
            }

            return files.Where(e => !".meta".Equals(Path.GetExtension(e), StringComparison.CurrentCultureIgnoreCase))
                .Select(e => e.Replace("\\", "/"));
        }

        public static List<T> GetSubClassInstanceOf<T>()
        {
            var list = new List<T>();
            foreach (var assembly in AppDomain.CurrentDomain
                .GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (!typeof(T).IsAssignableFrom(type) || type.IsAbstract) continue;
                        try
                        {
                            if (Activator.CreateInstance(type) is T t) list.Add(t);
                        }
                        catch
                        {
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            return list;
        }

        public static IDictionary<Type, List<Type>> GetSubClassOf(params Type[] types)
        {
            var result = new Dictionary<Type, List<Type>>();
            foreach (var assembly in AppDomain.CurrentDomain
                .GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        foreach (var baseType in types)
                        {
                            if (baseType.IsAssignableFrom(type) && !type.IsAbstract)
                            {
                                result.AddItem(baseType, type);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            return result;
        }

        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> enumerable)
        {
            foreach (var t in enumerable) collection.Add(t);
        }

        public static void AddItem<TKey, TValue, TCollection>(
            this IDictionary<TKey, TCollection> dictionary,
            TKey key,
            TValue value)
            where TCollection : ICollection<TValue>, new()
        {
            if (!dictionary.TryGetValue(key, out var collection))
                dictionary.Add(key, collection = new TCollection());
            collection.Add(value);
        }

        public static TValue GetOrDefault<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            TKey key,
            Func<TValue> defaultProvider)
        {
            if (!dictionary.TryGetValue(key, out var value))
                value = defaultProvider();
            return value;
        }

        public static bool TryGetResourcesPath(this string assetPath, out string resourcesPath)
        {
            var path = assetPath
                .Substring(0, assetPath.Length - Path.GetExtension(assetPath).Length)
                .Replace("\\", "/");
            var indexOf = path.IndexOf("/Resources/", StringComparison.CurrentCultureIgnoreCase);
            if (indexOf < 0)
            {
                resourcesPath = default;
                return false;
            }

            resourcesPath = path.Substring(indexOf + 11);
            return true;
        }


        public static IEnumerable<string> IgnoreAssetsOf(this IEnumerable<string> paths, string directoryPath)
        {
            var fullPath = Path.GetFullPath(directoryPath);
            foreach (var path in paths)
            {
                if (!File.Exists(path)) continue;
                if (Path.GetFullPath(path).StartsWith(fullPath)) continue;
                yield return path;
            }
        }

        public delegate bool TrySelectPredicate<TIn, TOut>(in TIn tIn, out TOut tOut);

        public static IEnumerable<TOut> TrySelect<TIn, TOut>(this IEnumerable<TIn> enumerable,
            TrySelectPredicate<TIn, TOut> predicate)
        {
            foreach (var tIn in enumerable)
            {
                if (predicate(tIn, out var tOut))
                    yield return tOut;
            }
        }
    }
}