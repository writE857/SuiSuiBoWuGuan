using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Fix.Editor
{
    public class NGUIFix : FixEditorWindow
    {
        [InitializeOnLoadMethod]
        private static void InitEditor()
        {
            FixEditorDef.RegisterDef(new FixEditorDef.Def(
                "NGUITools, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "USE_NGUI"));
        }

        private const string Title = "NGUI修复";
        [FixEditor(FixRoot + "NGUI/" + Title)]
        private static void ShowWindow()
        {
#if USE_NGUI
            var window = GetWindowWithRect<NGUIFix>(new Rect(0, 0, 512, 512));
            window.titleContent = new GUIContent(Title);
            window.Show();       
#else
            Debug.LogError("未检测到NGUI");
#endif
            
        }
#if USE_NGUI
        private const string InitUnityPackageMsg = "拖入NGUI包(.unitypackage)";
        private const string WorkspaceName = "Workspace_NGUIFix";
        private const string FileMatchTask = nameof(FileMatchTask);
        private const string NGUINotExist = "NGUI未解析";
        private const string IgnoreFolder = "Editor";
        private const string IgnoreFileExtension = ".meta";

        private static readonly string[] MatchExtensions =
            new[]
            {
                ".cs",
                // ".shader",
            };


        private static readonly IComparer<int> IntComparer = Comparer<int>.Default;
        private static string WorkspaceFolder => GetWorkspaceFolder(WorkspaceName);

        private string unityPackagePath = InitUnityPackageMsg;
        private TaskManager taskManager = new TaskManager();
        private bool matchWithContent = false;

        private void OnGUI()
        {
            StateRegion(!taskManager.IsTaskProcessing(FileMatchTask), () =>
            {
                HorizontalRegion(drawRect =>
                {
                    GUILayout.Box(unityPackagePath, GUILayout.Height(40), GUILayout.ExpandWidth(true));
                    UnityEngine.Event currentEvent = UnityEngine.Event.current;
                    //拖拽范围内
                    if (!drawRect.Contains(currentEvent.mousePosition)) return;
                    switch (currentEvent.type)
                    {
                        case EventType.DragUpdated:
                            DragAndDrop.visualMode = DragAndDropVisualMode.Generic; //到达目标区域的显示方式
                            break;
                        case EventType.DragPerform:
                            var path = GetTotalFiles(DragAndDrop.paths)
                                .FirstOrDefault(e =>
                                    e.EndsWith(".unitypackage", StringComparison.CurrentCultureIgnoreCase));

                            unityPackagePath = !string.IsNullOrWhiteSpace(path)
                                ? path.FormatPath()
                                : InitUnityPackageMsg;
                            break;
                    }
                });
                GUILayout.Space(20);
                HorizontalRegion(() =>
                {
                    if (GUILayout.Button("解析", GUILayout.Height(30)))
                    {
                        UnPack(
                            () => EditorApplication.delayCall += () => ShowNotification(new GUIContent("解包完成")),
                            () => EditorApplication.delayCall += () => ShowNotification(new GUIContent("解包失败"))
                        );
                    }
                });
                GUILayout.Space(20);
                HorizontalRegion(() =>
                {
                    ColorRegion(Color.cyan, () =>
                    {
                        GUILayout.Label("匹配度:", GUILayout.Height(36));
                        GUILayout.Label(!taskManager.HasTask(FileMatchTask)
                            ? "未进行匹配"
                            : !taskManager.IsTaskDone(FileMatchTask, out var task)
                                ? "匹配中"
                                : $"匹配度{(task as Task<float>).Result * 100f}%", GUILayout.Height(36));
                    });
                });

                HorizontalRegion(() =>
                {
                    matchWithContent = GUILayout.Toggle(matchWithContent, "使用深度匹配", GUILayout.Height(36));
                    if (GUILayout.Button("匹配", GUILayout.Height(36)))
                    {
                        taskManager.AddTask(FileMatchTask, !matchWithContent ? FileMatch() : FileMatchWithContent());
                    }
                });
                GUILayout.Space(10);
                ColorRegion(new Color(1f, 0.3882353f, 0.2784314f, 1f), () =>
                {
                    if (matchWithContent)
                    {
                        HorizontalRegion(() => GUILayout.Label($"额外检测{string.Join("、", MatchExtensions)}的内容"));
                        HorizontalRegion(() => GUILayout.Label("这会花费额外的时间"));
                    }
                    else
                    {
                        HorizontalRegion(() => GUILayout.Label("只会匹配文件名"));
                    }
                });

                GUILayout.Space(40);
                HorizontalRegion(() =>
                {
                    GUILayout.Space(145);
                    if (GUILayout.Button("替换", GUILayout.Width(200), GUILayout.Height(40)))
                    {
                        Replace();
                    }
                });
            });
        }

        private void OnDestroy()
        {
            taskManager.Dispose();
            taskManager = null;
        }

        #region unpack

        private CancellationTokenSource cts;
        private Task unPackTask;

        private void UnPack(Action success = null, Action failure = null, Action completed = null)
        {
            if (!File.Exists(unityPackagePath)) throw new Exception($"路径:\n{unityPackagePath}\n不存在");
            if (unPackTask != null)
            {
                ShowNotification(new GUIContent("解包中"));
                return;
            }

            WorkspaceFolder.Rmdir();
            WorkspaceFolder.Mkdir();
            completed += () =>
            {
                cts = null;
                unPackTask = null;
            };
            cts = new CancellationTokenSource();
            unPackTask = UnpackUnityPackage(unityPackagePath, WorkspaceFolder, cts.Token, success, failure, completed);
            while (unPackTask != null)
            {
                EditorUtility.DisplayProgressBar("解压中", "", 0.5f);
                Thread.Sleep(200);
            }
            EditorUtility.ClearProgressBar();
        }

        private static Task UnpackUnityPackage(string upPath, string outputPath,
            CancellationToken token,
            Action success, Action failure, Action completed)
        {
            return Task.Run(() =>
            {
                try
                {
                    UnityPackageUtils.UnPack(upPath.FormatPath(), outputPath.FormatPath());
                    $"{outputPath}/Assets/NGUI/Examples".Rmdir();
                    foreach (var readMeFile in Directory.GetFiles($"{outputPath}/Assets/NGUI")
                        .Where(e => e.Contains("ReadMe")))
                        File.Delete(readMeFile);
                    success?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.Log(e);
                    failure?.Invoke();
                    throw;
                }
                finally
                {
                    completed?.Invoke();
                }
            }, token);
        }

        #endregion

        #region match

        private async Task<float> FileMatch()
        {
            var rootFolder = $"{WorkspaceFolder}/Assets/NGUI";
            if (!Directory.Exists(rootFolder))
            {
                EditorApplication.delayCall += () => ShowNotification(new GUIContent(NGUINotExist));
                return 0;
            }

            var sourceFiles = new List<string>();
            var rootInfo = new DirectoryInfo(rootFolder);
            var queue = new Queue<DirectoryInfo>();
            queue.Enqueue(rootInfo);
            while (queue.Count != 0)
            {
                var info = queue.Dequeue();
                if (Equals(info.Name, IgnoreFolder)) continue;
                sourceFiles.AddRange(info.GetFiles()
                    .Where(e => !IgnoreFileExtension.Equals(e.Extension, StringComparison.InvariantCultureIgnoreCase))
                    .Select(e => e.Name).Select(e => e.ToLower()));
                foreach (var next in info.GetDirectories()) queue.Enqueue(next);
            }

            string[] assetPaths = null;
            EditorApplication.delayCall += () => assetPaths = AssetDatabase.GetAllAssetPaths();
            while (assetPaths == null) await Task.Delay(500);
            var count = sourceFiles
                .Intersect(assetPaths.Where(File.Exists).Select(Path.GetFileName).Select(e => e.ToLower())).Count();
            return (float) count / sourceFiles.Count;
        }


        private async Task<float> FileMatchWithContent()
        {
            var rootFolder = $"{WorkspaceFolder}/Assets/NGUI";
            if (!Directory.Exists(rootFolder))
            {
                EditorApplication.delayCall += () => ShowNotification(new GUIContent(NGUINotExist));
                return 0;
            }

            var sourceFiles = new Dictionary<string, FileInfo>();
            var rootInfo = new DirectoryInfo(rootFolder);
            var queue = new Queue<DirectoryInfo>();
            queue.Enqueue(rootInfo);
            while (queue.Count != 0)
            {
                var info = queue.Dequeue();
                if (Equals(info.Name, IgnoreFolder)) continue;
                foreach (var fileInfo in info.GetFiles().Where(e =>
                    !IgnoreFileExtension.Equals(e.Extension, StringComparison.InvariantCultureIgnoreCase)))
                    sourceFiles.Add(fileInfo.Name.ToLower(), fileInfo);
                foreach (var next in info.GetDirectories()) queue.Enqueue(next);
            }

            float totalPoints = sourceFiles.Count, current = 0;
            string[] assetPaths = null;
            EditorApplication.delayCall += () => assetPaths = AssetDatabase.GetAllAssetPaths();
            while (assetPaths == null) await Task.Delay(500);
            foreach (var assetPath in AssetDatabase.GetAllAssetPaths())
            {
                var name = Path.GetFileName(assetPath).ToLower();
                if (!sourceFiles.TryGetValue(name, out var info)) continue;
                if (!MatchExtensions.Any(e => e.Equals(info.Extension, StringComparison.InvariantCultureIgnoreCase)))
                    current++;
                else
                {
                    var match = Match(File.ReadAllText(info.FullName),
                        File.ReadAllText(assetPath));
                    current += match;
                    // $"{assetPath} provide:{match}".Log();
                }

                sourceFiles.Remove(name);
            }

            return current / totalPoints;
        }


        private static float Match(string source, string target)
        {
            string[] sourceSplit =
                    source.Split(new char[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries),
                targetSplit = target.Split(new char[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
            int m = sourceSplit.Length;
            var dictionary = new Dictionary<string, List<int>>();
            for (int i = 0; i < m; i++)
            {
                var key = sourceSplit[i];
                if (!dictionary.TryGetValue(key, out var list))
                    dictionary.Add(key, list = new List<int>());
                list.Add(i);
            }

            int matchCount = 0, preMatchIndex = -1;
            foreach (var key in targetSplit)
            {
                if (!dictionary.TryGetValue(key, out var list)) continue;
                var leIndex = list.BinaryLE(preMatchIndex, IntComparer);
                if (leIndex == list.Count - 1) continue;
                matchCount++;
                preMatchIndex = list[leIndex + 1];
            }

            return (float) matchCount / m;
        }

        #endregion

        #region replace

        private void Replace()
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                var workspaceFolder = WorkspaceFolder;
                var rootFolder = $"{workspaceFolder}/Assets/NGUI";
                if (!Directory.Exists(rootFolder))
                {
                    ShowNotification(new GUIContent(NGUINotExist));
                    return;
                }

                var assetName2TargetPath = new Dictionary<string, FileInfo>();
                var rootInfo = new DirectoryInfo(rootFolder);
                var queue = new Queue<DirectoryInfo>();
                queue.Enqueue(rootInfo);
                while (queue.Count != 0)
                {
                    var info = queue.Dequeue();
                    if (Equals(info.Name, IgnoreFolder)) continue;
                    foreach (var fileInfo in info.GetFiles().Where(e =>
                        !IgnoreFileExtension.Equals(e.Extension, StringComparison.InvariantCultureIgnoreCase)))
                        assetName2TargetPath.Add(fileInfo.Name.ToLower(), fileInfo);
                    foreach (var next in info.GetDirectories()) queue.Enqueue(next);
                }

                foreach (var assetPath in AssetDatabase.GetAllAssetPaths())
                {
                    var assetName = Path.GetFileName(assetPath).ToLower();
                    if (!assetName2TargetPath.TryGetValue(assetName, out var info)) continue;
                    info.CopyTo(assetPath, true);
                    assetName2TargetPath.Remove(assetName);
                }

                var workspacePrefixLenght = Path.GetFullPath(workspaceFolder).Length;
                var projectRoot = Path.GetFullPath("./");
                foreach (var info in assetName2TargetPath.Values)
                {
                    var fileName =
                        $"{projectRoot}{info.FullName.Substring(workspacePrefixLenght).Replace("Assets/NGUI", "Assets/Plugins/NGUI")}";
                    Path.GetDirectoryName(fileName).Mkdir();
                    info.CopyTo(fileName);
                }

                var nguiEditor = $"{workspaceFolder}/Assets/NGUI/Editor";
                var nguiScriptsEditor = $"{workspaceFolder}/Assets/NGUI/Scripts/Editor";
                var projectNGUIEditor = $"{projectRoot}Assets/NGUI/Editor";
                var projectNGUIScriptsEditor = $"{projectRoot}Assets/NGUI/Scripts/Editor";
                projectNGUIEditor.Rmdir();
                projectNGUIScriptsEditor.Rmdir();
                if (Directory.Exists(nguiEditor)) nguiEditor.CopyTo(projectNGUIEditor);
                if (Directory.Exists(nguiScriptsEditor)) nguiScriptsEditor.CopyTo(projectNGUIScriptsEditor);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        #endregion

#endif
    }
}