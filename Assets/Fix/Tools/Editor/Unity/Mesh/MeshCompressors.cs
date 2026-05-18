using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Fix.Editor
{
    public class MeshCompressors : FixEditorWindow
    {
        private const string TipName = "拖入Mesh";
        private const string Title = "Mesh裁剪";


        [MenuItem(FixRoot + Title, priority = 103)]
        [FixEditor(FixRoot + nameof(Mesh) + "/" + Title)]
        private static void ShowWindow()
        {
            GetWindowWithRect<MeshCompressors>(new Rect(Screen.width / 2, Screen.height / 2, 500, 500)).titleContent =
                new GUIContent(Title);
        }

        private static string OutputRoot => "Assets/MC";
        private static string OutputInfo => $"{OutputRoot}/Info.json";
        private float quality = 1;
        private IList<IFixMeshCompressor> compressors;
        private ISet<Mesh> meshes = new HashSet<Mesh>();
        private IFixMeshCompressor currentCompressor;
        private Vector2 pos;
        private bool keepSourceAsset;

        private void OnEnable()
        {
            Init();
        }

        private void OnGUI()
        {
            LayoutMeshCompressor();
        }

        private void LayoutMeshCompressor()
        {
            HorizontalRegion(() =>
            {
                if (!GUILayout.Button(new GUIContent("扫描全部Mesh", "默认不扫描导出文件夹"))) return;
                meshes.Clear();
                var paths = AssetDatabase.FindAssets("t:Mesh", new string[] {"Assets"})
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .IgnoreAssetsOf(OutputRoot).ToArray();
                int c = 0, total = paths.Length;
                try
                {
                    foreach (var path in paths)
                    {
                        EditorUtility.DisplayProgressBar("加载Mesh", path, (float) c++ / total);
                        meshes.Add(AssetDatabase.LoadAssetAtPath<Mesh>(path));
                    }
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }
            });
            //! 实现拖拽
            DragRegion(path =>
            {
                var assetPaths = GetTotalFiles(path)
                    .Distinct().ToArray();
                int c = 0, total = assetPaths.Length;
                try
                {
                    foreach (var assetPath in assetPaths)
                    {
                        EditorUtility.DisplayProgressBar("加载Mesh", assetPath, (float) c++ / total);
                        meshes.AddRange(AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Mesh>());
                    }
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }
            }, TipName, GUILayout.MinHeight(64), GUILayout.MinWidth(512));
            Module(() =>
            {
                GUILayout.FlexibleSpace();
                Button(Clear, "清除", GUILayout.Width(80));
            }, HorizontalR(), ColorR(Color.red));
            Space();
            GUILayout.Label($"Mesh数 : {meshes.Count}");
            Space();
            HorizontalRegion(() =>
            {
                VerticalRegion(() =>
                {
                    pos = EditorGUILayout.BeginScrollView(pos, GUI.skin.box, GUILayout.Height(60));
                    foreach (var compressor in compressors)
                    {
                        HorizontalRegion(() =>
                        {
                            GUILayout.Label(new GUIContent(compressor.Name, compressor.Desc),
                                GUILayout.Width(EditorGUIUtility.labelWidth));
                            if (GUILayout.Toggle(compressor == currentCompressor, "")
                                && compressor != currentCompressor)
                            {
                                currentCompressor = compressor;
                                quality = currentCompressor.DefaultQuality;
                            }
                        });
                    }

                    EditorGUILayout.EndScrollView();
                });
            });
            Space(20);
            Module(() =>
            {
                GUILayout.Label(
                    currentCompressor != null
                        ? $"当前压缩器 {currentCompressor.Name}"
                        : "找不到压缩器");
                Space();
                GUILayout.Label($"详细的参数设定改代码");
            }, ColorR(Color.cyan), HorizontalR());
            Space(20);
            HorizontalRegion(() => { quality = EditorGUILayout.Slider("质量", quality, 0, 1); });
            Space(20);
            HorizontalRegion(() =>
            {
                GUILayout.Label(new GUIContent("保持源文件"),
                    GUILayout.Width(EditorGUIUtility.labelWidth));
                keepSourceAsset = GUILayout.Toggle(keepSourceAsset, "");
            });
            ColorRegion(Color.magenta, () =>
            {
                HorizontalRegion(() =>
                {
                    if (keepSourceAsset)
                    {
                        GUILayout.Label(new GUIContent("使用引用替换，耗时间"));
                    }
                    else
                    {
                        GUILayout.Label(new GUIContent("以文件替换的形式，不能替换的使用引用替换"));
                    }
                });
            });

            Space(40);
            HorizontalRegion(() =>
            {
                Space(40);
                if (GUILayout.Button("裁剪", GUILayout.Height(40)))
                {
                    CompressAllMesh();
                    Clear();
                }

                Space(40);
                if (GUILayout.Button("删除", GUILayout.Height(40)))
                {
                    Delete();
                }

                Space(40);
            });
        }

        private void LayoutMeshRevert()
        {
        }

        private void Clear()
        {
            meshes.Clear();
            EditorUtility.UnloadUnusedAssetsImmediate();
        }

        #region compress

        private void CompressAllMesh()
        {
            OutputRoot.Rmdir();
            OutputRoot.Mkdir();
            AssetDatabase.Refresh();
            var replaceInfo = new MeshReplaceInfo();
            AssetDatabase.StartAssetEditing();
            try
            {
                int c = 0, total = meshes.Count;
                foreach (var mesh in meshes)
                {
                    EditorUtility.DisplayProgressBar(
                        "开始处理Mesh",
                        $"{c + 1}/{total}",
                        (float) c / total);
                    c++;
                    if (keepSourceAsset) ProcessKeep(mesh, replaceInfo);
                    else ProcessReplace(mesh, replaceInfo);
                }

                File.WriteAllText(OutputInfo, JsonConvert.SerializeObject(replaceInfo));
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            FixEditorExtension.BatchReplaceRef(replaceInfo.infos.Select(e =>
                new KeyValuePair<string, string>(e.sourceId, e.outputId)));
        }

        private static readonly int AssetsPrefixLength = "Assets".Length;

        private void ProcessKeep(Mesh mesh, MeshReplaceInfo replaceInfo)
        {
            try
            {
                string assetPath = AssetDatabase.GetAssetPath(mesh),
                    extension = Path.GetExtension(assetPath);
                string newAssetPath =
                    $"{OutputRoot}{assetPath.Substring(0, assetPath.Length - extension.Length).Substring(AssetsPrefixLength)}.asset";
                if (File.Exists(newAssetPath))
                    newAssetPath = AssetDatabase.GenerateUniqueAssetPath(newAssetPath);
                var directoryName = Path.GetDirectoryName(newAssetPath);
                if (!Directory.Exists(directoryName))
                {
                    directoryName.Mkdir();
                    AssetDatabase.Refresh();
                }

                currentCompressor.Init(mesh);
                var compressedMesh = currentCompressor.Compress(quality);

                AssetDatabase.CreateAsset(compressedMesh, newAssetPath);
                if (mesh.TryGetRefString(out var @oldRef)
                    && AssetDatabase.LoadAssetAtPath<Mesh>(newAssetPath).TryGetRefString(out var @newRef)
                ) replaceInfo.infos.Add(new MeshReplaceInfo.Entry(@oldRef, @newRef));
                replaceInfo.sourcePaths.Add(assetPath);
            }
            finally
            {
                currentCompressor.Release();
            }
        }

        private void ProcessReplace(Mesh mesh, MeshReplaceInfo replaceInfo)
        {
            try
            {
                string assetPath = AssetDatabase.GetAssetPath(mesh),
                    extension = Path.GetExtension(assetPath);

                string newAssetPath = assetPath;
                bool isSubAsset;
                if (isSubAsset = AssetDatabase.IsSubAsset(mesh))
                {
                    newAssetPath = $"{assetPath.Substring(0, assetPath.Length - extension.Length)}.asset";
                    if (File.Exists(newAssetPath))
                        newAssetPath = AssetDatabase.GenerateUniqueAssetPath(newAssetPath);
                }

                var directoryName = Path.GetDirectoryName(newAssetPath);
                if (!Directory.Exists(directoryName))
                {
                    directoryName.Mkdir();
                    AssetDatabase.Refresh();
                }

                currentCompressor.Init(mesh);
                var compressedMesh = currentCompressor.Compress(quality);

                AssetDatabase.CreateAsset(compressedMesh, newAssetPath);
                if (isSubAsset
                    && mesh.TryGetRefString(out var @oldRef)
                    && AssetDatabase.LoadAssetAtPath<Mesh>(newAssetPath).TryGetRefString(out var @newRef)
                ) replaceInfo.infos.Add(new MeshReplaceInfo.Entry(@oldRef, @newRef));
            }
            finally
            {
                currentCompressor.Release();
            }
        }

        #endregion

        #region delete

        private void Delete()
        {
            if (!File.Exists(OutputInfo)) return;
            var info = JsonConvert.DeserializeObject<MeshReplaceInfo>(File.ReadAllText(OutputInfo));
            if (info == null)
            {
                ShowNotification(new GUIContent("文件内容损坏"));
                return;
            }

            var failure = new List<string>();
            var subAssets = new List<string>();
            FixEditorExtension.DeleteAssets(
                info
                    .sourcePaths
                    .Where(e =>
                    {
                        var b = AssetDatabase.GetMainAssetTypeAtPath(e) == typeof(Mesh);
                        if (!b) subAssets.Add(e);
                        return b;
                    })
                    .ToArray(), failure);
            if (failure.Count + subAssets.Count > 0)
            {
                ShowNotification(new GUIContent("有错误信息,查看控制台"));
                if (failure.Count > 0)
                    Debug.LogError($"以下网格删除失败:\n{string.Join("\n", failure)}");
                if (subAssets.Count > 0)
                    Debug.LogError($"以下网格为子资源:\n{string.Join("\n", subAssets)}");
            }
        }

        #endregion

        [NonSerialized] private bool init;

        private void Init()
        {
            if (init) return;
            meshes.Clear();
            compressors = FixEditorExtension.GetSubClassInstanceOf<IFixMeshCompressor>();
            currentCompressor = compressors.FirstOrDefault();
            quality = currentCompressor?.DefaultQuality ?? 1;
            init = true;
        }

        private class MeshReplaceInfo
        {
            public List<Entry> infos = new List<Entry>();
            public List<string> sourcePaths = new List<string>();

            public class Entry
            {
                public string sourceId;
                public string outputId;

                public Entry()
                {
                }

                public Entry(string sourceId, string outputId)
                {
                    this.sourceId = sourceId;
                    this.outputId = outputId;
                }
            }
        }
    }

    public interface IFixMeshCompressor : IDisposable
    {
        string Name { get; }
        string Desc { get; }
        float DefaultQuality { get; }
        void Init(Mesh mesh);
        void Release();
        Mesh Compress(float quality);
        Mesh Preview(float quality);
    }
}