#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class PopulateSimHeiTmpFont
{
    private const string FontAssetPath = "Assets/Font/simhei SDF.asset";
    private const string TranslationTextPath = "Assets/SceneTexts/Translation/translated_texts.txt";
    private const string HashPath = "Library/PopulateSimHeiTmpFont.hash";

    static PopulateSimHeiTmpFont()
    {
        EditorApplication.delayCall += AutoPopulateIfNeeded;
    }

    [MenuItem("Tools/Fix/Populate SimHei TMP Font")]
    public static void PopulateFromMenu()
    {
        Populate(force: true);
    }

    private static void AutoPopulateIfNeeded()
    {
        Populate(force: false);
    }

    private static void Populate(bool force)
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot))
        {
            Debug.LogError("[PopulateSimHeiTmpFont] Could not resolve project root.");
            return;
        }

        string textFullPath = Path.Combine(projectRoot, TranslationTextPath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(textFullPath))
        {
            Debug.LogError($"[PopulateSimHeiTmpFont] Missing translation text file: {TranslationTextPath}");
            return;
        }

        string text = File.ReadAllText(textFullPath, Encoding.UTF8);
        string characters = new string(text
            .Where(c => c != '\uFEFF' && !char.IsControl(c))
            .Distinct()
            .OrderBy(c => c)
            .ToArray());

        string contentHash = ComputeHash(characters);
        string hashFullPath = Path.Combine(projectRoot, HashPath.Replace('/', Path.DirectorySeparatorChar));
        if (!force && File.Exists(hashFullPath) && File.ReadAllText(hashFullPath) == contentHash)
        {
            return;
        }

        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (fontAsset == null)
        {
            Debug.LogError($"[PopulateSimHeiTmpFont] Missing TMP font asset: {FontAssetPath}");
            return;
        }

        fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        fontAsset.isMultiAtlasTexturesEnabled = true;
        Texture2D primaryAtlas = fontAsset.atlasTexture;
        if (primaryAtlas == null)
        {
            Debug.LogError($"[PopulateSimHeiTmpFont] Missing primary atlas texture on {FontAssetPath}.");
            return;
        }

        fontAsset.TryAddCharacters(characters, out string missingCharacters);

        foreach (Texture2D atlasTexture in fontAsset.atlasTextures)
        {
            if (atlasTexture != null)
            {
                EditorUtility.SetDirty(atlasTexture);
            }
        }

        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();

        Directory.CreateDirectory(Path.GetDirectoryName(hashFullPath));
        File.WriteAllText(hashFullPath, contentHash);

        if (string.IsNullOrEmpty(missingCharacters))
        {
            Debug.Log($"[PopulateSimHeiTmpFont] Added/verified {characters.Length} characters in {FontAssetPath}.");
        }
        else
        {
            Debug.LogWarning($"[PopulateSimHeiTmpFont] Missing {missingCharacters.Length} characters after population: {missingCharacters}");
        }
    }

    private static string ComputeHash(string value)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(value));
            return BitConverter.ToString(hash).Replace("-", string.Empty);
        }
    }
}
#endif
