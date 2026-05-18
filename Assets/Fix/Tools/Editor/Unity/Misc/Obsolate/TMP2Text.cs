#if false

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using FontStyles = TMPro.FontStyles;
using Object = UnityEngine.Object;

namespace Fix.Editor
{
    //todo 将来换成UniqueID
    /// <summary>
    ///     ------------------------暂时只有场景的-----------------------
    /// <para>推荐将 <see cref="TMPro.TextMeshProUGUI"/> 换成 <see cref="UnityEngine.UI.Graphic"/> 然后用 as 转为 <see cref="UnityEngine.UI.Text"/></para>
    /// <para><see cref="SaveRef"/> 保存/刷新引用 </para>
    /// <para><see cref="ToText"/> 在Hierarchy右键使用(支持批量) </para>
    /// </summary>
    public static class TMP2Text
    {
        private const string OutlineKeyword = "OUTLINE_ON";
        private static readonly int OutlineUVSpeedY = Shader.PropertyToID("_OutlineUVSpeedY");
        private static readonly int OutlineUVSpeedX = Shader.PropertyToID("_OutlineUVSpeedX");
        private static readonly int OutlineColor = Shader.PropertyToID("_OutlineColor");
        private static string WorkspaceFolder => nameof(TMP2Text).GetWorkspaceFolder();
        private static string SaveFile => WorkspaceFolder + "/TMP_Ref.json";

        private static Dictionary<string, Dictionary<ulong, Dictionary<string, List<string>>>>
            refFromFile;

        private static readonly Dictionary<string, bool> checkTypeMemo = new Dictionary<string, bool>();
        private static readonly Type TextType = typeof(Text);

        /// <summary>
        /// 保存引用
        /// </summary>
        [MenuItem("Tools/" + nameof(Fix) + "/TMP/" + nameof(SaveRef))]
        private static void SaveRef()
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                var settingsScenes = EditorBuildSettings.scenes;
                int c = 0, length = settingsScenes.Length;
                var path2Ref = new Dictionary<string, Dictionary<ulong, Dictionary<string, List<string>>>>();
                foreach (var scenePath in settingsScenes.Select(e => e.path))
                {
                    EditorUtility.DisplayProgressBar("场景", $"{c}/{length}", (float) c / length);
                    var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                    try
                    {
                        path2Ref.Add(scenePath, GetRef(scene.GetRootGameObjects()));
                    }
                    finally
                    {
                        EditorSceneManager.CloseScene(scene, true);
                    }

                    c++;
                }

                // var tempScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
                var assets = AssetDatabase.FindAssets("t:Prefab", new string[] {"Assets"});
                c = 0;
                length = assets.Length;
                try
                {
                    foreach (var prefabPath in assets.Select(AssetDatabase.GUIDToAssetPath))
                    {
                        EditorUtility.DisplayProgressBar("预制体", $"{c}/{length}", (float) c / length);
                        
                        var prefab = PrefabUtility.LoadPrefabContents(prefabPath);
                        try
                        {
                            if (prefab.GetComponentsInChildren<TextMeshProUGUI>(true).Length <= 0) continue;
                            path2Ref.Add(prefabPath,
                                GetRef(new GameObject[] {prefab}));
                        }
                        finally
                        {
                            PrefabUtility.UnloadPrefabContents(prefab);
                        }
                        
                        c++;
                    }
                }
                catch(Exception e){ Debug.Log(e);}
                finally
                {
                    // EditorSceneManager.CloseScene(tempScene, true);
                    // AssetDatabase.DeleteAsset(tempScene.path);
                }

                Path.GetDirectoryName(SaveFile).Mkdir();
                File.WriteAllText(SaveFile, JsonConvert.SerializeObject(path2Ref, Formatting.Indented));
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                refFromFile = null;
            }
        }


        [MenuItem("GameObject/" + nameof(ToText), false, -300)]
        private static void ToText()
        {
            ReadRef();
            var objects = Selection.gameObjects;
            foreach (var go in objects) ConvertTextMeshProUGUIToText(go);
        }

        [MenuItem("Assets/" + nameof(PrefabToText), false, -300)]
        private static void PrefabToText()
        {
            ReadRef();
            var paths = Selection.objects.OfType<GameObject>().Select(AssetDatabase.GetAssetPath).ToArray();
            if (paths.Length == 0) return;
            try
            {
                foreach (var path in paths)
                {
                    var prefab = PrefabUtility.LoadPrefabContents(path);
                    // var instantiatePrefab = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    // foreach (var go in instantiatePrefab.GetComponentsInChildren<TextMeshProUGUI>(true).Select(e=>e.gameObject))
                    //     ConvertTextMeshProUGUIToText(go,path);
                    // foreach (var go in instantiatePrefab.GetComponentsInChildren<Transform>(true).Select(e=>e.gameObject))
                    //     GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                    // PrefabUtility.SaveAsPrefabAsset(instantiatePrefab, path);
                    try
                    {
                        foreach (var go in prefab.GetComponentsInChildren<TextMeshProUGUI>(true).Select(e=>e.gameObject)) 
                            ConvertTextMeshProUGUIToText(go,path);
                        PrefabUtility.SavePrefabAsset(prefab);
                    }
                    finally
                    {
                        PrefabUtility.UnloadPrefabContents(prefab);
                    }
                }
            }
            finally
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private static Dictionary<ulong, Dictionary<string, List<string>>> GetRef(IEnumerable<GameObject> roots)
        {
            var refDictionary = new Dictionary<ulong, Dictionary<string, List<string>>>();
            foreach (var component in
                roots.SelectMany(e => e.GetComponentsInChildren<Component>(true)))
            {
                if (component == null) continue;
                var globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(component);

                var so = new SerializedObject(component);
                var property = so.GetIterator();
                while (property.Next(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference ||
                        !(property.objectReferenceValue is TextMeshProUGUI tmp)) continue;
                    var tmpId = GlobalObjectId.GetGlobalObjectIdSlow(tmp).targetObjectId;
                    if (!refDictionary.TryGetValue(tmpId, out var dictionary))
                        refDictionary.Add(tmpId, dictionary = new Dictionary<string, List<string>>());
                    var key = globalObjectId.ToString();
                    if (!dictionary.TryGetValue(key, out var list))
                        dictionary.Add(key, list = new List<string>());
                    list.Add(property.propertyPath);
                }

                so.Dispose();
            }

            return refDictionary;
        }

        private static void ReadRef()
        {
            if (!File.Exists(SaveFile))
            {
                if (EditorUtility.DisplayDialog("未保存依赖", "是否创建依赖文件", "是的", "取消"))
                    SaveRef();
            }

            if (!File.Exists(SaveFile))
            {
                Debug.LogError("找不到依赖文件");
                return;
            }

            refFromFile = refFromFile ?? JsonConvert
                .DeserializeObject<Dictionary<string, Dictionary<ulong, Dictionary<string, List<string>>>>>(
                    File.ReadAllText(SaveFile));
        }

        private static void ConvertTextMeshProUGUIToText(GameObject root,string assetPath=default)
        {
            var tmp = root.GetComponent<TextMeshProUGUI>();
            if (tmp == null) return;
            var tmpId = GlobalObjectId.GetGlobalObjectIdSlow(tmp).targetObjectId;
            var path = !string.IsNullOrWhiteSpace(assetPath) 
                ?assetPath
                :!string.IsNullOrWhiteSpace(root.scene.path)
                    ? root.scene.path 
                    : GetPrefabPath(root);
            if (!IsValidRef(path, tmpId)) return;
            var data = TextData.FromTMP(tmp);
            Object.DestroyImmediate(tmp);
            var text = root.AddComponent<Text>();
            TextData.ToText(data, text);
            EditorUtility.SetDirty(root);
            //ref
            var refer = refFromFile[path][tmpId];
            foreach (var pair in refer)
            {
                if (!GlobalObjectId.TryParse(pair.Key, out var id))
                {
                    
                    continue;
                }
                var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id);
                using (var so = new SerializedObject(obj))
                {
                    foreach (var propertyPath in pair.Value) so.FindProperty(propertyPath).objectReferenceValue = text;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            EditorUtility.SetDirty(root);
        }
        /// <summary>
        /// 在Hierarchy界面绑定不了prefab
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        private static string GetPrefabPath(GameObject root)
        {
            return 
#if UNITY_2020_OR_NEWER
                    PrefabStageUtility.GetPrefabStage(root)?.assetPath
#else
                    UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetPrefabStage(root).prefabAssetPath
#endif
                ;
        }

        private static bool IsValidRef(string path, ulong tmpId)
        {
            if (!refFromFile.TryGetValue(path, out var refDictionary))
            {
                Debug.LogError($"未保存的资源路径 : {path}");
                return false;
            }
            
            if (!refDictionary.TryGetValue(tmpId, out var refer)) return true;
            foreach (var pair in refer)
            {
                if (!GlobalObjectId.TryParse(pair.Key, out var id))
                {
                    Debug.LogError($"Invalid : {pair.Key}");
                    return false;
                }
                var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id);
                if (obj == null)
                {
                    Debug.LogError($"Convert {pair.Key} To Object Failure");
                    return false;
                }
                using (var so = new SerializedObject(obj))
                {
                    foreach (var propertyPath in pair.Value)
                    {
                        try
                        {
                            var typeString = so.FindProperty(propertyPath).type;
                            var type = obj.GetDefinitionType(propertyPath);
                            if (!checkTypeMemo.TryGetValue(typeString, out var isValid))
                                checkTypeMemo.Add(typeString, isValid = type.IsAssignableFrom(TextType));
                            if (!isValid) throw new Exception($"无法将{TextType}分配给{type}");
                        }
                        catch (Exception e)
                        {
                            $"无法将 {TextType} 分配给 {so.targetObject.name}.{propertyPath}".LogError();
                            e.LogException();
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private static readonly Regex Indexer = new Regex(@"(?<=data\[)\d+(?=\])");

        private const BindingFlags SearchFlags = BindingFlags.Instance
                                                 | BindingFlags.Public
                                                 | BindingFlags.NonPublic;

        private static Type GetDefinitionType(this object obj, string propertyPath)
        {
            var type = obj.GetType();
            var tokens = propertyPath.Split('.');
            var length = tokens.Length;
        
            for (int i = 0; i < length; i++)
            {
                if (type == null) return null;
                var token = tokens[i];
            
                if (i < length - 1 && "Array".Equals(token) && Indexer.IsMatch(tokens[i + 1]))
                {
                    type = type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0];
                    i++;
                }
                else
                {
                    MemberInfo[] memberInfos;
                    do memberInfos = type.GetMember(token, SearchFlags);
                    while (memberInfos.Length == 0 && (type = type.BaseType) != typeof(object));
                    foreach (var info in memberInfos)
                    {
                        switch (info)
                        {
                            case FieldInfo fieldInfo:
                                type = fieldInfo.FieldType;
                                break;
                            case PropertyInfo propertyInfo:
                                type = propertyInfo.PropertyType;
                                break;
                            default:
                                throw new Exception($"Invalid Token:{type} {token}");
                        }
                    }
                }

            }

            return type;
        }

        private struct TextData
        {
            public string text;
            public Font font;
            public FontStyle fontStyle;
            public int fontSize;
            public int lineSpacing;
            public bool supportRichText;
            public TextAnchor alignment;
            public bool alignByGeometry;
            public HorizontalWrapMode horizontalOverflow;
            public VerticalWrapMode verticalOverflow;
            public bool resizeTextForBestFit;
            public int resizeTextMinSize;
            public int resizeTextMaxSize;
            public Color color;
            public Material material;
            public bool raycastTarget;
            public Vector4 raycastPadding;
            public bool maskable;
            public bool enableOutline;
            public Color effectColor;
            public Vector2 effectDistance;

            public static TextData FromTMP(TextMeshProUGUI tmp)
            {
                var data = new TextData();
                data.text = tmp.text;
                data.font = tmp.font != null ? tmp.font.sourceFontFile : default;
                var tmpFontStyle = tmp.fontStyle;
                if (tmpFontStyle == FontStyles.Normal) data.fontStyle = FontStyle.Normal;
                else if ((tmpFontStyle & FontStyles.Bold) != 0 && (tmpFontStyle & FontStyles.Italic) != 0)
                    data.fontStyle = FontStyle.BoldAndItalic;
                else if ((tmpFontStyle & FontStyles.Bold) != 0) data.fontStyle = FontStyle.Bold;
                else if ((tmpFontStyle & FontStyles.Italic) != 0) data.fontStyle = FontStyle.Italic;
                if ((tmpFontStyle & FontStyles.LowerCase) != 0) data.text = data.text.ToLower();
                else if ((tmpFontStyle & FontStyles.UpperCase) != 0) data.text = data.text.ToUpper();
                data.fontSize = Mathf.CeilToInt(tmp.fontSize);
                data.lineSpacing = Mathf.CeilToInt(tmp.lineSpacing);
                data.supportRichText = tmp.richText;
                data.resizeTextForBestFit = tmp.autoSizeTextContainer;
                data.color = tmp.color;
                data.material = tmp.material;
                data.maskable = tmp.maskable;
                data.resizeTextMinSize = Mathf.CeilToInt(tmp.fontSizeMin);
                data.resizeTextMaxSize = Mathf.CeilToInt(tmp.fontSizeMax);
                switch (tmp.alignment)
                {
                    //upper
                    case TextAlignmentOptions.TopLeft:
                        data.alignment = TextAnchor.UpperLeft;
                        break;
                    case TextAlignmentOptions.Top:
                    case TextAlignmentOptions.TopJustified:
                    case TextAlignmentOptions.TopFlush:
                        data.alignment = TextAnchor.UpperCenter;
                        break;
                    case TextAlignmentOptions.TopGeoAligned:
                        data.alignByGeometry = true;
                        data.alignment = TextAnchor.UpperCenter;
                        break;
                    case TextAlignmentOptions.TopRight:
                        data.alignment = TextAnchor.UpperRight;
                        break;
                    //middle
                    case TextAlignmentOptions.Left:
                    case TextAlignmentOptions.BaselineLeft:
                    case TextAlignmentOptions.MidlineLeft:
                    case TextAlignmentOptions.CaplineLeft:
                        data.alignment = TextAnchor.MiddleLeft;
                        break;
                    case TextAlignmentOptions.Center:
                    case TextAlignmentOptions.Justified:
                    case TextAlignmentOptions.Flush:
                    case TextAlignmentOptions.Baseline:
                    case TextAlignmentOptions.BaselineJustified:
                    case TextAlignmentOptions.BaselineFlush:
                    case TextAlignmentOptions.Midline:
                    case TextAlignmentOptions.MidlineJustified:
                    case TextAlignmentOptions.MidlineFlush:
                    case TextAlignmentOptions.CaplineJustified:
                    case TextAlignmentOptions.Capline:
                    case TextAlignmentOptions.CaplineFlush:
                    case TextAlignmentOptions.Converted:
                        data.alignment = TextAnchor.MiddleCenter;
                        break;
                    case TextAlignmentOptions.CenterGeoAligned:
                    case TextAlignmentOptions.BaselineGeoAligned:
                    case TextAlignmentOptions.CaplineGeoAligned:
                    case TextAlignmentOptions.MidlineGeoAligned:
                        data.alignByGeometry = true;
                        data.alignment = TextAnchor.MiddleCenter;
                        break;
                    case TextAlignmentOptions.Right:
                    case TextAlignmentOptions.BaselineRight:
                    case TextAlignmentOptions.MidlineRight:
                    case TextAlignmentOptions.CaplineRight:
                        data.alignment = TextAnchor.MiddleRight;
                        break;
                    //lower
                    case TextAlignmentOptions.BottomLeft:
                        data.alignment = TextAnchor.LowerLeft;
                        break;
                    case TextAlignmentOptions.Bottom:
                    case TextAlignmentOptions.BottomJustified:
                    case TextAlignmentOptions.BottomFlush:
                        data.alignment = TextAnchor.LowerCenter;
                        break;
                    case TextAlignmentOptions.BottomGeoAligned:
                        data.alignByGeometry = true;
                        data.alignment = TextAnchor.LowerCenter;
                        break;
                    case TextAlignmentOptions.BottomRight:
                        data.alignment = TextAnchor.LowerRight;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (tmp.enableWordWrapping)
                {
                    data.horizontalOverflow = HorizontalWrapMode.Wrap;
                    data.verticalOverflow = VerticalWrapMode.Truncate;
                }
                else
                {
                    switch (tmp.overflowMode)
                    {
                        case TextOverflowModes.Overflow:
                            data.horizontalOverflow = HorizontalWrapMode.Overflow;
                            data.verticalOverflow = VerticalWrapMode.Overflow;
                            break;
                        default:
                            data.horizontalOverflow = HorizontalWrapMode.Wrap;
                            data.verticalOverflow = VerticalWrapMode.Truncate;
                            break;
                    }
                }

                data.raycastTarget = tmp.raycastTarget;
#if UNITY_2020_OR_NEWER
            data.raycastPadding = tmp.raycastPadding;
#endif
                var sharedMaterial = tmp.fontSharedMaterial;
                data.enableOutline = sharedMaterial.IsKeywordEnabled(OutlineKeyword);
                if (data.enableOutline)
                {
                    data.effectColor = sharedMaterial.GetColor(OutlineColor);
                    data.effectDistance = new Vector2(sharedMaterial.GetFloat(OutlineUVSpeedX),
                        sharedMaterial.GetFloat(OutlineUVSpeedY));
                }

                return data;
            }

            public static void ToText(TextData data, Text text)
            {
                text.text = data.text;
                text.font = data.font;
                text.fontStyle = data.fontStyle;
                text.fontSize = data.fontSize;
                text.lineSpacing = data.lineSpacing;
                text.supportRichText = data.supportRichText;
                text.alignment = data.alignment;
                text.alignByGeometry = data.alignByGeometry;
                text.horizontalOverflow = data.horizontalOverflow;
                text.verticalOverflow = data.verticalOverflow;
                text.resizeTextForBestFit = data.resizeTextForBestFit;
                text.resizeTextMinSize = data.resizeTextMinSize;
                text.resizeTextMaxSize = data.resizeTextMaxSize;
                text.color = data.color;
                text.material = data.material;
                text.raycastTarget = data.raycastTarget;
#if UNITY_2020_OR_NEWER
            text.raycastPadding = data.raycastPadding;
#endif
                text.maskable = data.maskable;
                if (data.enableOutline)
                {
                    if (!text.gameObject.TryGetComponent<Outline>(out var outline))
                        outline = text.gameObject.AddComponent<Outline>();
                    outline.effectColor = data.effectColor;
                    outline.effectDistance = data.effectDistance;
                }
            }
        }
    }
}
#endif
