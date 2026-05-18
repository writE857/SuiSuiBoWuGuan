using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Fix.Editor
{
    public class AnimShrink : FixEditorWindow
    {
        private const string TipName = "拖入需要裁剪的动画";
        private const string Title = "裁剪动画";

        [FixEditor(FixRoot + nameof(AnimationClip) + "/" + Title)]
        [MenuItem(FixRoot + Title, priority = 103)]
        private static void ShowWindow()
        {
            GetWindowWithRect<AnimShrink>(new Rect(Screen.width / 2, Screen.height / 2, 500, 500)).titleContent =
                new GUIContent(Title);
        }

        private readonly List<string> filePaths = new List<string>();

        private void Awake() => filePaths.Clear();

        private void OnDestroy() => filePaths.Clear();

        private Vector2 pos;
        private int space = 1;

        private void OnGUI()
        {
            //! 实现拖拽
            DragRegion(path =>
            {
                var paths = GetTotalFiles(path)
                    .Where(e => !filePaths.Contains(e))
                    .Distinct()
                    .Where(e => ".anim".Equals(Path.GetExtension(e), StringComparison.InvariantCultureIgnoreCase));
                filePaths.AddRange(paths);
            }, TipName, GUILayout.MinHeight(64), GUILayout.MinWidth(512));

            GUILayout.Space(10);
            HorizontalRegion(() =>
            {
                GUILayout.Label("采样率");
                space = EditorGUILayout.IntSlider(space, 0, 100);
            });

            GUILayout.Space(10);
            HorizontalRegion(() =>
                ColorRegion(new Color(0, 1, 072f, 0.5f), () =>
                    GUILayout.Label($"选择了 {filePaths.Count} 个文件", new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleLeft
                    })));

            GUILayout.Space(10);
            HorizontalRegion(() =>
            {
                VerticalRegion(() =>
                {
                    pos = GUILayout.BeginScrollView(pos, GUILayout.MaxHeight(400), GUILayout.MinHeight(20));
                    var itemLabel = new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleLeft};
                    var itemBox = new GUIStyle(GUI.skin.box) {alignment = TextAnchor.MiddleLeft};
                    foreach (var path in filePaths.ToArray())
                    {
                        HorizontalRegion(() =>
                        {
                            GUILayout.Label(AssetDatabase.GetCachedIcon(path), itemBox,
                                GUILayout.Width(20), GUILayout.Height(20));
                            ColorRegion(new Color(0.9411765f, 0.9019608f, 0.5490196f, 1f),
                                () => { GUILayout.Label(Path.GetFileNameWithoutExtension(path), itemLabel); });
                            if (GUILayout.Button("X", GUILayout.Width(20))) filePaths.Remove(path);
                        });
                    }

                    GUILayout.EndScrollView();
                });
            });

            GUILayout.Space(20);
            HorizontalRegion(() => GUILayout.Label("1. 可以从资源管理器拖入"));
            HorizontalRegion(() => GUILayout.Label("2. 1 + 1 = 3"));

            GUILayout.Space(20);
            HorizontalRegion(() =>
            {
                if (GUILayout.Button("清除", GUILayout.Width(256), GUILayout.Height(40))) filePaths.Clear();
                if (GUILayout.Button("执行", GUILayout.Width(256), GUILayout.Height(40))) ShrinkAnim();
            });
        }

        private void ShrinkAnim()
        {
            if (filePaths.Count == 0 || space <= 0) return;
            AssetDatabase.StartAssetEditing();
            try
            {
                int c = 0, total = filePaths.Count;
                foreach (var path in filePaths)
                {
                    if (EditorUtility.DisplayCancelableProgressBar(
                        "开始处理",
                        $"{c + 1}/{total}",
                        (float) c / total))
                        break;
                    c++;
                    var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                    var bindings = AnimationUtility.GetCurveBindings(clip);
                    foreach (var binding in bindings)
                    {
                        var curve = AnimationUtility.GetEditorCurve(clip, binding);
                        var keys = curve.keys;
                        if (keys.Length <= 2) continue;
                        curve.keys = keys.Where((t, i) => i % (space + 1) != 0 || i == 0 || i == keys.Length - 1)
                            .ToArray();
                        AnimationUtility.SetEditorCurve(clip, binding, curve);
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
    }
}