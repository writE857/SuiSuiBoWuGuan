using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Fix.Editor
{
    public static class FixEditorUtils
    {
        internal static string ReformatKey(this string path)
        {
            // 快速哈希计算
            uint hash = 2166136261;
            for (int i = 0; i < path.Length; i++)
            {
                hash = (hash ^ path[i]) * 16777619;
            }

            // 组合长度信息
            ulong combined = ((ulong) hash << 32) | (uint) path.Length;

            // 简单混合
            combined = (combined ^ (combined >> 23)) * 0x2127599bf4325c37UL;
            combined ^= combined >> 47;

            // 8位Base62输出（更快）
            const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            char[] result = new char[8];

            for (int i = 7; i >= 0; i--)
            {
                result[i] = chars[(int) (combined % 62)];
                combined /= 62;
            }

            return new string(result);
        }

        public static string StepTransform(
            Transform transform,
            Func<Transform, string> infoGetter,
            TabConfig config = null)
        {
            var list = new List<string>();
            do
            {
                list.Add(infoGetter.Invoke(transform));
                transform = transform.parent;
            } while (transform != null);

            list.Reverse();
            if (config == null) config = TabConfig.Default;
            if (config.tab)
            {
                var builder = new StringBuilder();
                var prefix = new StringBuilder();
                foreach (var s in list)
                {
                    builder.Append(prefix).Append(s).Append("\n");
                    prefix.Append(config.space);
                }

                return builder.ToString();
            }
            else
            {
                return string.Join(config.space, list);
            }
        }

        public class TabConfig
        {
            public static readonly TabConfig Default = new TabConfig();

            public static readonly TabConfig NoTab = new TabConfig()
            {
                tab = false
            };

            public bool tab = true;
            public string space = " ";
        }

        public static Texture2D Duplicate(Texture2D source)
        {
            RenderTexture tempRT = RenderTexture.GetTemporary(
                source.width,
                source.height,
                0,
                RenderTextureFormat.ARGB32);

            Graphics.Blit(source, tempRT);

            RenderTexture previousRT = RenderTexture.active;
            RenderTexture.active = tempRT;
            try
            {
                Texture2D readableTexture = new Texture2D(
                    source.width,
                    source.height,
                    TextureFormat.RGBA32,
                    false);

                readableTexture.ReadPixels(new Rect(0, 0, tempRT.width, tempRT.height), 0, 0);
                readableTexture.Apply();
                return readableTexture;
            }
            finally
            {
                RenderTexture.active = previousRT;
                RenderTexture.ReleaseTemporary(tempRT);
            }
        }

        public static string GetAssetPathWithoutExtension(string source)
        {
            if (string.IsNullOrWhiteSpace(source)) return source;
            var slash = source.LastIndexOf('/');
            var dot = source.LastIndexOf('.');
            if (slash >= dot || dot == -1) return source;
            return source.Substring(0, dot);
        }
    }
}