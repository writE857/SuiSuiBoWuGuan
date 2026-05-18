using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Object = UnityEngine.Object;

namespace Fix.Editor
{
    public class ConvertBlood : FixEditorWindow
    {
        private const string TipName = "拖入需要图片";
        private const string Title = "转换红血";


        [FixEditor(FixRoot + nameof(Texture) + "/" + Title)]
        private static void ShowWindow()
        {
            GetWindowWithRect<ConvertBlood>(new Rect(Screen.width / 2, Screen.height / 2, 512, 720)).titleContent =
                new GUIContent(Title);
        }

        private static readonly int CutoffProp = Shader.PropertyToID("_Cutoff");
        private static readonly int RedThresholdProp = Shader.PropertyToID("_RedThreshold");
        private static readonly int ColorProp = Shader.PropertyToID("_Color");

        private readonly List<string> filePaths = new List<string>();
        private Material r2gMat;
        private float _Cutoff;
        private float _RedThreshold;
        private Color _Color;
        private void Awake() => filePaths.Clear();

        private void OnEnable()
        {
            r2gMat = new Material(Shader.Find("Fix/Utils/R2G"));
            _Cutoff = r2gMat.GetFloat(CutoffProp);
            _RedThreshold = r2gMat.GetFloat(RedThresholdProp);
            _Color = r2gMat.GetColor(ColorProp);
            Release(primaryTex);
            Release(previewTex);
            primaryTex = RenderTexture.GetTemporary(256, 256);
            previewTex = RenderTexture.GetTemporary(256, 256);
            EditorApplication.update += Renderer;
        }

        private void OnDisable()
        {
            Release(r2gMat);
            Release(primaryTex);
            Release(previewTex);
            EditorApplication.update -= Renderer;
        }

        private void OnDestroy() => filePaths.Clear();

        private Vector2 pos;


        private void OnGUI()
        {
            //! 实现拖拽
            DragRegion(path =>
            {
                var paths = GetTotalFiles(path)
                    .Where(e => !filePaths.Contains(e))
                    .Distinct()
                    .Where(e => AssetDatabase.GetMainAssetTypeAtPath(e) == typeof(Texture2D));
                filePaths.AddRange(paths);
            }, TipName, GUILayout.MinHeight(64), GUILayout.MinWidth(512));

            GUILayout.Space(10);
            HorizontalRegion(() => _RedThreshold = EditorGUILayout.FloatField("_RedThreshold", _RedThreshold));
            HorizontalRegion(() => _Color = EditorGUILayout.ColorField("_Color", _Color));
            HorizontalRegion(() => _Cutoff = EditorGUILayout.Slider("_Cutoff", _Cutoff, 0f, 1f));

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
                                () =>
                                {
                                    if (GUILayout.Button(Path.GetFileNameWithoutExtension(path), itemLabel))
                                        DoPreview(path);
                                });
                            if (GUILayout.Button("X", GUILayout.Width(20))) filePaths.Remove(path);
                        });
                    }

                    GUILayout.EndScrollView();
                });
            });

            HorizontalRegion(rect =>
            {
                GUILayout.Box("", GUILayout.Width(512), GUILayout.Height(256));
                if (IsValidPreview())
                {
                    EditorGUI.DrawTextureTransparent(new Rect(rect.position, new Vector2(256, 256)), primaryTex);
                    EditorGUI.DrawTextureTransparent(
                        new Rect(rect.position + new Vector2(256, 0), new Vector2(256, 256)), previewTex);
                }
            });
            GUILayout.Space(20);
            HorizontalRegion(() =>
            {
                if (GUILayout.Button("清除", GUILayout.Width(256), GUILayout.Height(40))) DoClear();
                if (GUILayout.Button("执行", GUILayout.Width(256), GUILayout.Height(40))) DoConvertBlood();
            });
        }


        private string previewAssetPath;
        private RenderTexture primaryTex, previewTex;

        private void DoPreview(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            if (previewAssetPath != path)
            {
                primaryTex.Clear();
                Graphics.Blit(AssetDatabase.LoadAssetAtPath<Texture2D>(path), primaryTex);
            }

            previewAssetPath = path;
            previewTex.Clear();
        }

        private bool IsValidPreview()
        {
            return primaryTex != null && previewTex != null && !string.IsNullOrWhiteSpace(previewAssetPath);
        }

        private void Renderer()
        {
            if (!IsValidPreview()) return;
            r2gMat.SetFloat(CutoffProp, _Cutoff);
            r2gMat.SetFloat(RedThresholdProp, _RedThreshold);
            r2gMat.SetColor(ColorProp, _Color);
            previewTex.Clear();
            Graphics.Blit(primaryTex, previewTex, r2gMat);
        }

        private void SetInvalid()
        {
            previewAssetPath = null;
        }

        private Texture2D Convert(Texture2D source)
        {
            var result = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            r2gMat.SetFloat(CutoffProp, _Cutoff);
            r2gMat.SetFloat(RedThresholdProp, _RedThreshold);
            r2gMat.SetColor(ColorProp, _Color);
            var temporary = RenderTexture.GetTemporary(source.width, source.height);
            var active = RenderTexture.active;
            try
            {
                RenderTexture.active = temporary;
                GL.Clear(true, true, Color.clear);
                Graphics.Blit(source, temporary, r2gMat);
                result.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                result.Apply();
                return result;
            }
            finally
            {
                RenderTexture.active = active;
                RenderTexture.ReleaseTemporary(temporary);
            }
        }

        private void DoClear()
        {
            filePaths.Clear();
            SetInvalid();
        }

        private void DoConvertBlood()
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var path in filePaths)
                {
                    var result = Convert(AssetDatabase.LoadAssetAtPath<Texture2D>(path));
                    switch (Path.GetExtension(path))
                    {
                        case ".jpg":
                            File.WriteAllBytes(path, result.EncodeToJPG());
                            break;
                        default:
                            File.WriteAllBytes(path, result.EncodeToPNG());
                            break;
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                SetInvalid();
            }
        }

        private static void Release(Object obj)
        {
            if (obj == null) return;
            switch (obj)
            {
                case RenderTexture rt:
                    RenderTexture.ReleaseTemporary(rt);
                    // DestroyImmediate(rt);
                    break;
                default:
                    DestroyImmediate(obj);
                    break;
            }
        }
    }
}