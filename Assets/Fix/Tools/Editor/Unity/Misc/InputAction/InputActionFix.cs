#if USE_UNITY_INPUTSYSTEM
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine.InputSystem;

namespace Fix.Editor
{
    public static class InputActionFix
    {
        private const string InputActionExtension = ".inputactions";
        private static readonly Type InputActionInterfaceType = typeof(IInputActionCollection);
        private static readonly string[] FindRoot = new string[] {"Assets"};

        [MenuItem("Assets/" + nameof(Fix) + "/" + nameof(InputActionFix), true)]
        public static bool IsValidateAsset()
        {
            return Selection.activeObject is InputActionAsset;
        }

        [MenuItem("Assets/" + nameof(Fix) + "/" + nameof(InputActionFix))]
        public static void Create()
        {
            if (!(Selection.activeObject is InputActionAsset asset)) return;
            AssetDatabase.StartAssetEditing();
            string assetPath, newAssetPath, assetJson;
            var deleteAssets = new List<string>();
            try
            {
                assetPath = AssetDatabase.GetAssetPath(asset);
                if (Path.GetExtension(assetPath) == InputActionExtension) return;
                //Create Asset
                assetJson = asset.ToJson();


                newAssetPath = Path.ChangeExtension(assetPath, ".inputactions");
                File.WriteAllText(newAssetPath, assetJson);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();
            }

            AssetDatabase.StartAssetEditing();
            try
            {
                //Rep Ref

                deleteAssets.Add(assetPath);
                var repDictionary = new Dictionary<string, string>();
                if (!AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath)
                        .TryGetRefString(out var oldAssetStr)
                    || !AssetDatabase.LoadAssetAtPath<InputActionAsset>(newAssetPath)
                        .TryGetRefString(out var newAssetStr))
                    throw new Exception("Invalid Data");
                repDictionary.Add(oldAssetStr, newAssetStr);
                var fixActionRefs = AssetDatabase.LoadAllAssetRepresentationsAtPath(newAssetPath)
                    .OfType<InputActionReference>().ToList();
                var actionRefs = AssetDatabase.FindAssets("t:InputActionReference", FindRoot)
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Select(AssetDatabase.LoadAssetAtPath<InputActionReference>)
                    .Where(e => !fixActionRefs.Contains(e));
                var actionId2ActionRef = fixActionRefs.ToDictionary(e => e.ToString());
                foreach (var reference in actionRefs)
                {
                    if (!actionId2ActionRef.TryGetValue(reference.ToString(), out var newActionRef)) continue;
                    if (!reference.TryGetRefString(out var oldStr) || !newActionRef.TryGetRefString(out var newStr))
                        throw new Exception("Invalid Data");
                    repDictionary.Add(oldStr, newStr);
                    deleteAssets.Add(AssetDatabase.GetAssetPath(reference));
                }

                FixEditorExtension.BatchReplaceRef(repDictionary);
                //Delete
                if (EditorUtility.DisplayDialog("将要删除以下资源", string.Join("\n", deleteAssets), "确认", "取消"))
                {
                    foreach (var deleteAsset in deleteAssets)
                        AssetDatabase.DeleteAsset(deleteAsset);
                }

                //Create Script
                var assetScriptType = AppDomain
                    .CurrentDomain
                    .GetAssemblies()
                    .SelectMany(e =>
                    {
                        try
                        {
                            return e.GetTypes();
                        }
                        catch
                        {
                            return new Type[0];
                        }
                    })
                    .Where(e => !e.IsAbstract && InputActionInterfaceType.IsAssignableFrom(e))
                    .FirstOrDefault(type =>
                    {
                        try
                        {
                            var instance = Activator.CreateInstance(type);

                            var json =
                                (type.GetProperty("asset").GetValue(instance) as InputActionAsset).ToJson();
                            return json == assetJson;
                        }
                        catch
                        {
                            return false;
                        }
                    });

                if (assetScriptType != null)
                {
                    var scriptPath = AssetDatabase
                        .FindAssets($"t:MonoScript {assetScriptType.Name}", FindRoot)
                        .Select(AssetDatabase.GUIDToAssetPath)
                        .FirstOrDefault(path =>
                            AssetDatabase.LoadAssetAtPath<MonoScript>(path).GetClass() == assetScriptType);
                    if (scriptPath == null) return;
                    AssetDatabase.DeleteAsset(scriptPath);
                    var importer = AssetImporter.GetAtPath(newAssetPath);
                    using (var so = new SerializedObject(importer))
                    {
                        so.FindProperty("m_GenerateWrapperCode").boolValue = true;
                        so.FindProperty("m_WrapperClassName").stringValue = assetScriptType.Name;
                        so.FindProperty("m_WrapperCodeNamespace").stringValue = assetScriptType.Namespace;
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }

                    importer.SaveAndReimport();
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();
            }
        }
    }
}
#endif