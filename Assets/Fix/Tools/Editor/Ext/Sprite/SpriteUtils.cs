using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Fix.Editor
{
    public static class SpriteUtils
    {
        private static readonly int RotateMode = Shader.PropertyToID("_RotateMode");

        public static bool TryCreateTextureFromAtlasSprite(this Sprite sprite, out Texture2D tex)
        {
            tex = default;
            var source = sprite.texture;
            if (sprite == null || source == null || !sprite.packed) return false;
            var spriteRect = sprite.rect;
            int width = Mathf.FloorToInt(spriteRect.width);
            int height = Mathf.FloorToInt(spriteRect.height);
            tex = new Texture2D(width, height,
                source.alphaIsTransparency ? TextureFormat.RGBA32 : TextureFormat.RGB24, false)
            {
                name = sprite.name,
                alphaIsTransparency = source.alphaIsTransparency,
                filterMode = source.filterMode,
                anisoLevel = source.anisoLevel,
                wrapMode = source.wrapMode,
            };
            tex.Read(sprite);

            // 使用mesh数据裁剪掉不属于该Sprite的像素
            // tex.ApplySpriteMeshMask(sprite);

            return true;
        }

        /// <summary>
        /// 使用Sprite的mesh数据创建遮罩，裁剪掉不属于该Sprite的像素（GPU实现）
        /// 这可以防止图集中其他Sprite的像素被包含进来
        /// </summary>
        private static void ApplySpriteMeshMask(this Texture2D tex, Sprite sprite)
        {
            Vector2[] vertices = sprite.vertices;
            ushort[] triangles = sprite.triangles;
            Vector2[] uvs = sprite.uv;

            if (vertices == null || vertices.Length == 0 || triangles == null || triangles.Length == 0)
            {
                // 没有mesh数据，跳过裁剪
                return;
            }

            int width = tex.width;
            int height = tex.height;
            float ppu = sprite.pixelsPerUnit;
            Vector2 pivotPixels = sprite.pivot;

            using (RenderTexHandle.Get(RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32),
                out var rt))
            using (ObjectHandle<Material>.Get(new Material(Shader.Find("Hidden/BlitCopy")),
                out var mat))
            {
                var previous = RenderTexture.active;
                RenderTexture.active = rt;

                GL.Clear(true, true, Color.clear);

                mat.mainTexture = tex;

                GL.PushMatrix();
                GL.LoadPixelMatrix(0, width, 0, height);

                if (mat.SetPass(0))
                {
                    GL.Begin(GL.TRIANGLES);
                    foreach (int idx in triangles)
                    {
                        Vector2 v = vertices[idx];
                        Vector2 uv = new Vector2(
                            (v.x * ppu + pivotPixels.x) / width,
                            (v.y * ppu + pivotPixels.y) / height);
                        GL.TexCoord2(uv.x, uv.y);
                        GL.Vertex3(v.x * ppu + pivotPixels.x, v.y * ppu + pivotPixels.y, 0);
                    }

                    GL.End();
                }

                GL.PopMatrix();

                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();

                RenderTexture.active = previous;
            }
        }

        private static void Read(
            this Texture2D tex,
            Sprite sprite)
        {
            var source = sprite.texture;

            using (RenderTexHandle.Get(RenderTexture.GetTemporary(
                    source.width,
                    source.height,
                    0),
                out var temp))
            using (RenderTexHandle.Get(RenderTexture.GetTemporary(
                    tex.width,
                    tex.height,
                    0),
                out var temp2))
            using (ObjectHandle<Material>.Get(new Material(Shader.Find("Fix/Utils/ReadContent")),
                out var material))
            {
                Graphics.Blit(source, temp);
                tex.Read(temp, sprite.GetRTRect(), (0, 0));
                material.SetInt(RotateMode, sprite.packingRotation.ToRotateMode());
                Graphics.Blit(tex, temp2, material, 0);
                tex.Read(temp2, new Rect(0, 0, tex.width, tex.height), (0, 0));
            }
        }

        private static void Read(this Texture2D tex, RenderTexture rt, Rect rect, (int destX, int destY) offset)
        {
            var active = RenderTexture.active;
            RenderTexture.active = rt;
            try
            {
                tex.ReadPixels(rect, offset.destX, offset.destY);
                tex.Apply();
            }
            finally
            {
                RenderTexture.active = active;
            }
        }

        private static int ToRotateMode(this SpritePackingRotation rotation)
        {
            switch (rotation)
            {
                case SpritePackingRotation.None:
                    return 0;
                case SpritePackingRotation.FlipHorizontal:
                    return 1;
                case SpritePackingRotation.FlipVertical:
                    return 2;
                case SpritePackingRotation.Rotate180:
                    return 3;
                case SpritePackingRotation.Any:
                    return 4;
                default:
                    return 0;
            }
        }

        //todo 如果不对的话改这里，测试版本 2019.4.18和2021.3.40 分别是两种写法
        private static Rect GetRTRect(this Sprite sprite)
        {
            var spriteRect = sprite.rect;
#if UNITY_2021_1_OR_NEWER
            return new Rect(spriteRect.x, spriteRect.y, spriteRect.width,
                spriteRect.height);
#else
            return new Rect(spriteRect.x, sprite.texture.height - spriteRect.y - spriteRect.height, spriteRect.width,
                spriteRect.height);
#endif
        }
    }

    internal struct RenderTexHandle : IDisposable
    {
        public RenderTexture RT { get; }

        public RenderTexHandle(RenderTexture t)
        {
            RT = t;
        }

        public void Dispose()
        {
            if (RT != null)
            {
                if (RT.IsCreated())
                {
                    RT.Release();
                }
                else
                {
                    RenderTexture.ReleaseTemporary(RT);
                }
            }
        }

        public static RenderTexHandle Get(RenderTexture obj, out RenderTexture target)
        {
            var handle = new RenderTexHandle(obj);
            target = handle.RT;
            return handle;
        }
    }

    internal struct ObjectHandle<T> : IDisposable
        where T : Object
    {
        public T Object { get; }

        public ObjectHandle(T t)
        {
            Object = t;
        }


        public void Dispose()
        {
            if (Object != null)
                UnityEngine.Object.DestroyImmediate(Object);
        }

        public static ObjectHandle<T> Get(in T obj, out T target)
        {
            var handle = new ObjectHandle<T>(obj);
            target = handle.Object;
            return handle;
        }
    }
}