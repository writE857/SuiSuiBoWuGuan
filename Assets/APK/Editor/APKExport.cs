using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace APK.Editor
{
    public class APKExport : EditorWindow
    {
        private const int Width = 540;
        private const int Height = 540;

        private BuildTarget target;
        private BuildTargetGroup targetGroup;
        private PropertyInfo exportProp;

        [MenuItem("Tools/导出APK", false, int.MaxValue)]
        public static void Export()
        {
            GetWindowWithRect<APKExport>(
                new Rect(Screen.width / 2, Screen.height / 2, Width, Height)
            ).titleContent = new GUIContent("导出APK");
        }

        private void OnEnable()
        {
            try
            {
                target = (BuildTarget) Enum
                    .Parse(typeof(BuildTarget), typeof(BuildTarget)
                        .GetEnumNames()
                        .First(e => e == "OpenHarmony"));
                targetGroup = (BuildTargetGroup) Enum
                    .Parse(typeof(BuildTargetGroup), target.ToString());
                exportProp = typeof(EditorUserBuildSettings).GetProperty("exportAsOpenHarmonyProject");
            }
            catch
            {
                target = BuildTarget.Android;
                targetGroup = (BuildTargetGroup) Enum
                    .Parse(typeof(BuildTargetGroup), target.ToString());
                exportProp = typeof(EditorUserBuildSettings).GetProperty("exportAsGoogleAndroidProject");
            }

            outputPath = EditorUserBuildSettings.GetBuildLocation(target) ?? "";
            dirs = typeof(UIOrientation).GetEnumNames();
            dirValues = new int[dirs.Length];
            for (var i = 0; i < dirValues.Length; i++) dirValues[i] = i;
            dirIndex = Math.Max(0, Array.IndexOf(dirs, PlayerSettings.defaultInterfaceOrientation.ToString()));
        }

        private string outputPath = "";
        private int dirIndex;
        private string[] dirs;
        private int[] dirValues;

        private void OnGUI()
        {
			GUILayout.Label($"当前构建平台:{target}");
            GUILayout.Space(50);
            HorizontalRegion(() =>
            {
                GUILayout.Label("屏幕方向:", GUILayout.Width(60), GUILayout.Height(24));
                dirIndex = EditorGUILayout.IntPopup(dirIndex, dirs, dirValues, GUILayout.Width(180),
                    GUILayout.Height(24));
            });

            GUILayout.Space(30);
            HorizontalRegion(() =>
            {
                GUILayout.Label("导出路径:", GUILayout.Width(60), GUILayout.Height(24));
                GUILayout.Box(outputPath, GUILayout.ExpandWidth(true), GUILayout.Height(24));
            });
            GUILayout.Space(50);
            HorizontalRegion(() =>
            {
                GUILayout.Space(Width / 2 - 100);
                if (GUILayout.Button("选择", GUILayout.Width(85), GUILayout.Height(28)))
                {
                    var path = EditorUtility.OpenFolderPanel("选择导出目录",
                        Directory.Exists(outputPath) ? Path.GetDirectoryName(outputPath) ?? "" : "", "");
                    if (!string.IsNullOrWhiteSpace(path)) outputPath = path.Replace("\\", "/");
                }

                GUILayout.Space(10);
                if (GUILayout.Button("打开", GUILayout.Width(85), GUILayout.Height(28)))
                {
                    ShowInExplorer(outputPath);
                }

                GUILayout.Space(Width / 2 - 100);
            });
            GUILayout.Space(30);
            HorizontalRegion(() =>
            {
                GUILayout.Space(Width / 2 - 100);
                ColorRegion(new Color(0.9f, 0.3f, 0.3f), () =>
                {
                    if (GUILayout.Button("清空目标文件夹", new GUIStyle(GUI.skin.button)
                    {
                        fontSize = 10,
                    }, GUILayout.Width(85), GUILayout.Height(28)))
                        ClearDirectoryWithDialog(outputPath);
                });
                GUILayout.Space(10);

                if (GUILayout.Button("删除无用场景", GUILayout.Width(85), GUILayout.Height(28))) DeleteNoUseScene();
                GUILayout.Space(Width / 2 - 100);
            });
            GUILayout.Space(30);
            HorizontalRegion(() =>
            {
                ColorRegion(new Color(0f, 1f, 0.6f), () =>
                {
                    GUILayout.Space(Width / 2 - 100);
                    if (GUILayout.Button("导出", GUILayout.Width(185), GUILayout.Height(30))) DoExport();
                });
            });
        }

        private void DoExport()
        {
            AssetDatabase.SaveAssets();
            if (!Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);
            if (!IsEmptyOutputDirectory())
            {
                switch (EditorUtility.DisplayDialogComplex("删除文件夹", "导出文件夹不为空\n是否删除并继续构建"
                    , "是的", "取消构建", "继续构建"))
                {
                    case 0:
                        ClearDirectory(outputPath);
                        break;
                    case 1:
                        return;
                    case 2:
                        break;
                }
            }


            if (EditorUserBuildSettings.activeBuildTarget != target)
            {
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(targetGroup, target))
                {
                    ShowNotification(new GUIContent($"不支持{target}平台"));
                    return;
                }
            }


            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.defaultInterfaceOrientation =
                (UIOrientation) Enum.Parse(typeof(UIOrientation), dirs[dirIndex]);
            PlayerSettings.use32BitDisplayBuffer = true;
            PlayerSettings.bundleVersion = "1.0";
            PlayerSettings.SetScriptingBackend(targetGroup, ScriptingImplementation.IL2CPP);

            var types = typeof(PlayerSettings).GetNestedTypes();
            var dictionary = new Dictionary<string, Type>();
            foreach (var t in types)
            {
                var lower = t.Name.ToLower();
                if (!dictionary.ContainsKey(lower)) dictionary.Add(lower, t);
            }

            if (dictionary.TryGetValue(target.ToString().ToLower(), out var type))
            {
                type.GetProperty("bundleVersionCode").SetValue(null, 10);
                var targetArchitecturesProp = type.GetProperty("targetArchitectures");
                var enumType = targetArchitecturesProp.PropertyType;
                var targetArchitectures =
                    enumType.GetEnumNames().ToDictionary(e => e, e => (uint) Enum.Parse(enumType, e));
                targetArchitecturesProp.SetValue(null,
                    Enum.ToObject(enumType, targetArchitectures["ARM64"] | targetArchitectures["ARMv7"]));
            }

            exportProp?.SetValue(null, true);
            EditorUserBuildSettings.SetBuildLocation(target, outputPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();


            var options = new BuildPlayerOptions
            {
                scenes = GetScenes(),
                target = target,
                targetGroup = targetGroup,
                locationPathName = outputPath
            };
            var report = BuildPipeline.BuildPlayer(options);

            if (report.summary.result == BuildResult.Succeeded) ShowInExplorer(outputPath);
            else ShowNotification(new GUIContent("构建失败"));
        }

        private void ShowInExplorer(string path)
        {
            if (!Directory.Exists(path))
            {
                ShowNotification(new GUIContent($"路径:\n{path}\n不存在"));
                return;
            }

            var process = Process.Start("Explorer.exe", path.Replace("/", "\\"));
            process?.Dispose();
        }

        private void ClearDirectoryWithDialog(string path)
        {
            if (!Directory.Exists(path))
            {
                ShowNotification(new GUIContent($"路径:\n{path}\n不存在"));
                return;
            }

            var selected = EditorUtility.DisplayDialogComplex("清空文件夹", "确认清除?", "确认", "取消", "");
            switch (selected)
            {
                case 0:
                    ClearDirectory(path);
                    break;
                case 1:
                    ShowNotification(new GUIContent("已取消"));
                    break;
            }
        }

        private void DeleteNoUseScene()
        {
            EditorBuildSettings.scenes =
                EditorBuildSettings.scenes.Where(e => File.Exists(e.path) && e.enabled).ToArray();
            ShowNotification(new GUIContent("已删除"));
        }

        private bool IsEmptyOutputDirectory() =>
            Directory.GetDirectories(outputPath).Length + Directory.GetFiles(outputPath).Length == 0;

        private static string[] GetScenes()
        {
            return EditorBuildSettings
                .scenes
                .Where(e => e.enabled)
                .Select(e => e.path)
                .ToArray();
        }

        private static void ClearDirectory(string path)
        {
            foreach (var directory in Directory.GetDirectories(path)) Directory.Delete(directory, true);
            foreach (var file in Directory.GetFiles(path)) File.Delete(file);
        }

        protected static void HorizontalRegion(Action<Rect> action)
        {
            if (action == null) return;
            var rect = EditorGUILayout.BeginHorizontal();
            action.Invoke(rect);
            EditorGUILayout.EndHorizontal();
        }

        protected static void HorizontalRegion(Action action)
        {
            if (action == null) return;
            GUILayout.BeginHorizontal();
            action.Invoke();
            GUILayout.EndHorizontal();
        }

        protected static void VerticalRegion(Action action)
        {
            if (action == null) return;
            GUILayout.BeginVertical();
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
    }
}