using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Fix.Editor
{
    public class AudioClipEditorUtils : FixEditorWindow
    {
        private const string TipName = "拖入需要裁剪的动画";
        private const string Title = "音频裁剪";
        private static readonly Type AssetType = typeof(AudioClip);
        private static readonly string AssetTypeName = AssetType.Name;

        private static readonly ISet<string> AudioExtensions = new HashSet<string>()
        {
            ".mp3", // Unity 支持，跨平台兼容性好
            ".wav", // Unity 支持，无压缩，高质量
            ".ogg", // Unity 支持，开源格式，适合游戏
            ".aif", // Unity 支持，苹果常用格式
            ".aiff", // Unity 支持，苹果常用格式
            ".aifc", // Unity 支持，压缩的 AIFF
            ".xm", // Unity 支持
            ".mod", // Unity 支持
            ".it", // Unity 支持
            ".s3m", // Unity 支持
        };

        [FixEditor(FixRoot + nameof(AudioClip) + "/" + Title)]
        private static void ShowWindow()
        {
            GetWindowWithRect<AudioClipEditorUtils>(new Rect(Screen.width / 2, Screen.height / 2, 500, 500))
                    .titleContent =
                new GUIContent(Title);
        }

        public static readonly TimeSpan MinLength = new TimeSpan(0, 0, 0, 0, 50);
        private static string Workspace => nameof(AudioClipEditorUtils).GetWorkspaceFolder();

        private static readonly string Prefix = Path.GetFullPath("./").Replace("\\", "/");

        private Dictionary<string, string> Path2Guid = new Dictionary<string, string>();
        private float percentage = 0.2f;
        private readonly ISet<string> filePaths = new HashSet<string>();
        private IAudioCompressor currentCompressor;
        private void Awake() => filePaths.Clear();

        private void OnDestroy() => filePaths.Clear();

        private void OnEnable()
        {
            // currentCompressor = new NAudioCompressor();
            currentCompressor = new FFMpegCompressor();
        }

        private void OnGUI()
        {
            HorizontalRegion(() =>
            {
                if (!GUILayout.Button(new GUIContent($"扫描全部{AssetTypeName}"))) return;
                var paths = Directory
                    .GetFiles(Application.dataPath, "*", SearchOption.AllDirectories)
                    .Where(e => AudioExtensions.Contains(Path.GetExtension(e).ToLower()))
                    .ToArray();
                AddPaths(paths);
            });

            //! 实现拖拽
            DragRegion(path =>
            {
                var paths = GetTotalFiles(path)
                    .Where(e => AudioExtensions.Contains(Path.GetExtension(e).ToLower()));
                AddPaths(paths);
            }, TipName, GUILayout.MinHeight(64), GUILayout.MinWidth(512));

            GUILayout.Space(10);
            HorizontalRegion(() =>
            {
                GUILayout.Label("保留长度");
                percentage = EditorGUILayout.Slider(percentage, 0, 1);
            });
            GUILayout.Space(10);
            HorizontalRegion(() =>
                ColorRegion(new Color(0, 1, 072f, 0.5f), () =>
                    GUILayout.Label($"选择了 {filePaths.Count} 个文件", new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleLeft
                    })));
            GUILayout.Space(240);
            HorizontalRegion(() =>
            {
                if (GUILayout.Button("清除", GUILayout.Width(256), GUILayout.Height(40))) filePaths.Clear();
                if (GUILayout.Button("执行", GUILayout.Width(256), GUILayout.Height(40))) CompressAudioClip();
            });
        }


        private void AddPaths(IEnumerable<string> paths)
        {
            filePaths.AddRange(paths.Select(e => e.Replace("\\", "/"))
                .Select(e => e.StartsWith(Prefix) ? e.Remove(0, Prefix.Length) : e));
        }

        private void CompressAudioClip()
        {
            Init();
            var array = filePaths.ToArray();
            AssetDatabase.StartAssetEditing();

            try
            {
                Path2Guid.Clear();
                foreach (var path in array) Path2Guid.Add(path, AssetDatabase.AssetPathToGUID(path));
                Workspace.Rmdir();
                Workspace.Mkdir();
                var tasks = EditorTasks.ForEach(array, filePath =>
                {
                    var path = filePath;
                    var result = Compress(path);
                    if (!result) return;
                    if (Path.GetExtension(path) != ".ogg") File.Delete(path);
                    RenameMeta(path);
                    ReplaceAudio(path);
                }).Start();
                while (!tasks.IsCompleted)
                {
                    EditorUtility.DisplayProgressBar(
                        "开始处理AudioClip",
                        $"{tasks.CompletedCount}/{tasks.Count}",
                        tasks.Progress);
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

        private void Init()
        {
            currentCompressor.Init();
        }

        private static void RenameMeta(string path)
        {
            var metaPath = $"{path}.meta";
            if (!File.Exists(metaPath)) return;
            var outputPath = Path.Combine(Path.GetDirectoryName(path) ?? string.Empty,
                Path.GetFileNameWithoutExtension(path) + ".ogg.meta");
            if (File.Exists(outputPath)) return;
            File.Move(metaPath, outputPath);
        }

        private string GetOutputPath(string path)
        {
            return Path.Combine(Workspace, Path2Guid[path] + ".ogg");
        }

        private void ReplaceAudio(string path)
        {
            File.Copy(
                GetOutputPath(path),
                Path.Combine(Path.GetDirectoryName(path) ?? string.Empty,
                    Path.GetFileNameWithoutExtension(path) + ".ogg"), true);
        }

        private bool Compress(string path)
        {
            var outputPath = GetOutputPath(path);
            return currentCompressor.Compress(path, outputPath, percentage);
        }
    }

    public interface IAudioCompressor
    {
        void Init();
        bool Compress(string path, string outputPath, float percentage);
    }
}