using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Fix.Editor
{
    public class FixEditorManager : FixEditorWindow
    {
        #region init

        [InitializeOnLoadMethod]
        private static void InitEditor()
        {
            LoadMenuItem();
            CompilationPipeline.compilationFinished += Check;
        }

        private static void Check(object obj) => LoadMenuItem();


        private class NodeCollection : IList<Node>
        {
            private readonly List<Node> _items; // 原始无序列表
            private List<Node> _sortedView; // 排序后的缓存视图
            private bool _dirty; // 是否需要重新排序
            private readonly IComparer<Node> _comparer; // 比较器

            public NodeCollection()
            {
                _items = new List<Node>();
                _sortedView = null;
                _dirty = false;
                _comparer = new NodeComparer();
            }

            // 确保排序视图有效
            private void EnsureSorted()
            {
                if (_sortedView == null || _dirty)
                {
                    _sortedView = _items.OrderBy(x => x, _comparer).ToList();
                    _dirty = false;
                }
            }

            // 标记修改，使缓存失效
            private void InvalidateCache()
            {
                _dirty = true;
                _sortedView = null;
            }

            // ---------- IList<Node> 成员 ----------

            public Node this[int index]
            {
                get
                {
                    EnsureSorted();
                    if (index < 0 || index >= _sortedView.Count)
                        throw new ArgumentOutOfRangeException(nameof(index));
                    return _sortedView[index];
                }
                set
                {
                    if (index < 0 || index >= Count)
                        throw new ArgumentOutOfRangeException(nameof(index));

                    EnsureSorted();
                    Node oldNode = _sortedView[index];

                    // 从原始列表中移除旧节点
                    _items.Remove(oldNode);
                    // 添加新节点
                    _items.Add(value);
                    InvalidateCache();
                }
            }

            public Node this[string name] => _items.Find(e => e.name == name);
            public int Count => _items.Count;

            public bool IsReadOnly => false;

            public void Add(Node item)
            {
                if (item == null) throw new ArgumentNullException(nameof(item));
                _items.Add(item);
                InvalidateCache();
            }

            public void Clear()
            {
                _items.Clear();
                InvalidateCache();
            }

            public bool Contains(Node item)
            {
                EnsureSorted();
                return _sortedView.Contains(item); // 基于排序视图查找（引用相等）
            }

            public void CopyTo(Node[] array, int arrayIndex)
            {
                EnsureSorted();
                _sortedView.CopyTo(array, arrayIndex);
            }

            public IEnumerator<Node> GetEnumerator()
            {
                EnsureSorted();
                return _sortedView.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public int IndexOf(Node item)
            {
                EnsureSorted();
                return _sortedView.IndexOf(item); // 返回在排序视图中的索引
            }

            // 插入操作：由于集合是有序的，插入的索引无意义，故直接添加元素并重新排序。
            // 若需要严格按索引插入（破坏排序），可改为抛出 NotSupportedException。
            public void Insert(int index, Node item)
            {
                // 忽略 index 参数，直接添加（保持有序性）
                Add(item);
            }

            public bool Remove(Node item)
            {
                bool removed = _items.Remove(item);
                if (removed)
                    InvalidateCache();
                return removed;
            }

            public void RemoveAt(int index)
            {
                EnsureSorted();
                if (index < 0 || index >= _sortedView.Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                Node node = _sortedView[index];
                _items.Remove(node);
                InvalidateCache();
            }

            // ---------- 比较器实现 ----------
            private class NodeComparer : IComparer<Node>
            {
                public int Compare(Node x, Node y)
                {
                    if (ReferenceEquals(x, y)) return 0;
                    if (x == null) return -1; // null 排在前（可根据需求调整）
                    if (y == null) return 1;

                    // 优先按 methodPair 是否为空：不为空的排在前面
                    bool xHasMethod = x.methodPair != null;
                    bool yHasMethod = y.methodPair != null;
                    if (xHasMethod && !yHasMethod) return -1;
                    if (!xHasMethod && yHasMethod) return 1;

                    // 如果 methodPair 都为空或都不为空，按 name 排序（默认字典序）
                    return string.Compare(x.name, y.name, StringComparison.Ordinal);
                }
            }
        }


        private sealed class Node
        {
            public readonly string name;
            public NodeCollection children = new NodeCollection();
            public MethodPair methodPair;

            public Node(string name)
            {
                this.name = name;
            }

            private bool Equals(Node other)
            {
                return name == other.name;
            }

            public override bool Equals(object obj)
            {
                return ReferenceEquals(this, obj) || obj is Node other && Equals(other);
            }

            public override int GetHashCode()
            {
                return (name != null ? name.GetHashCode() : 0);
            }
        }

        private class MethodPair
        {
            public MethodInfo method;
            public MethodInfo validateMethod;

            public bool IsValid
            {
                get
                {
                    try
                    {
                        if (validateMethod != null)
                            return (bool) validateMethod.Invoke(null, Parameters);
                    }
                    catch
                    {
                        return false;
                    }

                    return true;
                }
            }
        }

        private static readonly NodeCollection Roots = new NodeCollection();
        private static readonly object[] Parameters = new object[0];

        private static void LoadMenuItem()
        {
            if (BuildPipeline.isBuildingPlayer) return;
            Roots.Clear();
            foreach (var valueTuple in FixEditorExtension.GetSubClassOf(
                    typeof(FixEditorWindow),
                    typeof(FixEditorBase))
                .SelectMany(e => e.Value)
                .SelectMany(GetAttrMethod))
            {
                LocateNode(valueTuple.menuItem).methodPair = new MethodPair()
                {
                    method = valueTuple.method,
                    validateMethod = valueTuple.validateMethod
                };
            }
        }

        private static Node LocateNode(string menuItem)
        {
            var step = FixEditorAttribute.Step(menuItem);
            var t = Roots;
            Node node = null;
            foreach (var s in step)
            {
                node = t[s];
                if (node == null) t.Add(node = new Node(s));
                t = node.children;
            }

            return node;
        }

        private static IEnumerable<(string menuItem, MethodInfo method, MethodInfo validateMethod)>
            GetAttrMethod(Type type)
        {
            var dictionary = new Dictionary<string, (MethodInfo method, MethodInfo validateMethod)>();
            var methodInfos = type
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .Where(e => e.IsDefined(typeof(FixEditorAttribute)))
                .Where(e => e.GetParameters().Length == 0)
                .Where(e => !e.IsGenericMethod)
                .ToDictionary(e => e, CustomAttributeExtensions.GetCustomAttribute<FixEditorAttribute>);
            foreach (var pair in methodInfos)
            {
                var attr = pair.Value;
                var methodInfo = pair.Key;
                if (attr.validate || string.IsNullOrWhiteSpace(attr.menuItem)) continue;
                dictionary.Add(attr.menuItem, (methodInfo, null));
            }

            foreach (var pair in methodInfos)
            {
                var attr = pair.Value;
                var methodInfo = pair.Key;
                if (!attr.validate
                    || methodInfo.ReturnType != typeof(bool)
                    || !dictionary.TryGetValue(attr.menuItem, out var tuple)) continue;
                dictionary[attr.menuItem] = (tuple.method, methodInfo);
            }

            foreach (var pair in dictionary)
            {
                yield return (pair.Key, pair.Value.method, pair.Value.validateMethod);
            }
        }

        #endregion

        private const string Title = "Others";

        [MenuItem(FixRoot + Title)]
        private static void ShowWindow()
        {
            GetWindowWithRect<FixEditorManager>(new Rect(Screen.width / 2, Screen.height / 2, 500, 500)).titleContent =
                new GUIContent(Title);
        }

        private IDictionary<Node, bool> foldoutMemo = new Dictionary<Node, bool>();
        private Vector2 pos;

        private void OnGUI()
        {
            pos = EditorGUILayout.BeginScrollView(pos, Height(450));
            foreach (var node in Roots) DrawNode(node);
            EditorGUILayout.EndScrollView();
            Space(40);
            ColorSpace(new Color(0.4f, 0.7f, 0.9f), 10);
        }

        private void DrawNode(Node node)
        {
            EditorGUILayout.BeginHorizontal();
            if (node.methodPair != null)
            {
                Space(250);
                using (new EditorGUI.DisabledScope(!node.methodPair.IsValid))
                {
                    if (GUILayout.Button(node.name, Height(28)))
                    {
                        try
                        {
                            node.methodPair.method?.Invoke(null, Parameters);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }
                }

                Space(20);
            }

            bool drawChildren = node.children.Count > 0;
            if (drawChildren)
            {
                drawChildren &= foldoutMemo[node] =
                    EditorGUILayout.Foldout(foldoutMemo.GetOrDefault(node, () => true), node.name, true,
                        new GUIStyle(EditorStyles.foldout)
                        {
                            fontSize = 16
                        });
            }

            EditorGUILayout.EndHorizontal();
            if (!drawChildren) return;
            EditorGUI.indentLevel++;
            foreach (var child in node.children)
            {
                Space(10);
                DrawNode(child);
            }

            EditorGUI.indentLevel--;
        }
    }
}