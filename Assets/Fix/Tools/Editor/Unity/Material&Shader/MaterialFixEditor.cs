using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Fix.Editor
{
    public class MaterialFixEditor : FixEditorBase
    {
        private const string Unit = "Material | Shader";

        private static readonly string[] ReplaceExtensions = new[]
        {
            ".unity",
            ".asset",
            ".mat",
            ".prefab",
            ".meta"
        };

        [FixEditor(FixRoot + Unit + "/" + "Shader同名合并")]
        public static void ShaderMerge()
        {
            var dictionary = new Dictionary<string, List<Shader>>();
            foreach (var shader in Resources.FindObjectsOfTypeAll<Shader>())
            {
                var shaderName = shader.name;
                if (!dictionary.TryGetValue(shaderName, out var list))
                    dictionary.Add(shaderName, list = new List<Shader>());
                list.Add(shader);
            }

            foreach (var key in dictionary.Keys.ToArray())
                if (dictionary[key].Count <= 1)
                    dictionary.Remove(key);
            var repDictionary = new Dictionary<string, List<string>>();
            try
            {
                foreach (var pair in dictionary)
                {
                    var shaders = pair.Value;
                    var nativeShader = shaders
                                           .FirstOrDefault(e =>
                                           {
                                               var assetPath = AssetDatabase.GetAssetPath(e);
                                               return assetPath.Equals("Resources/unity_builtin_extra")
                                                      || assetPath.Equals("Resources/tuanjie_builtin_extra")
                                                      || assetPath.StartsWith("Packages")
                                                      || !assetPath.StartsWith("Assets/Shader")
                                                      && !assetPath.StartsWith("Assets/Resources")
                                                   ;
                                           })
                                       ?? shaders[0];
                    if (nativeShader == null) continue;
                    shaders.Remove(nativeShader);
                    if (!nativeShader.TryGetRefString(out var newStr))
                        throw new Exception($"查找{nativeShader}失败");

                    var repList = new List<string>();
                    foreach (var shader in shaders)
                    {
                        if (!shader.TryGetRefString(out var oldStr))
                            throw new Exception($"查找{shader}失败");
                        repList.Add(oldStr);
                    }

                    repDictionary.Add(newStr, repList);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                if (EditorUtility.DisplayDialog("存在未知资源", e.Message, "继续", "取消")) return;
            }

            FixEditorExtension.BatchReplaceRefWithExtensions(
                repDictionary
                    .SelectMany(e =>
                        e.Value.Select(s => new KeyValuePair<string, string>(s, e.Key))
                            .ToList()),
                ReplaceExtensions);
            var removableShader = dictionary.Values.SelectMany(e => e).Select(AssetDatabase.GetAssetPath).ToArray();
            FixEditorExtension.DeleteAssets(removableShader);
            Debug.Log("替换结束,以下shader可移除:");
            Debug.Log(string.Join("\n", removableShader));
        }


        [FixEditor(FixRoot + Unit + "/" + "刷新预制体Shader")]
        public static void ShaderRefresh()
        {
            AssetDatabase.StartAssetEditing();
            var tempScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                var shader = Shader.Find("Unlit/Texture");
                if (shader == null) throw new Exception("Empty");
                foreach (var prefab in AssetDatabase
                    .FindAssets("t:Prefab", new[] {"Assets"})
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
                    .Where(e => e != null)
                )
                {
                    var renderers = prefab.GetComponentsInChildren<Renderer>(true);
                    if (renderers.Length == 0) continue;
                    var instantiatePrefab = (GameObject) PrefabUtility.InstantiatePrefab(prefab);

                    foreach (var renderer in instantiatePrefab.GetComponentsInChildren<Renderer>(true))
                    {
                        foreach (var material in renderer.sharedMaterials)
                        {
                            if (material == null) continue;
                            var primary = material.shader;
                            material.shader = shader;
                            material.shader = primary;
                        }
                    }

                    PrefabUtility.SaveAsPrefabAsset(instantiatePrefab, AssetDatabase.GetAssetPath(prefab));
                    Object.DestroyImmediate(instantiatePrefab);
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(tempScene, true);
                Object.DestroyImmediate(AssetDatabase.LoadAssetAtPath<SceneAsset>(tempScene.path));
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
#if USE_UNITY_URP
        [FixEditor(FixRoot + Unit + "/" + "URP重置材质索引")]
        public static void URP()
        {
            var type = AppDomain
                .CurrentDomain
                .GetAssemblies()
                .Select(assembly =>
                    assembly.GetType("UnityEditor.Rendering.Universal.AssetVersion"))
                .First(e => e != null);
            if (type == null)
            {
                Debug.Log("find failure");
                return;
            }

            AssetDatabase.StartAssetEditing();
            int c = 0;
            try
            {
                foreach (var assetPath in AssetDatabase
                    .FindAssets("t:Material", new[] {"Assets"})
                    .Select(AssetDatabase.GUIDToAssetPath))
                {
                    var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                    var assetVersion = assets.FirstOrDefault(e => e.GetType() == type);
                    if (assetVersion == null) continue;
                    AssetDatabase.RemoveObjectFromAsset(assetVersion);
                    c++;
                }
            }
            finally
            {
                Debug.Log($"Material: {c} ");
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
#endif
    }
}