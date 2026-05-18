using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Fix.Editor
{
    public class AssetsOutput : FixEditorWindow
    {
        private const string TipName = "拖入需要导出的源文件";
        private const string Title = "导出资源";

        private readonly List<string> filePaths = new List<string>();

        [FixEditor(FixRoot+"Assets/" + Title)]
        private static void ShowWindow()
        {
            GetWindowWithRect<AssetsOutput>(new Rect(Screen.width / 2, Screen.height / 2, 500, 500)).titleContent =
                new GUIContent(Title);
        }

        private void Awake() => filePaths.Clear();
        private void OnDestroy() => filePaths.Clear();

        private bool keepStyle = true;
        private Vector2 pos;

        private void OnGUI()
        {
            DragRegion(path =>
            {
                var paths = GetTotalFiles(path);
                foreach (var t in paths.Where(e => !filePaths.Contains(e))) filePaths.Add(t);
            }, TipName, GUILayout.MinHeight(64), GUILayout.MinWidth(512));

            GUILayout.Space(10);
            HorizontalRegion(() => { keepStyle = GUILayout.Toggle(keepStyle, "保持文件格式"); });

            GUILayout.Space(10);
            HorizontalRegion(() =>
            {
                ColorRegion(new Color(0, 1, 072f, 0.5f), () =>
                {
                    var fileCountLabel = new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleLeft};
                    GUILayout.Label($"选择了 {filePaths.Count} 个文件", fileCountLabel);
                });
            });

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
                            GUI.color = Color.white;
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

            GUI.color = Color.white;
            GUILayout.Space(20);
            HorizontalRegion(() =>
            {
                if (GUILayout.Button("清除", GUILayout.Width(256), GUILayout.Height(40))) filePaths.Clear();
                if (GUILayout.Button("导出", GUILayout.Width(256), GUILayout.Height(40))) OutputFile();
            });
        }

        private void OutputFile()
        {
            if (filePaths.Count == 0)
            {
                ShowNotification(new GUIContent("未选择任何资源文件"));
                return;
            }

            var outputRootPath = EditorUtility.OpenFolderPanel("选择导出路径", "", "");
            if (!Directory.Exists(outputRootPath))
            {
                ShowNotification(new GUIContent("路径不存在"));
                return;
            }

            outputRootPath = $"{outputRootPath}/Output-{DateTime.Now:yyyyMMdd-HHmmss}";
            int fc = 0, sc = 0;
            foreach (var filePath in filePaths)
            {
                try
                {
                    string path = filePath;
                    if (!keepStyle) path = new FileInfo(path).Name;
                    var info = new FileInfo($"{outputRootPath}/{path}");
                    if (!info.Directory.Exists) Directory.CreateDirectory(info.Directory.FullName);
                    File.Copy(filePath, info.FullName, true);
                    sc++;
                }
                catch
                {
                    fc++;
                }
            }

            ShowNotification(new GUIContent($"导出完成,共:{filePaths.Count},成功:{sc},失败:{fc}"));
            ShowInExplorer(outputRootPath);
        }
    }
}