using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Fix.Editor
{
    public abstract class FixEditorWindow : EditorWindow
    {
        protected const string FixRoot = FixEditorConst.FixRoot;
        private void Awake() => currentDrag = null;

        private void OnDestroy() => currentDrag = null;

        #region utils

        protected internal class Ref<T>
        {
            internal T value;
            public static implicit operator T(Ref<T> handle) => handle.value;
            public static implicit operator Ref<T>(T value) => new Ref<T>() {value = value};
        }

        protected static IEnumerable<string> GetTotalFiles(IEnumerable<string> paths)
        {
            var files = new List<string>();
            foreach (var path in paths)
            {
                if (File.Exists(path)) files.Add(path);
                else if (Directory.Exists(path)) files.AddRange(Directory.EnumerateFiles(path));
            }

            return files.Where(e => !".meta".Equals(Path.GetExtension(e), StringComparison.InvariantCultureIgnoreCase))
                .Select(e => e.Replace("\\", "/"));
        }

        protected void ShowInExplorer(string path)
        {
            if (!Directory.Exists(path))
            {
                ShowNotification(new GUIContent($"路径:\n{path}\n不存在"));
                return;
            }

            var process = Process.Start("Explorer.exe", path.Replace("/", "\\"));
            process?.Dispose();
        }

        protected static string GetWorkspaceFolder(string workspaceName) => workspaceName.GetWorkspaceFolder();

        #endregion

        #region region

        protected static void HorizontalRegion(Action<Rect> action, params GUILayoutOption[] options)
        {
            if (action == null) return;
            var rect = EditorGUILayout.BeginHorizontal(options);
            action.Invoke(rect);
            EditorGUILayout.EndHorizontal();
        }

        protected static void HorizontalRegion(Action action, params GUILayoutOption[] options)
        {
            if (action == null) return;
            GUILayout.BeginHorizontal(options);
            action.Invoke();
            GUILayout.EndHorizontal();
        }

        protected static void VerticalRegion(Action<Rect> action, params GUILayoutOption[] options)
        {
            if (action == null) return;
            var rect = EditorGUILayout.BeginVertical(options);
            action.Invoke(rect);
            GUILayout.EndVertical();
        }

        protected static void VerticalRegion(Action action, params GUILayoutOption[] options)
        {
            if (action == null) return;
            GUILayout.BeginVertical(options);
            action.Invoke();
            GUILayout.EndVertical();
        }

        protected static void ColorRegion(Color color, Action action)
        {
            if (action == null) return;
            var pri = GUI.color;
            GUI.color = color;
            action.Invoke();
            GUI.color = pri;
        }

        protected void StateRegion(bool state, Action action)
        {
            if (action == null) return;
            var b = GUI.enabled;
            GUI.enabled = state;
            action.Invoke();
            GUI.enabled = b;
        }

        protected static void IndexLevelRegion(Action action)
        {
            if (action == null) return;
            EditorGUI.indentLevel++;
            action.Invoke();
            EditorGUI.indentLevel--;
        }

        private static string[] currentDrag;

        protected static void DragRegion(Action<string[]> action, string text, params GUILayoutOption[] options)
        {
            HorizontalRegion(drawRect =>
            {
                GUILayout.Box(text, options);
                UnityEngine.Event currentEvent = UnityEngine.Event.current;
                if (drawRect.Contains(currentEvent.mousePosition))
                {
                    switch (currentEvent.type)
                    {
                        case EventType.DragUpdated:
                            DragAndDrop.visualMode = DragAndDropVisualMode.Generic; //到达目标区域的显示方式
                            currentDrag = DragAndDrop.paths;
                            break;
                        case EventType.DragPerform:
                            if (currentDrag != null)
                            {
                                action?.Invoke(currentDrag);
                                currentDrag = null;
                            }

                            break;
                        case EventType.DragExited:
                            if (currentDrag != null)
                            {
                                action?.Invoke(currentDrag);
                                currentDrag = null;
                            }

                            break;
                    }
                }
            });
        }

        protected static void DragRegion(Action<string[]> action, GUIContent content, params GUILayoutOption[] options)
        {
            HorizontalRegion(drawRect =>
            {
                GUILayout.Box(content, options);
                UnityEngine.Event currentEvent = UnityEngine.Event.current;
                if (drawRect.Contains(currentEvent.mousePosition))
                {
                    switch (currentEvent.type)
                    {
                        case EventType.DragUpdated:
                            DragAndDrop.visualMode = DragAndDropVisualMode.Generic; //到达目标区域的显示方式
                            currentDrag = DragAndDrop.paths;
                            break;
                        case EventType.DragPerform:
                            if (currentDrag != null) action?.Invoke(currentDrag);
                            break;
                        case EventType.DragExited:
                            if (currentDrag != null) action?.Invoke(currentDrag);
                            break;
                    }
                }
            });
        }

        #endregion

        #region module

        protected static void Module(Action action, params IRegionModule[] modules)
        {
            foreach (var module in modules) module.BeginRegion();
            action.Invoke();
            foreach (var module in modules) module.EndRegion();
        }

        protected static IRegionModule ColorR(Color value)
        {
            var r = ColorM.Instance;
            r.cur = value;
            return r;
        }

        protected static IRegionModule StateR(bool value)
        {
            var r = StateM.Instance;
            r.cur = value;
            return r;
        }

        protected static IRegionModule IndexLevelR() => IndexLevelM.Instance;

        protected static IRegionModule ScrollViewR(Ref<Vector2> pos, params GUILayoutOption[] options)
        {
            return ScrollViewR(pos, null, options);
        }

        protected static IRegionModule ScrollViewR(Ref<Vector2> pos, GUIStyle style, params GUILayoutOption[] options)
        {
            var m = ScrollViewM.Instance;
            m.pos = pos;
            m.style = style;
            m.options = options;
            return m;
        }

        protected static IRegionModule VerticalR(params GUILayoutOption[] options)
        {
            var m = VerticalM.Instance;
            m.options = options;
            return m;
        }

        protected static IRegionModule HorizontalR(params GUILayoutOption[] options)
        {
            var m = HorizontalM.Instance;
            m.options = options;
            return m;
        }


        private class ColorM : BaseValueRegionModule<ColorM, Color>
        {
            public override void BeginRegion()
            {
                pri = GUI.color;
                GUI.color = cur;
            }

            public override void EndRegion() => GUI.color = pri;
        }

        private class StateM : BaseValueRegionModule<StateM, bool>
        {
            public override void BeginRegion()
            {
                pri = GUI.enabled;
                GUI.enabled = cur;
            }

            public override void EndRegion() => GUI.enabled = pri;
        }

        private class ScrollViewM : BaseRegionModule<ScrollViewM>
        {
            internal Ref<Vector2> pos;
            internal GUILayoutOption[] options;
            internal GUIStyle style;

            public override void BeginRegion()
            {
                if (style == null)
                    pos.value = EditorGUILayout.BeginScrollView(pos.value, options);
                else
                    pos.value = EditorGUILayout.BeginScrollView(pos.value, style, options);
            }

            public override void EndRegion()
            {
                EditorGUILayout.EndScrollView();
            }
        }

        private class IndexLevelM : BaseRegionModule<IndexLevelM>
        {
            public override void BeginRegion() => EditorGUI.indentLevel++;

            public override void EndRegion() => EditorGUI.indentLevel--;
        }


        private class VerticalM : BaseRegionModule<VerticalM>
        {
            internal GUILayoutOption[] options;
            public override void BeginRegion() => GUILayout.BeginVertical(options);

            public override void EndRegion() => GUILayout.EndVertical();
        }

        private class HorizontalM : BaseRegionModule<HorizontalM>
        {
            internal GUILayoutOption[] options;
            public override void BeginRegion() => GUILayout.BeginHorizontal(options);

            public override void EndRegion() => GUILayout.EndHorizontal();
        }

        protected abstract class BaseValueRegionModule<T, TValue> : BaseRegionModule<T>
            where T : BaseRegionModule<T>, new()
        {
            internal TValue pri, cur;
        }

        protected abstract class BaseRegionModule<T> : IRegionModule
            where T : BaseRegionModule<T>, new()
        {
            public static T Instance => new T();
            public abstract void BeginRegion();
            public abstract void EndRegion();
        }

        protected interface IRegionModule
        {
            void BeginRegion();
            void EndRegion();
        }

        #endregion

        #region component

        protected static void Space(int space = 10) => GUILayout.Space(space);
        protected static void FlexibleSpace() => GUILayout.FlexibleSpace();

        protected static void ColorSpace(Color color, int space = 10)
        {
            HorizontalRegion(rect =>
            {
                Space(space);
                EditorGUI.DrawRect(rect, color);
            }, Height(space));
        }

        protected static GUILayoutOption Width(float value) => GUILayout.Width(value);
        protected static GUILayoutOption Height(float value) => GUILayout.Height(value);

        protected static void Toggle(ref bool value, string text, params GUILayoutOption[] options) =>
            value = GUILayout.Toggle(value, text, options);

        protected static void Toggle(ref bool value, string text, GUIStyle style, params GUILayoutOption[] options) =>
            value = GUILayout.Toggle(value, text, style, options);

        private static void Slider<T>(ref T value, T leftValue, T rightValue, params GUILayoutOption[] options)
            where T : struct, IConvertible
        {
            switch (value)
            {
                case int _:
                    EditorGUILayout.IntSlider((int) (object) value,
                        (int) (object) leftValue, (int) (object) rightValue,
                        options);
                    break;
                case float __:
                    EditorGUILayout.Slider((float) (object) value,
                        (float) (object) leftValue, (float) (object) rightValue,
                        options);
                    break;
                case IConvertible ___:
                    EditorGUILayout.Slider(value.ToSingle(null),
                        leftValue.ToSingle(null), rightValue.ToSingle(null),
                        options);
                    break;
            }
        }

        protected static void Label(string text, params GUILayoutOption[] options) => GUILayout.Label(text, options);

        protected static void Label(string text, GUIStyle style, params GUILayoutOption[] options) =>
            GUILayout.Label(text, style, options);

        protected static void Button(Action action, string text, params GUILayoutOption[] options)
        {
            if (GUILayout.Button(text, options))
                action.Invoke();
        }

        protected static void Button(Action action, string text, GUIStyle style, params GUILayoutOption[] options)
        {
            if (GUILayout.Button(text, style, options))
                action.Invoke();
        }

        protected static void ScrollView<T>(IEnumerable<T> items, Action<T> action, ref Vector2 pos,
            params GUILayoutOption[] options)
        {
            pos = EditorGUILayout.BeginScrollView(pos, options);
            foreach (var item in items) action(item);

            EditorGUILayout.EndScrollView();
        }

        protected static void ScrollView<T>(IEnumerable<T> items, Action<T> action, ref Vector2 pos,
            GUIStyle style, params GUILayoutOption[] options)
        {
            pos = EditorGUILayout.BeginScrollView(pos, style, options);
            foreach (var item in items) action(item);

            EditorGUILayout.EndScrollView();
        }

        #endregion
    }
}