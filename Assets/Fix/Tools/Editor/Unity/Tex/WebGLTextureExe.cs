using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Fix.Editor
{
    public class WebGLTextureExe : FixEditorWindow
    {
        private const string Title = "WebGL图片处理";
        private const string TipName = "需要标准化的文件 For WebGL";
        private const string Platform = "WebGL"; //发布的平台 

        private static readonly Color FilesColor;

        private readonly List<string> imagePaths = new List<string>();
        private int compressionQuality = 32;
        private bool setBaseSetting = true;
        private bool setFourSizeTexture = true;
        private bool halfMaxSize = false;
        private Material material;
        private static readonly int ScaleOffset = Shader.PropertyToID("_Scale_Offset");
        private const string AlphaTexture = "ALPHA_TEXTURE";


        [MenuItem(FixRoot + Title, priority = 101)]
        [FixEditor(FixRoot + nameof(Texture) + "/" + Title)]
        private static void ShowWindow()
        {
            GetWindowWithRect<WebGLTextureExe>(new Rect(Screen.width / 2, Screen.height / 2, 500, 500)).titleContent =
                new GUIContent(Title);
        }

        private void OnEnable()
        {
            imagePaths.Clear();
            material = new Material(Shader.Find("Fix/Utils/FormatTexture"));
        }


        private void OnDisable()
        {
            imagePaths.Clear();
            DestroyImmediate(material);
        }

        private void AddPath(string path)
        {
            if (!imagePaths.Contains(path)) imagePaths.Add(path);
        }

        private void OnGUI()
        {
            HorizontalRegion(() =>
            {
                if (!GUILayout.Button("扫描全部图片")) return;
                var ts = AssetDatabase.FindAssets("t:Texture", new string[] {"Assets"});
                int i = 0, total = ts.Length;
                try
                {
                    foreach (var t in ts)
                    {
                        EditorUtility.DisplayProgressBar("加载中", $"第{i}/{total}个", (float) i / total);
                        AddPath(AssetDatabase.GUIDToAssetPath(t));
                        i++;
                    }
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }
            });
            DragRegion(paths =>
            {
                foreach (var path in GetTotalFiles(paths)) AddPath(path);
            }, TipName, GUILayout.MinHeight(64), GUILayout.MinWidth(512));
            Module(() =>
            {
                GUILayout.FlexibleSpace();
                Button(Clear, "清除", GUILayout.Width(80));
            }, HorizontalR(), ColorR(Color.red));
            GUILayout.Space(10);

            HorizontalRegion(() =>
            {
                ColorRegion(new Color(0, 1, 072f, 0.5f), () => { GUILayout.Label($"选择了 {imagePaths.Count} 个文件"); });
            });


            GUILayout.Space(20);
            HorizontalRegion(() => { setBaseSetting = EditorGUILayout.Toggle($"修改{Platform}设定", setBaseSetting); });
            HorizontalRegion(() => { setFourSizeTexture = EditorGUILayout.Toggle("四倍化图片", setFourSizeTexture); });

            HorizontalRegion(() => { halfMaxSize = EditorGUILayout.Toggle("减半最大大小", halfMaxSize); });
            HorizontalRegion(() => { compressionQuality = EditorGUILayout.IntSlider(compressionQuality, 0, 100); });

            GUILayout.Space(20);
            HorizontalRegion(() =>
            {
                FlexibleSpace();
                if (GUILayout.Button("开始处理图像", GUILayout.Width(256), GUILayout.Height(40)))
                    SetTextures();
                FlexibleSpace();
            });
        }

        private void Clear()
        {
            imagePaths.Clear();
        }

        private void SetTextures()
        {
            int sc = 0, fc = 0, imgc = 0;
            AssetDatabase.StartAssetEditing();
            var count = imagePaths.Count;
            try
            {
                for (int i = 0; i < count; i++)
                {
                    EditorUtility.DisplayProgressBar("开始处理", $"{i + 1}/{count}", (float) i / count);

                    string path = imagePaths[i];
                    if (!(AssetDatabase.LoadMainAssetAtPath(path) is Texture)) continue;
                    imgc++;
                    try
                    {
                        SetTexture(path); //!处理贴图
                        sc++;
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        fc++;
                    }
                }
            }
            finally
            {
                Clear();
                EditorUtility.ClearProgressBar();
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                ShowNotification(
                    new GUIContent($"总共 :{count}\n" +
                                   $"图像 :{imgc}\n" +
                                   $"成功 :{sc}\n" +
                                   $"失败 :{fc}\n"));
            }
        }

        private void SetTexture(string path)
        {
            var texture = (Texture) AssetDatabase.LoadMainAssetAtPath(path);
            var importer = (TextureImporter) AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
            var settings = importer.GetPlatformTextureSettings(Platform);

            if (setBaseSetting) SetBaseSetting(texture, importer, settings);
            if (halfMaxSize) HalfMaxSize(texture, importer, settings);
            if (setFourSizeTexture) SetFourSizeTexture(texture, importer, settings);

            importer.SetPlatformTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private void SetBaseSetting(Texture texture, TextureImporter importer, TextureImporterPlatformSettings settings)
        {
            var hasAlpha = importer.DoesSourceTextureHaveAlpha();

            importer.mipmapEnabled = false;
            TextureImporterFormat targetFormat;
            switch (settings.format)
            {
                case TextureImporterFormat.RGB16:
                case TextureImporterFormat.RGB24:
                case TextureImporterFormat.Alpha8:
                case TextureImporterFormat.R16:
                case TextureImporterFormat.R8:
                case TextureImporterFormat.RG16:
                case TextureImporterFormat.ARGB16:
                case TextureImporterFormat.RGBA32:
                case TextureImporterFormat.ARGB32:
                case TextureImporterFormat.RGBA16:
                    targetFormat = settings.format;
                    break;
                default:
                    switch (hasAlpha || importer.textureType == TextureImporterType.NormalMap)
                    {
                        case true:
                            targetFormat = TextureImporterFormat.DXT5Crunched;
                            break;
                        case false:
                            targetFormat = TextureImporterFormat.DXT1Crunched;
                            break;
                        default: return;
                    }

                    break;
            }

            settings.overridden = true;
            settings.name = Platform;
            settings.textureCompression = TextureImporterCompression.Compressed;
            settings.format = targetFormat;
            settings.compressionQuality = compressionQuality;
            settings.maxTextureSize =
                Mathf.Clamp(Math.Min(GetMaxSize(texture.width, texture.height), settings.maxTextureSize), 24, 8192);
        }

//168x172 -> 125x128
        private void SetFourSizeTexture(Texture texture, TextureImporter importer,
            TextureImporterPlatformSettings settings)
        {
            if (!(texture is Texture2D source)) return;
            var path = importer.assetPath;
            if (!File.Exists(path)) return;
            var bytes = File.ReadAllBytes(path);
            int width, height, sourceWidth, sourceHeight;
            if (!ImageHeaderReader.TryGetImageSize(bytes, out sourceWidth, out sourceHeight))
            {
                width = sourceWidth = source.width;
                height = sourceHeight = source.height;
            }
            else
            {
                width = sourceWidth;
                height = sourceHeight;
            }

            int maxTexSize = settings.maxTextureSize;
            float ppu = importer.spritePixelsPerUnit;
            UnityResize(width, height, maxTexSize, out var nw, out var nh);
            if (nw % 4 == 0 && nh % 4 == 0 && (width & 1 | height & 1) == 0) return;
            bool useEmptyFill = false;
            if (importer.textureType == TextureImporterType.Sprite)
            {
                switch (importer.spriteImportMode)
                {
                    case SpriteImportMode.Multiple:
                        ResizeForFill(
                            ref width, ref height,
                            maxTexSize,
                            sourceWidth, sourceHeight);

                        useEmptyFill = true;
                        break;
                    case SpriteImportMode.Single:
                    case SpriteImportMode.Polygon:
                        ResizeSpriteForAdjust(
                            ref width, ref height,
                            ref ppu,
                            maxTexSize,
                            sourceWidth, sourceHeight);
                        break;
                    default:
                        break;
                }
            }
            else
            {
                ResizeForFill(
                    ref width, ref height,
                    maxTexSize,
                    sourceWidth, sourceHeight);
            }

            importer.spritePixelsPerUnit = ppu;
            File.WriteAllBytes(path,
                FormatTexture(bytes,
                    sourceWidth, sourceHeight,
                    width, height,
                    useEmptyFill, importer.DoesSourceTextureHaveAlpha()));
        }
        
        private static void UnityResize(
            int width, int height,
            int maxTexSize,
            out int targetW, out int targetH)
        {
            if (width <= maxTexSize && height <= maxTexSize)
            {
                targetW = width;
                targetH = height;
                return;
            }

            if (width > height)
            {
                targetH = Clip((double) maxTexSize * height / width);
                targetW = maxTexSize;
            }
            else
            {
                targetW = Clip((double) maxTexSize * width / height);
                targetH = maxTexSize;
            }
        }

        private static int Clip(double f)
        {
            
            return Mathf.RoundToInt((float)f);
            // return (float) Math.Round(f, 4);
        }

        private static void ResizeSpriteForAdjust(
            ref int width, ref int height,
            ref float ppu,
            int maxTexSize,
            int sourceWidth, int sourceHeight)
        {
            int nw, nh;
            UnityResize(sourceWidth, sourceHeight, maxTexSize, out nw, out nh);

            var v2 = GetFourSize(nw, nh);
            if (sourceWidth > sourceHeight)
            {
                height = (int) Math.Floor((double) sourceWidth * v2.y / v2.x);
                width = sourceWidth;
            }
            else
            {
                height = sourceHeight;
                width = (int) Math.Floor((double) sourceHeight * v2.x / v2.y);
            }

            Grow(ref width, ref height, maxTexSize);

            ppu *= ((float) width / sourceWidth + (float) height / sourceHeight) * 0.5f;
        }

        private static void ResizeForFill(
            ref int width, ref int height,
            int maxTexSize,
            int sourceWidth, int sourceHeight)
        {
            int nw, nh;
            UnityResize(sourceWidth, sourceHeight, maxTexSize, out nw, out nh);
            var v2 = GetFourSize(nw, nh);
            if (sourceWidth > sourceHeight)
            {
                height = (int) Math.Ceiling((double) sourceWidth * v2.y / v2.x);
                width = sourceWidth;
            }
            else
            {
                height = sourceHeight;
                width = (int) Math.Ceiling((double) sourceHeight * v2.x / v2.y);
            }

            Shrink(ref width, ref height, maxTexSize, sourceWidth, sourceHeight);
            Grow(ref width, ref height, maxTexSize);

            if (width == 0) width = 4;
            if (height == 0) height = 4;
        }

        private static void Shrink(
            ref int width, ref int height,
            int maxTexSize,
            int sourceWidth, int sourceHeight)
        {
            int nw, nh;
            while (true)
            {
                UnityResize(width, height, maxTexSize, out nw, out nh);
                if (nw % 4 == 0 && nh % 4 == 0 && (width & 1 | height & 1) == 0) break;

                if (nh % 4 < nw % 4)
                {
                    if (height <= sourceHeight) break;
                    height--;
                }
                else
                {
                    if (width <= sourceWidth) break;
                    width--;
                }
            }
        }

        private static void Grow(
            ref int width, ref int height,
            int maxTexSize)
        {
            int nw, nh;
            while (true)
            {
                UnityResize(width, height, maxTexSize, out nw, out nh);
                if (nw % 4 == 0 && nh % 4 == 0 && (width & 1 | height & 1) == 0) break;
                if (nw % 4 > nh % 4) width++;
                else height++;
            }
        }

        private static int PrevFourOf(int v) => v / 4 * 4;
        private static int NextFourOf(int v) => (v + 3) / 4 * 4;

        private void HalfMaxSize(Texture texture, TextureImporter importer, TextureImporterPlatformSettings settings)
        {
            settings.maxTextureSize =
                Mathf.Clamp(
                    Math.Min(GetMaxSize(settings.maxTextureSize / 2, settings.maxTextureSize / 2),
                        importer.maxTextureSize), MinSize, MaxSize);
        }

        #region utils

        private byte[] FormatTexture(byte[] bytes,
            int sourceWidth, int sourceHeight,
            int width, int height,
            bool emptyFill, bool hasAlpha)
        {
            var source = new Texture2D(2, 2);
            source.LoadImage(bytes, false);
            var v2 = new Vector2Int(width, height);
            var st = emptyFill
                ? new Vector4((float) v2.x / sourceWidth, (float) v2.y / sourceHeight, 0, 0)
                : new Vector4(1, 1, 0, 0);
            material.SetVector(ScaleOffset, st);
            if (hasAlpha) material.EnableKeyword(AlphaTexture);
            else material.DisableKeyword(AlphaTexture);
            var temp = new RenderTexture(v2.x, v2.y, 0);
            Graphics.Blit(source, temp, material, hasAlpha ? 1 : 0);
            var active = RenderTexture.active;
            RenderTexture.active = temp;
            var texCopy = new Texture2D(v2.x, v2.y);
            texCopy.ReadPixels(new Rect(0, 0, v2.x, v2.y), 0, 0);
            RenderTexture.active = active;
            temp.Release();
            DestroyImmediate(temp);
            texCopy.Apply();
            var result = texCopy.EncodeToPNG();
            DestroyImmediate(source);
            DestroyImmediate(texCopy);
            return result;
        }

        private static readonly int[] MaxSizes = new int[] {32, 64, 128, 256, 512, 1024, 2048, 4096, 8192};
        private static readonly int MinSize = 32, MaxSize = 8192;

        private static int GetMaxSize(int width, int height)
        {
            var size = Math.Max(width, height); //取【宽】【高】中的最大值
            foreach (var maxSize in MaxSizes)
            {
                if (size <= maxSize) return maxSize;
            }

            return MaxSize;
        }

        private static Vector2Int GetFourSize(int width, int height)
        {
            // width += width % 4 != 0 ? 4 - width % 4 : 0;
            while (width % 4 != 0) width++;

            while (height % 4 != 0) height++;

            return new Vector2Int(Mathf.Max(4, width), Mathf.Max(4, height));
        }

        #endregion
    }
}