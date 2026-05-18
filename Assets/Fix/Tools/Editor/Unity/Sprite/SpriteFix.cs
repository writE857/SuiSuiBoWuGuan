using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
#if USE_UNITY_2D_SPRITE
using UnityEditor.U2D.Sprites;
using UnityEngine.U2D;

#endif
namespace Fix.Editor
{
    public class SpriteFix : FixEditorBase
    {
        private static string Folder => nameof(SpriteFix).GetWorkspaceFolder();
        private static string FilePath => Folder + "/RemoveSprites.json";
        private const float MinMatch = 0.013f;
        private const string FixName = FixRoot + "Sprite/" + "修复精灵图";
        private const string RemoveName = FixRoot + "Sprite/" + "删除无用精灵图";
        private static readonly string[] Root = new string[] {"Assets"};
        private static bool LegacyMethod = false;

        [FixEditor(FixName)]
        public static void FixSprite()
        {
#if USE_UNITY_2D_SPRITE
            MergeSprite(Collect());
            FixTotalSprites(Collect());

#else
            Debug.LogError("没有Unity.2D.Sprite");
#endif
        }
#if USE_UNITY_2D_SPRITE
        [FixEditor(RemoveName, true)]
        private static bool IsFileExist() => File.Exists(FilePath);

        [FixEditor(RemoveName, false)]
        private static void RemoveNoUseSprites()
        {
            if (!IsFileExist()) throw new Exception("缓存文件不存在");
            var removedSprites = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(FilePath));
            if (removedSprites == null) throw new Exception("文件损坏");
            var msgList = new List<string>();
            msgList.Add("将要删除以下资源");
            for (int i = 0; i < Math.Min(5, removedSprites.Count); i++) msgList.Add(removedSprites[i]);
            if (removedSprites.Count > 5) msgList.Add("...");
            if (EditorUtility.DisplayDialog("删除无用Sprite", string.Join("\n", msgList), "确认", "取消"))
            {
                var array = removedSprites.ToArray();
                removedSprites.Clear();
                FixEditorExtension.DeleteAssets(array, removedSprites);
            }

            if (removedSprites.Count > 0)
                throw new Exception($"以下资源删除失败:\n{string.Join("\n", removedSprites)}");
        }

        private static Dictionary<Texture2D, List<Sprite>> Collect()
        {
            var dictionary = new Dictionary<Texture2D, List<Sprite>>();
            foreach (var sprite in AssetDatabase
                .FindAssets("t:Sprite", Root)
                .Select(AssetDatabase.GUIDToAssetPath)
                .Distinct()
                .SelectMany(AssetDatabase.LoadAllAssetsAtPath)
                .OfType<Sprite>())
            {
                if (sprite.texture == null) continue;
                dictionary.AddItem(sprite.texture, sprite);
            }

            foreach (var pair in dictionary.ToArray())
            {
                var texPath = AssetDatabase.GetAssetPath(pair.Key);
                if (pair.Value.All(e => AssetDatabase.GetAssetPath(e) == texPath))
                    dictionary.Remove(pair.Key);
            }

            return dictionary;
        }

        private static void MergeSprite(IDictionary<Texture2D, List<Sprite>> source)
        {
            try
            {
                var repList = new List<KeyValuePair<string, string>>();
                var removedSprites = new List<string>();
                foreach (var pair in source)
                {
                    var tex = pair.Key;
                    var texPath = AssetDatabase.GetAssetPath(tex);
                    //散的精灵图
                    var sprites = pair
                        .Value
                        .Where(e => AssetDatabase.GetAssetPath(e) != texPath)
                        .ToList();
                    var dictionary = new Dictionary<long, Sprite>();

                    foreach (var @old in sprites)
                    {
                        var feature = GetSpriteFeature(@old);
                        if (!dictionary.TryGetValue(feature, out var @new))
                            dictionary.Add(feature, @old);
                        else
                        {
                            if (@old.TryGetRefString(out var oldStr)
                                && @new.TryGetRefString(out var newStr))
                            {
                                repList.Add(new KeyValuePair<string, string>(oldStr, newStr));
                                removedSprites.Add(AssetDatabase.GetAssetPath(@old));
                            }
                        }
                    }
                }

                FixEditorExtension.BatchReplaceRef(repList);
                var array = removedSprites.ToArray();
                removedSprites.Clear();
                FixEditorExtension.DeleteAssets(array, removedSprites);
                if (removedSprites.Count > 0)
                    throw new Exception($"以下资源删除失败:\n{string.Join("\n", removedSprites)}");
            }
            finally
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private static void FixTotalSprites(IDictionary<Texture2D, List<Sprite>> source)
        {
            SplitByPacked(source, out var sprites, out var packedSprites);
            CreateFeature(sprites, out var featureDictionary);
            FixSprite(sprites, featureDictionary, out var spriteOld2New);
            FixSpriteAtlas(packedSprites, out var packedSpriteOld2New);

            Replace(packedSpriteOld2New.Concat(spriteOld2New).ToDictionary(e => e.Key, e => e.Value));
        }

        private static void SplitByPacked(
            IDictionary<Texture2D, List<Sprite>> source,
            out IDictionary<Texture2D, List<Sprite>> sprites,
            out IDictionary<Texture2D, List<Sprite>> packedSprites)
        {
            sprites = new Dictionary<Texture2D, List<Sprite>>();
            packedSprites = new Dictionary<Texture2D, List<Sprite>>();
            foreach (var pair in source)
            {
                if (pair.Value.Any(e => e.packed)) packedSprites.Add(pair);
                else sprites.Add(pair);
            }
        }

        private static void CreateFeature(
            IDictionary<Texture2D, List<Sprite>> source,
            out IDictionary<Sprite, long> featureDictionary)
        {
            featureDictionary = source.Values.SelectMany(e => e).ToDictionary(e => e, GetSpriteFeature);
        }

        #region sprite

        private static void FixSprite(
            IDictionary<Texture2D, List<Sprite>> source,
            IDictionary<Sprite, long> featureDictionary,
            out IDictionary<Sprite, Sprite> spriteOld2New)
        {
            SetSprites(source);
            SyncSprites(source, out var updateTexturePath);
            Refresh(updateTexturePath, source, featureDictionary);
            spriteOld2New = new Dictionary<Sprite, Sprite>();
            if (LegacyMethod) LegacyReplace(source, spriteOld2New);
            else NewReplace(source, featureDictionary, spriteOld2New);
        }


        private static void SetSprites(IDictionary<Texture2D, List<Sprite>> source)
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var pair in source)
                {
                    var tex = pair.Key;
                    var texPath = AssetDatabase.GetAssetPath(tex);
                    var sprites = pair
                        .Value
                        .Where(e => AssetDatabase.GetAssetPath(e) != texPath)
                        .ToList();

                    var importer = (TextureImporter) AssetImporter.GetAtPath(texPath);
                    if (sprites.Count > 1
                        || sprites.Count == 1
                        && sprites[0].rect != new Rect(0, 0, tex.width, tex.height)
                        && sprites[0].name != tex.name)
                    {
                        if (importer.spriteImportMode != SpriteImportMode.Multiple)
                            importer.spriteImportMode = SpriteImportMode.Multiple;
                    }

                    switch (importer.spriteImportMode)
                    {
                        case SpriteImportMode.Single:
                        {
                            var sprite = sprites[0];
                            var texSize = new Vector2(tex.width, tex.height);
                            importer.spritePivot = (sprite.pivot + sprite.rect.position) / texSize;
                            break;
                        }

                        case SpriteImportMode.Multiple:
                        {
                            var datas = new SpriteMetaData[sprites.Count];
                            for (var i = 0; i < sprites.Count; i++)
                            {
                                datas[i] = new SpriteMetaData()
                                {
                                    name = sprites[i].name,
                                    border = sprites[i].border,
                                    rect = sprites[i].rect,
                                    pivot = sprites[i].pivot,
                                };
                            }

                            importer.spriteImportMode = SpriteImportMode.Single;
                            importer.spriteImportMode = SpriteImportMode.Multiple;
                            importer.spritesheet = datas;
                            break;
                        }

                        case SpriteImportMode.None:
                        case SpriteImportMode.Polygon:
                        default:
                            Debug.LogError($"Not Support : {texPath}");
                            break;
                    }

                    importer.SaveAndReimport();
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private static void SyncSprites(IDictionary<Texture2D, List<Sprite>> source, out List<string> updateTexturePath)
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                updateTexturePath = new List<string>();
                foreach (var pair in source)
                {
                    var tex = pair.Key;
                    var sprites = new List<Sprite>(pair.Value);
                    var texPath = AssetDatabase.GetAssetPath(tex);
                    var importer = (TextureImporter) AssetImporter.GetAtPath(texPath);
                    if (importer.spriteImportMode != SpriteImportMode.Multiple) continue;

                    using (var so = new SerializedObject(importer))
                    {
                        var spritesProp = so.FindProperty("m_SpriteSheet.m_Sprites");
                        for (int i = 0; i < spritesProp.arraySize; i++)
                        {
                            var nsp = spritesProp.GetArrayElementAtIndex(i);

                            var name = nsp.FindPropertyRelative("m_Name").stringValue;
                            var oldIndex = sprites.FindIndex(e => e != null && e.name == name);
                            var old = sprites[oldIndex];
                            new SpriteDataExt().From(old).Apply(nsp);
                            sprites.RemoveAt(oldIndex);
                        }

                        updateTexturePath.Add(importer.assetPath);
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }

                    importer.SaveAndReimport();
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private static void Refresh(
            List<string> updateTexturePath,
            IDictionary<Texture2D, List<Sprite>> source,
            IDictionary<Sprite, long> featureDictionary)
        {
            foreach (var pair in updateTexturePath
                .Select(e =>
                    new KeyValuePair<Texture2D, List<Sprite>>(
                        AssetDatabase.LoadAssetAtPath<Texture2D>(e),
                        AssetDatabase.LoadAllAssetRepresentationsAtPath(e)
                            .OfType<Sprite>()
                            .ToList())))
            {
                source[pair.Key].AddRange(pair.Value);
                foreach (var sprite in pair.Value) featureDictionary.Add(sprite, GetSpriteFeature(sprite));
            }
        }

        #endregion

        #region spriteAtlas

        private static void FixSpriteAtlas(
            IDictionary<Texture2D, List<Sprite>> source,
            out IDictionary<Sprite, Sprite> spriteOld2New)
        {
            SetSpriteAtlasSprites(source, out var entries);
            SyncSpriteAtlasSprites(entries);
            MapSpriteAtlasSprites(entries, out spriteOld2New);
        }

        private static void SetSpriteAtlasSprites(
            IDictionary<Texture2D, List<Sprite>> source,
            out List<(List<(Sprite, string)>, HashSet<Sprite>)> entries)
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                entries = new List<(List<(Sprite, string)>, HashSet<Sprite>)>();
                foreach (var tex2Sprites in source)
                {
                    var assetPath = AssetDatabase.GetAssetPath(tex2Sprites.Key);

                    var packedSprites = new Dictionary<Sprite, string>();
                    var otherSprites = new HashSet<Sprite>();
                    foreach (var sprite in tex2Sprites.Value)
                    {
                        string spritePath;
                        if ((spritePath = AssetDatabase.GetAssetPath(sprite)) != assetPath && sprite.packed)
                            packedSprites.Add(sprite, spritePath);
                        else otherSprites.Add(sprite);
                    }

                    var sprite2TexNewPath = new List<(Sprite, string)>();
                    foreach (var pair in packedSprites.ToArray())
                    {
                        if (!pair.Key.TryCreateTextureFromAtlasSprite(out var tex))
                        {
                            packedSprites.Remove(pair.Key);
                        }
                        else
                        {
                            string texNewPath;
                            byte[] data;
                            using (ObjectHandle<Texture2D>.Get(tex, out tex))
                            {
                                if (tex.alphaIsTransparency)
                                {
                                    texNewPath =
                                        AssetDatabase.GenerateUniqueAssetPath(
                                            $"{FixEditorUtils.GetAssetPathWithoutExtension(pair.Value)}.png");
                                    data = tex.EncodeToPNG();
                                }
                                else
                                {
                                    texNewPath =
                                        AssetDatabase.GenerateUniqueAssetPath(
                                            $"{FixEditorUtils.GetAssetPathWithoutExtension(pair.Value)}.jpg");
                                    data = tex.EncodeToJPG();
                                }
                            }

                            File.WriteAllBytes(texNewPath, data);
                            sprite2TexNewPath.Add((pair.Key, texNewPath));
                        }
                    }

                    entries.Add((sprite2TexNewPath, otherSprites));
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }
        }

        private static void SyncSpriteAtlasSprites(
            List<(List<(Sprite sprite, string path)> sprite2NewPathTex, HashSet<Sprite> otherSprites)> entries)
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var (sprite2NewPathTex, otherSprites) in entries)
                {
                    foreach (var (sprite, path) in sprite2NewPathTex)
                    {
                        ImageHeaderReader.TryGetImageSize(File.ReadAllBytes(path), out var width, out var height);

                        var importer = (TextureImporter) AssetImporter.GetAtPath(path);
                        importer.textureType = TextureImporterType.Sprite;
                        importer.spriteImportMode = SpriteImportMode.Single;
                        importer.spriteImportMode = SpriteImportMode.Multiple;
                        importer.spritePixelsPerUnit = sprite.pixelsPerUnit;
                        importer.spritesheet = new SpriteMetaData[]
                        {
                            new SpriteMetaData()
                            {
                                name = sprite.name,
                                pivot = (sprite.pivot / sprite.rect.size),
                                border = sprite.border,
                                rect = new Rect(0, 0, width, height),
                            }
                        };

                        importer.SaveAndReimport();
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }
        }

        private static void MapSpriteAtlasSprites(
            List<(List<(Sprite sprite, string path)> sprite2TexNewPath, HashSet<Sprite> otherSprites)> entries,
            out IDictionary<Sprite, Sprite> spriteOld2New)
        {
            spriteOld2New = new Dictionary<Sprite, Sprite>();
            foreach (var (sprite2TexNewPath, otherSprites) in entries)
            {
                var newSprites = sprite2TexNewPath
                    .ToDictionary(
                        e => e.sprite,
                        e => AssetDatabase.LoadAllAssetRepresentationsAtPath(e.path)
                            .OfType<Sprite>()
                            .ToList());
                foreach (var oldSprite in sprite2TexNewPath
                    .Select(e => e.sprite)
                    .Concat(otherSprites))
                {
                    if (newSprites.TryGetValue(oldSprite, out var list))
                    {
                        int i = list.FindIndex(e => e.name == oldSprite.name);
                        if (i >= 0)
                        {
                            spriteOld2New.Add(oldSprite, list[i]);
                            continue;
                        }
                    }

                    Debug.LogError($"Map {oldSprite} failure", oldSprite);
                }
            }
        }

        #endregion

        private static void Replace(IEnumerable<KeyValuePair<Sprite, Sprite>> spriteOld2New)
        {
            var removedSprites = new List<string>();
            var repList = new List<KeyValuePair<string, string>>();
            foreach (var pair in spriteOld2New)
            {
                var @old = pair.Key;
                var @new = pair.Value;
                if (@old.TryGetRefString(out var oldStr)
                    && @new.TryGetRefString(out var newStr))
                {
                    repList.Add(new KeyValuePair<string, string>(oldStr, newStr));
                    var spritePath = AssetDatabase.GetAssetPath(@old);
                    if (AssetDatabase.GetAssetPath(@old.texture) != spritePath)
                        removedSprites.Add(spritePath);
                }
            }

            Resources.UnloadUnusedAssets();
            GC.Collect();
            Folder.Mkdir();
            File.WriteAllText(FilePath, JsonConvert.SerializeObject(removedSprites, Formatting.Indented));
            FixEditorExtension.BatchReplaceRef(repList);
            var msgList = new List<string>()
            {
                $"若修改了原图片尺寸会替换失败,将类的{nameof(LegacyMethod)}改为{true}",
                $"如果生成的图片数据不对修改 SpriteUtils.GetRTRect 方法",
                $"可通过  {RemoveName}  删除精灵图",
            };
            EditorUtility.DisplayDialog("提示", string.Join("\n", msgList.Select((s, i) => $"{i + 1}.{s}")), "确认", "取消");
        }

        private static void LegacyReplace(
            IEnumerable<KeyValuePair<Texture2D, List<Sprite>>> source,
            IDictionary<Sprite, Sprite> spriteOld2New)
        {
            foreach (var pair in source)
            {
                var texPath = AssetDatabase.GetAssetPath(pair.Key);
                var dictionary = new Dictionary<Texture2D, Dictionary<string, Sprite>>();
                var sprites = pair.Value;
                foreach (var sprite in sprites.Where(e => AssetDatabase.GetAssetPath(e) == texPath).ToArray())
                {
                    sprites.Remove(sprite);
                    if (!dictionary.TryGetValue(sprite.texture, out var map))
                        dictionary.Add(sprite.texture, map = new Dictionary<string, Sprite>());
                    map.Add(sprite.name, sprite);
                }

                foreach (var @old in sprites)
                {
                    if (dictionary.TryGetValue(@old.texture, out var map)
                        && MaxMatch(@old.name, map, out var @new))
                    {
                        spriteOld2New.Add(old, @new);
                    }
                }
            }
        }

        private static void NewReplace(
            IEnumerable<KeyValuePair<Texture2D, List<Sprite>>> source,
            IDictionary<Sprite, long> cacheFeature,
            IDictionary<Sprite, Sprite> spriteOld2New)
        {
            var map = source.ToDictionary(e => e.Key, e =>
            {
                var importer = (TextureImporter) AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(e.Key));
                List<Sprite>
                    spritesInProject = new List<Sprite>(),
                    spritesInTexture = new List<Sprite>();
                var texPath = AssetDatabase.GetAssetPath(e.Key);
                foreach (var sprite in e.Value)
                {
                    if (AssetDatabase.GetAssetPath(sprite) == texPath)
                        spritesInTexture.Add(sprite);
                    else spritesInProject.Add(sprite);
                }

                return new ValueDetails()
                {
                    spritesInProject = spritesInProject,
                    spritesInTexture = spritesInTexture,
                    importMode = importer.spriteImportMode,
                };
            });
            var featureDictionary = new Dictionary<long, Sprite>();
            foreach (var pair in map)
            {
                featureDictionary.Clear();
                var details = pair.Value;
                foreach (var sprite in details.spritesInTexture)
                {
                    if (!cacheFeature.TryGetValue(sprite, out var feature))
                        cacheFeature.Add(sprite, feature = GetSpriteFeature(sprite));

                    featureDictionary[feature] = sprite;
                }

                foreach (var @old in details.spritesInProject)
                {
                    if (MaxMatch(@old, details.importMode, featureDictionary, cacheFeature, out var @new))
                    {
                        spriteOld2New.Add(old, @new);
                    }
                }
            }
        }


        private class ValueDetails
        {
            public SpriteImportMode importMode;
            public List<Sprite> spritesInProject;
            public List<Sprite> spritesInTexture;
        }

        private static float[] GetDiffArray(Sprite source, Sprite target)
        {
            var result = new List<float>();
            if (source.texture != target.texture) result.Add(float.MaxValue / 2);
            using (var s = new SerializedObject(source))
            using (var t = new SerializedObject(target))
            {
                Rect r1 = s.FindProperty("m_Rect").rectValue, r2 = t.FindProperty("m_Rect").rectValue;
                result.Add((new Vector4(r1.x, r1.y, r1.height, r1.width) - new Vector4(r2.x, r2.y, r2.height, r2.width))
                    .sqrMagnitude);
                result.Add((s.FindProperty("m_Border").vector4Value - t.FindProperty("m_Border").vector4Value)
                    .sqrMagnitude);
                result.Add((s.FindProperty("m_Pivot").vector2Value - t.FindProperty("m_Pivot").vector2Value)
                    .sqrMagnitude);
            }

            return result.ToArray();
        }

        private static long GetSpriteFeature(Sprite sprite)
        {
            long code = 0;
            MixHash(ref code, sprite.texture);
            using (var so = new SerializedObject(sprite))
            {
                var rect = so.FindProperty("m_Rect").rectValue;
                MixHash(ref code, rect);
                var border = so.FindProperty("m_Border").vector4Value;
                MixHash(ref code, border);
                var pivot = so.FindProperty("m_Pivot").vector2Value;
                MixHash(ref code, pivot);
            }

            return code;
        }

        private static void MixHash(ref long code, object obj)
        {
            code <<= 4;
            code ^= obj.GetHashCode();
        }

        private static bool MaxMatch(
            Sprite key,
            SpriteImportMode importMode,
            Dictionary<long, Sprite> featureDictionary,
            IDictionary<Sprite, long> cacheFeature,
            out Sprite match)
        {
            if (!cacheFeature.TryGetValue(key, out var feature))
                cacheFeature.Add(key, feature = GetSpriteFeature(key));
            if (featureDictionary.TryGetValue(feature, out match)) return true;
            if (importMode == SpriteImportMode.Single)
            {
                var values = featureDictionary.Values;

                match = values.FirstOrDefault();
                return match != null;
            }
            else
            {
                float diff = float.MaxValue;
                foreach (var sprite in featureDictionary.Values)
                {
                    float v = GetDiffArray(key, sprite).Sum();
                    if (v < diff)
                    {
                        diff = v;
                        match = sprite;
                    }
                }

                return diff < MinMatch;
            }
        }

        private static bool MaxMatch<T>(string key, IDictionary<string, T> dictionary, out T match)
        {
            if (dictionary.TryGetValue(key, out match)) return true;
            if (dictionary.Count == 0)
            {
                match = default;
                return false;
            }

            int maxMatch = 0;
            string matchKey = null;
            foreach (var s in dictionary.Keys)
            {
                var i = Match(s, key);
                if (i <= maxMatch) continue;
                maxMatch = i;
                matchKey = s;
            }

            if (maxMatch == 0 || matchKey == null)
            {
                match = default;
                return false;
            }

            match = dictionary[matchKey];
            return true;
        }

        /// <summary>
        /// 最大公共子序列
        /// </summary>
        private static int Match(string a, string b)
        {
            int m = a.Length, n = b.Length;
            int[,] dp = new int[m + 1, n + 1];
            for (int i = 0; i <= m; i++)
                dp[i, 0] = 1;
            for (int j = 0; j <= n; j++)
                dp[0, j] = 1;
            for (int i = 0; i < m; i++)
            for (int j = 0; j < n; j++)
                dp[i + 1, j + 1] = a[i] == b[j]
                    ? Math.Max(dp[i, j] + 1, Math.Max(dp[i, j + 1], dp[i + 1, j]))
                    : Math.Max(dp[i, j + 1], dp[i + 1, j]);
            return dp[m, n];
        }

        #region utils

        internal class SpriteDataExt : SpriteRect
        {
            public float tessellationDetail = 0;

            // The following lists are to be left un-initialized.
            // If they never loaded or assign explicitly, we avoid writing empty list to metadata.
            public List<Vector2[]> spriteOutline;
            public List<Vertex2DMetaData> vertices;
            public List<int> indices;
            public List<Vector2Int> edges;
            public List<Vector2[]> spritePhysicsOutline;
            public List<SpriteBone> spriteBone;


            public SpriteDataExt From(SerializedProperty sheetElement)
            {
                rect = sheetElement.FindPropertyRelative("m_Rect").rectValue;
                border = sheetElement.FindPropertyRelative("m_Border").vector4Value;
                alignment = (SpriteAlignment) sheetElement.FindPropertyRelative("m_Alignment").intValue;
                pivot = GetPivotValue(alignment, sheetElement.FindPropertyRelative("m_Pivot").vector2Value);
                tessellationDetail = sheetElement.FindPropertyRelative("m_TessellationDetail").floatValue;
                return this;
            }

            public SpriteDataExt From(Sprite sprite)
            {
                using (var so = new SerializedObject(sprite))
                {
                    rect = so.FindProperty("m_Rect").rectValue;
                    border = so.FindProperty("m_Border").vector4Value;
                    pivot = so.FindProperty("m_Pivot").vector2Value;
                    alignment = GetSpriteAlignment(pivot);
                    var ppu = so.FindProperty("m_PixelsToUnits").floatValue;
                    spriteBone = LoadBones(so.FindProperty("m_Bones"), ppu);
                    spritePhysicsOutline = LoadPhysicsShape(so.FindProperty("m_PhysicsShape"), ppu);
                }

                return this;
            }

            public void Apply(SerializedProperty sp)
            {
                sp.FindPropertyRelative("m_Rect").rectValue = rect;
                sp.FindPropertyRelative("m_Border").vector4Value = border;
                sp.FindPropertyRelative("m_Alignment").intValue = (int) alignment;
                sp.FindPropertyRelative("m_Pivot").vector2Value = pivot;
                sp.FindPropertyRelative("m_TessellationDetail").floatValue = tessellationDetail;

                if (spriteBone != null)
                    SyncBone(sp, spriteBone);
                if (spriteOutline != null)
                    SyncOutline(sp, spriteOutline);
                if (spritePhysicsOutline != null)
                    SyncPhysicsShape(sp, spritePhysicsOutline);
                if (vertices != null)
                    SyncMeshData(sp, vertices, indices, edges);
            }

            public static Vector2 GetPivotValue(SpriteAlignment alignment, Vector2 customOffset)
            {
                switch (alignment)
                {
                    case SpriteAlignment.BottomLeft:
                        return new Vector2(0f, 0f);
                    case SpriteAlignment.BottomCenter:
                        return new Vector2(0.5f, 0f);
                    case SpriteAlignment.BottomRight:
                        return new Vector2(1f, 0f);

                    case SpriteAlignment.LeftCenter:
                        return new Vector2(0f, 0.5f);
                    case SpriteAlignment.Center:
                        return new Vector2(0.5f, 0.5f);
                    case SpriteAlignment.RightCenter:
                        return new Vector2(1f, 0.5f);

                    case SpriteAlignment.TopLeft:
                        return new Vector2(0f, 1f);
                    case SpriteAlignment.TopCenter:
                        return new Vector2(0.5f, 1f);
                    case SpriteAlignment.TopRight:
                        return new Vector2(1f, 1f);

                    case SpriteAlignment.Custom:
                        return customOffset;
                }

                return Vector2.zero;
            }

            public static SpriteAlignment GetSpriteAlignment(Vector2 pivot)
            {
                if (pivot == new Vector2(0f, 0f)) return SpriteAlignment.BottomLeft;
                if (pivot == new Vector2(0.5f, 0f)) return SpriteAlignment.BottomCenter;
                if (pivot == new Vector2(1f, 0f)) return SpriteAlignment.BottomRight;

                if (pivot == new Vector2(0f, 0.5f)) return SpriteAlignment.LeftCenter;
                if (pivot == new Vector2(0.5f, 0.5f)) return SpriteAlignment.Center;
                if (pivot == new Vector2(1f, 0.5f)) return SpriteAlignment.RightCenter;

                if (pivot == new Vector2(0f, 1f)) return SpriteAlignment.TopLeft;
                if (pivot == new Vector2(0.5f, 1f)) return SpriteAlignment.TopCenter;
                if (pivot == new Vector2(1f, 1f)) return SpriteAlignment.TopRight;
                return SpriteAlignment.Custom;
            }

            /*
            private Vertex2DMetaData[] LoadVertex2DMetaData(SerializedObject importer, SpriteImportMode mode, int index)
            {
                var so = mode == SpriteImportMode.Multiple
                    ? importer.FindProperty("m_SpriteSheet.m_Sprites").GetArrayElementAtIndex(index)
                    : importer.FindProperty("m_SpriteSheet");

                var verticesSP = so.FindPropertyRelative("m_Vertices");
                var weightsSP = so.FindPropertyRelative("m_Weights");

                var vertices = new Vertex2DMetaData[verticesSP.arraySize];
                for (int i = 0; i < verticesSP.arraySize; ++i)
                {
                    var vsp = verticesSP.GetArrayElementAtIndex(i);
                    var wsp = weightsSP.GetArrayElementAtIndex(i);

                    vertices[i] = new Vertex2DMetaData
                    {
                        position = vsp.vector2Value,
                        boneWeight = new BoneWeight
                        {
                            weight0 = wsp.FindPropertyRelative("weight[0]").floatValue,
                            weight1 = wsp.FindPropertyRelative("weight[1]").floatValue,
                            weight2 = wsp.FindPropertyRelative("weight[2]").floatValue,
                            weight3 = wsp.FindPropertyRelative("weight[3]").floatValue,
                            boneIndex0 = wsp.FindPropertyRelative("boneIndex[0]").intValue,
                            boneIndex1 = wsp.FindPropertyRelative("boneIndex[1]").intValue,
                            boneIndex2 = wsp.FindPropertyRelative("boneIndex[2]").intValue,
                            boneIndex3 = wsp.FindPropertyRelative("boneIndex[3]").intValue
                        }
                    };
                }

                return vertices;
            }

            private int[] LoadIndices(SerializedObject importer, SpriteImportMode mode, int index)
            {
                var so = mode == SpriteImportMode.Multiple
                    ? importer.FindProperty("m_SpriteSheet.m_Sprites").GetArrayElementAtIndex(index)
                    : importer.FindProperty("m_SpriteSheet");

                var indicesSP = so.FindPropertyRelative("m_Indices");

                var indices = new int[indicesSP.arraySize];
                for (int i = 0; i < indicesSP.arraySize; ++i)
                {
                    indices[i] = indicesSP.GetArrayElementAtIndex(i).intValue;
                }

                return indices;
            }

            private Vector2Int[] LoadEdges(SerializedObject importer, SpriteImportMode mode, int index)
            {
                var so = mode == SpriteImportMode.Multiple
                    ? importer.FindProperty("m_SpriteSheet.m_Sprites").GetArrayElementAtIndex(index)
                    : importer.FindProperty("m_SpriteSheet");

                var edgesSP = so.FindPropertyRelative("m_Edges");

                var edges = new Vector2Int[edgesSP.arraySize];
                for (int i = 0; i < edgesSP.arraySize; ++i)
                {
                    edges[i] = edgesSP.GetArrayElementAtIndex(i).vector2IntValue;
                }

                return edges;
            }*/

            public static List<Vector2[]> LoadPhysicsShape(SerializedProperty element, float ppu)
            {
                var outline = new List<Vector2[]>();
                for (int j = 0; j < element.arraySize; ++j)
                {
                    SerializedProperty outlinePathSP = element.GetArrayElementAtIndex(j);
                    var o = new Vector2[outlinePathSP.arraySize];
                    for (int k = 0; k < outlinePathSP.arraySize; ++k)
                    {
                        o[k] = outlinePathSP.GetArrayElementAtIndex(k).vector2Value * ppu;
                    }

                    outline.Add(o);
                }

                return outline;
            }

            private static List<SpriteBone> LoadBones(SerializedProperty element, float ppu)
            {
                var spriteBone = new List<SpriteBone>(element.arraySize);
                for (int i = 0; i < element.arraySize; ++i)
                {
                    var boneSO = element.GetArrayElementAtIndex(i);
                    var sb = new SpriteBone();
                    sb.length = boneSO.FindPropertyRelative("length").floatValue * ppu;
                    sb.position = boneSO.FindPropertyRelative("position").vector3Value * ppu;
                    sb.rotation = boneSO.FindPropertyRelative("rotation").quaternionValue;
                    sb.parentId = boneSO.FindPropertyRelative("parentId").intValue;
                    sb.name = boneSO.FindPropertyRelative("name").stringValue;
                    spriteBone.Add(sb);
                }

                return spriteBone;
            }

            public static void SyncBone(SerializedProperty element, List<SpriteBone> spriteBone)
            {
                var sp = element.FindPropertyRelative("m_Bones");
                sp.arraySize = spriteBone.Count;
                for (int i = 0; i < sp.arraySize; ++i)
                {
                    var boneSO = sp.GetArrayElementAtIndex(i);
                    var sb = spriteBone[i];
                    boneSO.FindPropertyRelative("length").floatValue = sb.length;
                    boneSO.FindPropertyRelative("position").vector3Value = sb.position;
                    boneSO.FindPropertyRelative("rotation").quaternionValue = sb.rotation;
                    boneSO.FindPropertyRelative("parentId").intValue = sb.parentId;
                    boneSO.FindPropertyRelative("name").stringValue = sb.name;
                }
            }

            public static void SyncOutline(SerializedProperty element, List<Vector2[]> outline)
            {
                var outlineSP = element.FindPropertyRelative("m_Outline");
                outlineSP.ClearArray();
                for (int j = 0; j < outline.Count; ++j)
                {
                    outlineSP.InsertArrayElementAtIndex(j);
                    var o = outline[j];
                    SerializedProperty outlinePathSP = outlineSP.GetArrayElementAtIndex(j);
                    outlinePathSP.ClearArray();
                    for (int k = 0; k < o.Length; ++k)
                    {
                        outlinePathSP.InsertArrayElementAtIndex(k);
                        outlinePathSP.GetArrayElementAtIndex(k).vector2Value = o[k];
                    }
                }
            }

            public static void SyncPhysicsShape(SerializedProperty element, List<Vector2[]> value)
            {
                var outlineSP = element.FindPropertyRelative("m_PhysicsShape");
                outlineSP.ClearArray();
                for (int j = 0; j < value.Count; ++j)
                {
                    outlineSP.InsertArrayElementAtIndex(j);
                    var o = value[j];
                    SerializedProperty outlinePathSP = outlineSP.GetArrayElementAtIndex(j);
                    outlinePathSP.ClearArray();
                    for (int k = 0; k < o.Length; ++k)
                    {
                        outlinePathSP.InsertArrayElementAtIndex(k);
                        outlinePathSP.GetArrayElementAtIndex(k).vector2Value = o[k];
                    }
                }
            }

            public static void SyncMeshData(SerializedProperty element, List<Vertex2DMetaData> vertices,
                List<int> indices,
                List<Vector2Int> edges)
            {
                var verticesSP = element.FindPropertyRelative("m_Vertices");
                var weightsSP = element.FindPropertyRelative("m_Weights");
                var indicesSP = element.FindPropertyRelative("m_Indices");
                var edgesSP = element.FindPropertyRelative("m_Edges");

                verticesSP.arraySize = vertices.Count;
                weightsSP.arraySize = vertices.Count;

                for (int i = 0; i < vertices.Count; ++i)
                {
                    var vsp = verticesSP.GetArrayElementAtIndex(i);
                    var wsp = weightsSP.GetArrayElementAtIndex(i);

                    vsp.vector2Value = vertices[i].position;
                    wsp.FindPropertyRelative("weight[0]").floatValue = vertices[i].boneWeight.weight0;
                    wsp.FindPropertyRelative("weight[1]").floatValue = vertices[i].boneWeight.weight1;
                    wsp.FindPropertyRelative("weight[2]").floatValue = vertices[i].boneWeight.weight2;
                    wsp.FindPropertyRelative("weight[3]").floatValue = vertices[i].boneWeight.weight3;
                    wsp.FindPropertyRelative("boneIndex[0]").intValue = vertices[i].boneWeight.boneIndex0;
                    wsp.FindPropertyRelative("boneIndex[1]").intValue = vertices[i].boneWeight.boneIndex1;
                    wsp.FindPropertyRelative("boneIndex[2]").intValue = vertices[i].boneWeight.boneIndex2;
                    wsp.FindPropertyRelative("boneIndex[3]").intValue = vertices[i].boneWeight.boneIndex3;
                }

                indicesSP.arraySize = indices.Count;

                for (int i = 0; i < indices.Count; ++i)
                {
                    indicesSP.GetArrayElementAtIndex(i).intValue = indices[i];
                }

                edgesSP.arraySize = edges.Count;

                for (int i = 0; i < edges.Count; ++i)
                {
                    edgesSP.GetArrayElementAtIndex(i).vector2IntValue = edges[i];
                }
            }
        }

        #endregion

#endif
    }
}