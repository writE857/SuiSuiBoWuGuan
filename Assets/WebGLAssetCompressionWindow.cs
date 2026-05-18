using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class WebGLAssetCompressionWindow : EditorWindow
{
    private enum AudioApplyStatus
    {
        Changed,
        SkippedNoOverride,
        NoChange,
        Failed
    }

    private const string WebGLPlatform = "WebGL";
    private const float TwoColumnLayoutMinWidth = 920f;
    private const float SectionSpacing = 8f;

    private static readonly int[] MaxSizeOptions = { 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192 };
    private static readonly string[] PresetNames = { "高压缩", "均衡", "高质量" };
    private static readonly FieldInfo AudioSampleRateOverrideField =
        typeof(AudioImporterSampleSettings).GetField("sampleRateOverride");
    private static readonly PropertyInfo AudioSampleRateOverrideProperty =
        typeof(AudioImporterSampleSettings).GetProperty("sampleRateOverride");

    private DefaultAsset rootFolder;
    private Vector2 scrollPos;

    private int presetIndex = 1;

    private int textureMaxSize = 1024;
    private int textureQuality = 50;
    private bool disableTextureMipmap = true;
    private bool preserveTextureAspect = true;
    private bool useCrunchedCompression = true;
    private bool skipSpriteTextures = true;

    private int audioQualityPercent = 60;
    private AudioCompressionFormat audioCompressionFormat = AudioCompressionFormat.Vorbis;
    private AudioClipLoadType audioLoadType = AudioClipLoadType.CompressedInMemory;
    private AudioSampleRateSetting audioSampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
    private int audioSampleRateOverride = 44100;

    private bool runTextureOps = true;
    private bool texOpWebGLOverride = true;
    private bool texOpMaxSize = true;
    private bool texOpQuality = true;
    private bool texOpCompression = true;
    private bool texOpDisableMipmap = true;
    private bool texOpPreserveAspect = true;
    private bool texAutoCreateOverrideWhenNeeded = true;

    private bool runAudioOps = true;
    private bool audioOpWebGLOverride = true;
    private bool audioOpQuality = true;
    private bool audioOpCompressionFormat = true;
    private bool audioOpLoadType = true;
    private bool audioOpSampleRate = true;
    private bool audioAutoCreateOverrideWhenNeeded = true;

    private bool showTextureAdvanced;
    private bool showAudioAdvanced;
    private bool showCancelSection = true;

    private bool cancelTextureMipmap = true;
    private bool clearTextureWebGLOverride;
    private bool clearAudioWebGLOverride;

    [MenuItem("Tools/WebGL/Asset Compression Tool")]
    private static void Open()
    {
        var window = GetWindow<WebGLAssetCompressionWindow>("WebGL资源压缩");
        window.minSize = new Vector2(560f, 520f);
        window.Show();
    }

    [MenuItem("Tools/WebGL/Apply Aggressive Compression (All Assets)")]
    private static void ApplyAggressiveCompressionMenu()
    {
        ApplyAggressiveCompressionToFolder("Assets");
    }

    [MenuItem("Tools/WebGL/Apply Aggressive Audio Compression (All Assets)")]
    private static void ApplyAggressiveAudioCompressionMenu()
    {
        ApplyAggressiveAudioCompressionToFolder("Assets");
    }

    [MenuItem("Tools/WebGL/Apply Aggressive Compression (Build Dependencies)")]
    private static void ApplyAggressiveCompressionBuildDependenciesMenu()
    {
        ApplyAggressiveCompressionToEnabledBuildDependencies();
    }

    [MenuItem("Tools/WebGL/Apply Aggressive Audio Compression (Build Dependencies)")]
    private static void ApplyAggressiveAudioCompressionBuildDependenciesMenu()
    {
        ApplyAggressiveAudioCompressionToEnabledBuildDependencies();
    }

    [MenuItem("Tools/WebGL/Restore Sprite WebGL Texture Overrides")]
    private static void RestoreSpriteTextureOverridesMenu()
    {
        RestoreSpriteTextureWebGLOverrides("Assets");
    }


    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        DrawPrimarySections();
        EditorGUILayout.Space(SectionSpacing);
        DrawScopeSection();
        EditorGUILayout.Space(6f);
        DrawPresetSection();
        EditorGUILayout.Space(SectionSpacing);
        DrawBatchSection();
        EditorGUILayout.Space(SectionSpacing);
        DrawCancelSection();

        EditorGUILayout.EndScrollView();
    }

    private void DrawPrimarySections()
    {
        float availableWidth = Mathf.Max(0f, position.width - 24f);
        bool useTwoColumns = availableWidth >= TwoColumnLayoutMinWidth;

        if (!useTwoColumns)
        {
            DrawTextureSection();
            EditorGUILayout.Space(6f);
            DrawAudioSection();
            return;
        }

        float columnWidth = (availableWidth - 6f) * 0.5f;
        EditorGUILayout.BeginHorizontal();
        GUILayout.BeginVertical(GUILayout.Width(columnWidth));
        DrawTextureSection();
        GUILayout.EndVertical();

        GUILayout.BeginVertical(GUILayout.Width(columnWidth));
        DrawAudioSection();
        GUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawScopeSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("处理范围", EditorStyles.boldLabel);
        rootFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "目标文件夹（可选）",
            rootFolder,
            typeof(DefaultAsset),
            false);

        EditorGUILayout.HelpBox(
            "不选文件夹时，会处理 Assets/ 下全部资源。",
            MessageType.Info);
        EditorGUILayout.EndVertical();
    }

    private void DrawPresetSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("快速预设", EditorStyles.boldLabel);

        int newPresetIndex = GUILayout.Toolbar(presetIndex, PresetNames);
        if (newPresetIndex != presetIndex)
        {
            presetIndex = newPresetIndex;
            ApplyPreset(presetIndex);
        }

        EditorGUILayout.HelpBox(
            "预设只改参数值，实际执行仍以你勾选的执行项为准。",
            MessageType.None);
        EditorGUILayout.EndVertical();
    }

    private void DrawTextureSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("图片(Texture)", EditorStyles.boldLabel);

        runTextureOps = EditorGUILayout.ToggleLeft("启用图片处理", runTextureOps);
        using (new EditorGUI.DisabledScope(!runTextureOps))
        {
            textureMaxSize = DrawMaxSizePopup("Max Size", textureMaxSize);
            textureQuality = EditorGUILayout.IntSlider("图片质量", textureQuality, 0, 100);
            useCrunchedCompression = EditorGUILayout.Toggle("使用 Crunch 压缩", useCrunchedCompression);
            disableTextureMipmap = EditorGUILayout.Toggle("关闭 Mipmap", disableTextureMipmap);
            preserveTextureAspect = EditorGUILayout.Toggle("保持比例(NPOT=None)", preserveTextureAspect);
            skipSpriteTextures = EditorGUILayout.Toggle("跳过精灵图", skipSpriteTextures);

            showTextureAdvanced = EditorGUILayout.Foldout(showTextureAdvanced, "高级执行项（图片）", true);
            if (showTextureAdvanced)
            {
                EditorGUI.indentLevel++;
                texOpWebGLOverride = EditorGUILayout.Toggle("执行 WebGL Override", texOpWebGLOverride);
                texOpMaxSize = EditorGUILayout.Toggle("执行 Max Size", texOpMaxSize);
                texOpQuality = EditorGUILayout.Toggle("执行图片质量", texOpQuality);
                texOpCompression = EditorGUILayout.Toggle("执行压缩格式", texOpCompression);
                texOpDisableMipmap = EditorGUILayout.Toggle("执行 Mipmap 选项", texOpDisableMipmap);
                texOpPreserveAspect = EditorGUILayout.Toggle("执行 NPOT 选项", texOpPreserveAspect);
                DrawToggleShortcutButtons(
                    () =>
                    {
                        texOpWebGLOverride = true;
                        texOpMaxSize = true;
                        texOpQuality = true;
                        texOpCompression = true;
                        texOpDisableMipmap = true;
                        texOpPreserveAspect = true;
                    },
                    () =>
                    {
                        texOpWebGLOverride = false;
                        texOpMaxSize = false;
                        texOpQuality = false;
                        texOpCompression = false;
                        texOpDisableMipmap = false;
                        texOpPreserveAspect = false;
                    });
                EditorGUI.indentLevel--;
            }

            if (GUILayout.Button("仅执行图片", GUILayout.Height(24f)))
            {
                int changed = ApplyTextureSettings();
                Debug.Log("[WebGLAssetCompressionWindow] Texture apply done: " + changed);
            }
        }

        EditorGUILayout.HelpBox(
            skipSpriteTextures
                ? "提示：已启用跳过精灵图，会忽略 Sprite 类型或精灵图目录下的纹理。"
                : "提示：改 Max Size 后，NPOT=None 可避免非2次幂纹理自动缩放导致的变形。",
            MessageType.None);
        EditorGUILayout.EndVertical();
    }

    private void DrawAudioSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("音频(Audio)", EditorStyles.boldLabel);

        runAudioOps = EditorGUILayout.ToggleLeft("启用音频处理", runAudioOps);
        using (new EditorGUI.DisabledScope(!runAudioOps))
        {
            audioQualityPercent = EditorGUILayout.IntSlider("音频质量(0-100)", audioQualityPercent, 0, 100);
            audioCompressionFormat = (AudioCompressionFormat)EditorGUILayout.EnumPopup("压缩格式", audioCompressionFormat);
            audioLoadType = (AudioClipLoadType)EditorGUILayout.EnumPopup("加载方式", audioLoadType);
            audioSampleRateSetting = (AudioSampleRateSetting)EditorGUILayout.EnumPopup("采样率设置", audioSampleRateSetting);

            if (audioSampleRateSetting == AudioSampleRateSetting.OverrideSampleRate)
            {
                audioSampleRateOverride = EditorGUILayout.IntField("采样率", audioSampleRateOverride);
                audioSampleRateOverride = Mathf.Clamp(audioSampleRateOverride, 8000, 96000);
            }

            showAudioAdvanced = EditorGUILayout.Foldout(showAudioAdvanced, "高级执行项（音频）", true);
            if (showAudioAdvanced)
            {
                EditorGUI.indentLevel++;
                audioOpWebGLOverride = EditorGUILayout.Toggle("执行 WebGL Override", audioOpWebGLOverride);
                audioAutoCreateOverrideWhenNeeded = EditorGUILayout.Toggle("无 Override 时自动创建", audioAutoCreateOverrideWhenNeeded);
                audioOpQuality = EditorGUILayout.Toggle("执行音频质量", audioOpQuality);
                audioOpCompressionFormat = EditorGUILayout.Toggle("执行压缩格式", audioOpCompressionFormat);
                audioOpLoadType = EditorGUILayout.Toggle("执行加载方式", audioOpLoadType);
                audioOpSampleRate = EditorGUILayout.Toggle("执行采样率", audioOpSampleRate);
                DrawToggleShortcutButtons(
                    () =>
                    {
                        audioOpWebGLOverride = true;
                        audioOpQuality = true;
                        audioOpCompressionFormat = true;
                        audioOpLoadType = true;
                        audioOpSampleRate = true;
                    },
                    () =>
                    {
                        audioOpWebGLOverride = false;
                        audioOpQuality = false;
                        audioOpCompressionFormat = false;
                        audioOpLoadType = false;
                        audioOpSampleRate = false;
                    });
                EditorGUI.indentLevel--;
            }

            if (GUILayout.Button("仅执行音频", GUILayout.Height(24f)))
            {
                int changed = ApplyAudioSettings();
                Debug.Log("[WebGLAssetCompressionWindow] Audio apply done: " + changed);
            }

            if (GUILayout.Button("仅执行音频(当前选中)", GUILayout.Height(24f)))
            {
                int changed = ApplyAudioSettingsForSelection();
                Debug.Log("[WebGLAssetCompressionWindow] Audio apply for selection done: " + changed);
            }
        }

        EditorGUILayout.HelpBox(
            "音频质量按 0-100 输入，工具会自动换算成 Unity 底层的 0-1。",
            MessageType.None);
        EditorGUILayout.EndVertical();
    }

    private void DrawBatchSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("批量执行", EditorStyles.boldLabel);

        EditorGUILayout.LabelField(
            string.Format(
                "当前选择：图片={0}，音频={1}",
                runTextureOps ? "开" : "关",
                runAudioOps ? "开" : "关"));

        if (GUILayout.Button("执行已勾选项", GUILayout.Height(30f)))
        {
            ApplyAll();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawCancelSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        showCancelSection = EditorGUILayout.Foldout(showCancelSection, "一键取消参数", true);
        if (showCancelSection)
        {
            cancelTextureMipmap = EditorGUILayout.Toggle("关闭图片 Mipmap", cancelTextureMipmap);
            clearTextureWebGLOverride = EditorGUILayout.Toggle("清除图片 WebGL Override", clearTextureWebGLOverride);
            clearAudioWebGLOverride = EditorGUILayout.Toggle("清除音频 WebGL Override", clearAudioWebGLOverride);

            if (GUILayout.Button("执行取消操作", GUILayout.Height(28f)))
            {
                RunCancelOperation();
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawToggleShortcutButtons(Action onSelectAll, Action onClearAll)
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("全选", GUILayout.Width(100f)))
        {
            onSelectAll?.Invoke();
        }

        if (GUILayout.Button("清空", GUILayout.Width(100f)))
        {
            onClearAll?.Invoke();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void ApplyPreset(int index)
    {
        switch (index)
        {
            case 0:
                textureMaxSize = 512;
                textureQuality = 0;
                useCrunchedCompression = true;
                disableTextureMipmap = true;
                preserveTextureAspect = true;

                audioQualityPercent = 5;
                audioCompressionFormat = AudioCompressionFormat.Vorbis;
                audioLoadType = AudioClipLoadType.CompressedInMemory;
                audioSampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
                audioSampleRateOverride = 11025;
                break;
            case 2:
                textureMaxSize = 2048;
                textureQuality = 85;
                useCrunchedCompression = false;
                disableTextureMipmap = true;
                preserveTextureAspect = true;

                audioQualityPercent = 85;
                audioCompressionFormat = AudioCompressionFormat.Vorbis;
                audioLoadType = AudioClipLoadType.DecompressOnLoad;
                audioSampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                break;
            default:
                textureMaxSize = 1024;
                textureQuality = 50;
                useCrunchedCompression = true;
                disableTextureMipmap = true;
                preserveTextureAspect = true;

                audioQualityPercent = 60;
                audioCompressionFormat = AudioCompressionFormat.Vorbis;
                audioLoadType = AudioClipLoadType.CompressedInMemory;
                audioSampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                break;
        }
    }

    private void ApplyAll()
    {
        int textureChanged = runTextureOps ? ApplyTextureSettings() : 0;
        int audioChanged = runAudioOps ? ApplyAudioSettings() : 0;

        Debug.Log(
            string.Format(
                "[WebGLAssetCompressionWindow] Done. Textures changed: {0}, Audio changed: {1}",
                textureChanged,
                audioChanged));
    }

    private int ApplyTextureSettings()
    {
        if (!runTextureOps)
        {
            return 0;
        }

        if (!(texOpWebGLOverride || texOpMaxSize || texOpQuality || texOpCompression || texOpDisableMipmap || texOpPreserveAspect))
        {
            Debug.LogWarning("[WebGLAssetCompressionWindow] No texture operation is selected.");
            return 0;
        }

        string[] searchFolders = GetSearchFolders();
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", searchFolders);
        int changedCount = 0;
        int skippedNoOverride = 0;
        int maxSize = NormalizeMaxSize(textureMaxSize);

        try
        {
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                EditorUtility.DisplayProgressBar(
                    "Apply Texture WebGL Settings",
                    string.Format("{0}/{1} {2}", i + 1, guids.Length, path),
                    (float)i / Mathf.Max(1, guids.Length));

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                if (ShouldSkipTextureCompression(path, importer))
                {
                    continue;
                }

                bool changed = false;
                TextureImporterPlatformSettings platformSettings = importer.GetPlatformTextureSettings(WebGLPlatform);
                platformSettings.name = WebGLPlatform;
                bool hasOverride = platformSettings.overridden;

                bool needsPlatformSettings = texOpMaxSize || texOpQuality || texOpCompression;
                if (needsPlatformSettings && !texOpWebGLOverride && !hasOverride)
                {
                    skippedNoOverride++;
                    continue;
                }

                if (texOpWebGLOverride && !platformSettings.overridden)
                {
                    platformSettings.overridden = true;
                    changed = true;
                }

                if (texOpMaxSize && platformSettings.maxTextureSize != maxSize)
                {
                    platformSettings.maxTextureSize = maxSize;
                    changed = true;
                }

                int quality = Mathf.Clamp(textureQuality, 0, 100);
                if (texOpQuality && platformSettings.compressionQuality != quality)
                {
                    platformSettings.compressionQuality = quality;
                    changed = true;
                }

                if (texOpCompression && platformSettings.crunchedCompression != useCrunchedCompression)
                {
                    platformSettings.crunchedCompression = useCrunchedCompression;
                    changed = true;
                }

                if (texOpCompression && platformSettings.textureCompression != TextureImporterCompression.CompressedLQ)
                {
                    platformSettings.textureCompression = TextureImporterCompression.CompressedLQ;
                    changed = true;
                }

                TextureImporterFormat desiredFormat = TextureImporterFormat.Automatic;
#if !UNITY_2021_1_OR_NEWER
                if (texOpCompression && useCrunchedCompression)
                {
                    desiredFormat = importer.DoesSourceTextureHaveAlpha()
                        ? TextureImporterFormat.DXT5Crunched
                        : TextureImporterFormat.DXT1Crunched;
                }
#endif
                if (texOpCompression && platformSettings.format != desiredFormat)
                {
                    platformSettings.format = desiredFormat;
                    changed = true;
                }

                bool targetMipmapEnabled = !disableTextureMipmap;
                if (texOpDisableMipmap && importer.mipmapEnabled != targetMipmapEnabled)
                {
                    importer.mipmapEnabled = targetMipmapEnabled;
                    changed = true;
                }

                if (texOpPreserveAspect)
                {
                    TextureImporterNPOTScale targetNpotScale = preserveTextureAspect
                        ? TextureImporterNPOTScale.None
                        : TextureImporterNPOTScale.ToNearest;
                    if (importer.npotScale != targetNpotScale)
                    {
                        importer.npotScale = targetNpotScale;
                        changed = true;
                    }
                }

                if (!changed)
                {
                    continue;
                }

                importer.SetPlatformTextureSettings(platformSettings);
                importer.SaveAndReimport();
                changedCount++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        if (skippedNoOverride > 0)
        {
            Debug.LogWarning(
                string.Format(
                    "[WebGLAssetCompressionWindow] Skipped {0} textures because Apply WebGL Override is disabled and those assets currently have no WebGL override.",
                    skippedNoOverride));
        }

        return changedCount;
    }

    private int ApplyAudioSettings()
    {
        if (!runAudioOps)
        {
            return 0;
        }

        if (!(audioOpWebGLOverride || audioOpQuality || audioOpCompressionFormat || audioOpLoadType || audioOpSampleRate))
        {
            Debug.LogWarning("[WebGLAssetCompressionWindow] No audio operation is selected.");
            return 0;
        }

        string[] searchFolders = GetSearchFolders();
        string[] guids = AssetDatabase.FindAssets("t:AudioClip", searchFolders);
        int changedCount = 0;
        int skippedNoOverride = 0;
        int failedCount = 0;

        try
        {
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                EditorUtility.DisplayProgressBar(
                    "Apply Audio WebGL Settings",
                    string.Format("{0}/{1} {2}", i + 1, guids.Length, path),
                    (float)i / Mathf.Max(1, guids.Length));

                AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null)
                {
                    continue;
                }

                AudioApplyStatus status = ApplyAudioSettingsToImporter(path, importer);
                if (status == AudioApplyStatus.Changed)
                {
                    changedCount++;
                }
                else if (status == AudioApplyStatus.SkippedNoOverride)
                {
                    skippedNoOverride++;
                }
                else if (status == AudioApplyStatus.Failed)
                {
                    failedCount++;
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        if (skippedNoOverride > 0)
        {
            Debug.LogWarning(
                string.Format(
                    "[WebGLAssetCompressionWindow] Skipped {0} audio clips because Apply WebGL Override is disabled and those assets currently have no WebGL override.",
                    skippedNoOverride));
        }

        if (failedCount > 0)
        {
            Debug.LogWarning("[WebGLAssetCompressionWindow] Failed to apply audio settings count: " + failedCount);
        }

        return changedCount;
    }

    private int ApplyAudioSettingsForSelection()
    {
        if (!runAudioOps)
        {
            return 0;
        }

        int changedCount = 0;
        int skippedNoOverride = 0;
        int failedCount = 0;

        HashSet<string> audioAssetPaths = new HashSet<string>();
        string[] selectedGuids = Selection.assetGUIDs;
        for (int i = 0; i < selectedGuids.Length; i++)
        {
            string selectedPath = AssetDatabase.GUIDToAssetPath(selectedGuids[i]);
            if (string.IsNullOrEmpty(selectedPath))
            {
                continue;
            }

            if (AssetDatabase.IsValidFolder(selectedPath))
            {
                string[] nestedAudioGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { selectedPath });
                for (int n = 0; n < nestedAudioGuids.Length; n++)
                {
                    string nestedPath = AssetDatabase.GUIDToAssetPath(nestedAudioGuids[n]);
                    if (!string.IsNullOrEmpty(nestedPath))
                    {
                        audioAssetPaths.Add(nestedPath);
                    }
                }
                continue;
            }

            if (AssetImporter.GetAtPath(selectedPath) is AudioImporter)
            {
                audioAssetPaths.Add(selectedPath);
            }
        }

        if (audioAssetPaths.Count == 0 && Selection.activeObject != null)
        {
            string activePath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (!string.IsNullOrEmpty(activePath) && AssetImporter.GetAtPath(activePath) is AudioImporter)
            {
                audioAssetPaths.Add(activePath);
            }
        }

        if (audioAssetPaths.Count == 0)
        {
            Debug.LogWarning("[WebGLAssetCompressionWindow] No audio assets selected.");
            return 0;
        }

        foreach (string path in audioAssetPaths)
        {
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
            {
                continue;
            }

            AudioApplyStatus status = ApplyAudioSettingsToImporter(path, importer);
            if (status == AudioApplyStatus.Changed)
            {
                changedCount++;
            }
            else if (status == AudioApplyStatus.SkippedNoOverride)
            {
                skippedNoOverride++;
            }
            else if (status == AudioApplyStatus.Failed)
            {
                failedCount++;
            }
        }

        Debug.Log(
            string.Format(
                "[WebGLAssetCompressionWindow] Selection audio assets: {0}, changed: {1}, skipped: {2}, failed: {3}",
                audioAssetPaths.Count,
                changedCount,
                skippedNoOverride,
                failedCount));

        return changedCount;
    }

    private AudioApplyStatus ApplyAudioSettingsToImporter(string path, AudioImporter importer)
    {
        bool hasOverride = importer.ContainsSampleSettingsOverride(WebGLPlatform);
        bool needOverrideValues = audioOpQuality || audioOpCompressionFormat || audioOpLoadType || audioOpSampleRate;
        bool shouldCreateOverride = audioOpWebGLOverride || (audioAutoCreateOverrideWhenNeeded && needOverrideValues);

        if (needOverrideValues && !shouldCreateOverride && !hasOverride)
        {
            return AudioApplyStatus.SkippedNoOverride;
        }

        AudioImporterSampleSettings defaultSettings = importer.defaultSampleSettings;
        AudioImporterSampleSettings currentSettings = hasOverride
            ? importer.GetOverrideSampleSettings(WebGLPlatform)
            : defaultSettings;
        AudioImporterSampleSettings targetSettings = currentSettings;

        if (audioOpLoadType)
        {
            targetSettings.loadType = audioLoadType;
        }

        if (audioOpCompressionFormat)
        {
            targetSettings.compressionFormat = audioCompressionFormat;
        }

        if (audioOpSampleRate)
        {
            targetSettings.sampleRateSetting = audioSampleRateSetting;
        }

        if (audioOpQuality)
        {
            targetSettings.quality = Mathf.Clamp01(audioQualityPercent / 100f);
        }

        if (audioOpSampleRate && audioSampleRateSetting == AudioSampleRateSetting.OverrideSampleRate)
        {
            SetSampleRateOverride(ref targetSettings, audioSampleRateOverride);
        }

        bool settingsChanged = !AreAudioSettingsEqual(currentSettings, targetSettings);
        float targetQuality = Mathf.Clamp01(audioQualityPercent / 100f);
        bool sharedQualityChanged = audioOpQuality && Mathf.Abs(defaultSettings.quality - targetQuality) >= 0.0001f;
        bool mustCreateOverride = !hasOverride && shouldCreateOverride;
        bool forceWriteCurrent = audioOpWebGLOverride && hasOverride;
        if (!settingsChanged && !mustCreateOverride && !forceWriteCurrent && !sharedQualityChanged)
        {
            return AudioApplyStatus.NoChange;
        }

        // Prefer writing meta directly for all override writes.
        // Unity API may silently keep old values in some project/package states.
        if (TryWriteAudioOverrideViaMeta(path, targetSettings, audioOpQuality ? (float?)targetQuality : null))
        {
            AudioImporter metaReloaded = AssetImporter.GetAtPath(path) as AudioImporter;
            if (metaReloaded != null && metaReloaded.ContainsSampleSettingsOverride(WebGLPlatform))
            {
                AudioImporterSampleSettings appliedSettings = metaReloaded.GetOverrideSampleSettings(WebGLPlatform);
                if (AreAudioSettingsEqual(appliedSettings, targetSettings))
                {
                    return AudioApplyStatus.Changed;
                }
            }
        }

        if (sharedQualityChanged)
        {
            AudioImporterSampleSettings editedDefault = defaultSettings;
            editedDefault.quality = targetQuality;
            importer.defaultSampleSettings = editedDefault;
        }

        bool setOk = importer.SetOverrideSampleSettings(WebGLPlatform, targetSettings);
        importer.SaveAndReimport();
        if (!setOk)
        {
            Debug.LogWarning("[WebGLAssetCompressionWindow] SetOverrideSampleSettings returned false: " + path);
            return AudioApplyStatus.Failed;
        }

        AudioImporter apiReloaded = AssetImporter.GetAtPath(path) as AudioImporter;
        if (apiReloaded != null && apiReloaded.ContainsSampleSettingsOverride(WebGLPlatform))
        {
            AudioImporterSampleSettings appliedByApi = apiReloaded.GetOverrideSampleSettings(WebGLPlatform);
            if (!AreAudioSettingsEqual(appliedByApi, targetSettings))
            {
                Debug.LogWarning(
                    string.Format(
                        "[WebGLAssetCompressionWindow] API apply mismatch: {0} | target(format={1}, load={2}, quality={3:0.###}) actual(format={4}, load={5}, quality={6:0.###})",
                        path,
                        targetSettings.compressionFormat,
                        targetSettings.loadType,
                        targetSettings.quality,
                        appliedByApi.compressionFormat,
                        appliedByApi.loadType,
                        appliedByApi.quality));
            }
        }

        return AudioApplyStatus.Changed;
    }

    private static bool TryWriteAudioOverrideViaMeta(string assetPath, AudioImporterSampleSettings settings, float? sharedQuality)
    {
        string metaPath = assetPath + ".meta";
        if (!File.Exists(metaPath))
        {
            return false;
        }

        try
        {
            string[] lines = File.ReadAllLines(metaPath);
            if (lines.Length == 0)
            {
                return false;
            }
            string originalFileText = string.Join("\n", lines);

            int sampleRate = GetSampleRateOverride(settings);
            string qualityText = settings.quality.ToString("0.###", CultureInfo.InvariantCulture);
            string[] webglBlock =
            {
                "    13:",
                "      loadType: " + (int)settings.loadType,
                "      sampleRateSetting: " + (int)settings.sampleRateSetting,
                "      sampleRateOverride: " + sampleRate,
                "      compressionFormat: " + (int)settings.compressionFormat,
                "      quality: " + qualityText,
                "      conversionMode: 0"
            };

            int platformStart = -1;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("  platformSettingOverrides:"))
                {
                    platformStart = i;
                    break;
                }
            }

            if (platformStart < 0)
            {
                return false;
            }

            if (lines[platformStart].Contains("{}"))
            {
                lines[platformStart] = "  platformSettingOverrides:";
            }

            int platformEnd = lines.Length;
            for (int i = platformStart + 1; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("  ") && !lines[i].StartsWith("    "))
                {
                    platformEnd = i;
                    break;
                }
            }

            List<string> output = new List<string>(lines);

            int webglStart = -1;
            int webglEnd = -1;
            for (int i = platformStart + 1; i < platformEnd; i++)
            {
                if (output[i].StartsWith("    13:"))
                {
                    webglStart = i;
                    webglEnd = i + 1;
                    while (webglEnd < platformEnd && output[webglEnd].StartsWith("      "))
                    {
                        webglEnd++;
                    }

                    break;
                }
            }

            if (webglStart >= 0)
            {
                output.RemoveRange(webglStart, webglEnd - webglStart);
                output.InsertRange(webglStart, webglBlock);
            }
            else
            {
                output.InsertRange(platformEnd, webglBlock);
            }

            if (sharedQuality.HasValue)
            {
                string qualityTextInDefault = sharedQuality.Value.ToString("0.###", CultureInfo.InvariantCulture);
                int defaultStart = -1;
                for (int i = 0; i < output.Count; i++)
                {
                    if (output[i].StartsWith("  defaultSettings:"))
                    {
                        defaultStart = i;
                        break;
                    }
                }

                if (defaultStart >= 0)
                {
                    int defaultEnd = output.Count;
                    for (int i = defaultStart + 1; i < output.Count; i++)
                    {
                        if (output[i].StartsWith("  ") && !output[i].StartsWith("    "))
                        {
                            defaultEnd = i;
                            break;
                        }
                    }

                    bool replaced = false;
                    for (int i = defaultStart + 1; i < defaultEnd; i++)
                    {
                        if (output[i].TrimStart().StartsWith("quality:"))
                        {
                            output[i] = "    quality: " + qualityTextInDefault;
                            replaced = true;
                            break;
                        }
                    }

                    if (!replaced)
                    {
                        output.Insert(defaultEnd, "    quality: " + qualityTextInDefault);
                    }
                }
            }

            string updatedText = string.Join("\n", output);
            if (originalFileText == updatedText)
            {
                return true;
            }

            File.WriteAllText(metaPath, updatedText + "\n");
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[WebGLAssetCompressionWindow] Meta fallback exception: " + e.Message);
            return false;
        }
    }

    private static bool AreAudioSettingsEqual(AudioImporterSampleSettings a, AudioImporterSampleSettings b)
    {
        return a.loadType == b.loadType &&
               a.sampleRateSetting == b.sampleRateSetting &&
               a.compressionFormat == b.compressionFormat &&
               Mathf.Abs(a.quality - b.quality) < 0.0001f &&
               GetSampleRateOverride(a) == GetSampleRateOverride(b);
    }

    private void RunCancelOperation()
    {
        string[] searchFolders = GetSearchFolders();
        int textureChanged = 0;
        int audioChanged = 0;

        string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", searchFolders);
        try
        {
            for (int i = 0; i < textureGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(textureGuids[i]);
                EditorUtility.DisplayProgressBar(
                    "Cancel Texture Params",
                    string.Format("{0}/{1} {2}", i + 1, textureGuids.Length, path),
                    (float)i / Mathf.Max(1, textureGuids.Length));

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                bool changed = false;

                if (cancelTextureMipmap && importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    changed = true;
                }

                if (clearTextureWebGLOverride)
                {
                    TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(WebGLPlatform);
                    if (settings.overridden)
                    {
                        importer.ClearPlatformTextureSettings(WebGLPlatform);
                        changed = true;
                    }
                }

                if (!changed)
                {
                    continue;
                }

                importer.SaveAndReimport();
                textureChanged++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        if (clearAudioWebGLOverride)
        {
            string[] audioGuids = AssetDatabase.FindAssets("t:AudioClip", searchFolders);
            try
            {
                for (int i = 0; i < audioGuids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(audioGuids[i]);
                    EditorUtility.DisplayProgressBar(
                        "Cancel Audio Params",
                        string.Format("{0}/{1} {2}", i + 1, audioGuids.Length, path),
                        (float)i / Mathf.Max(1, audioGuids.Length));

                    AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
                    if (importer == null)
                    {
                        continue;
                    }

                    if (!importer.ClearSampleSettingOverride(WebGLPlatform))
                    {
                        continue;
                    }

                    importer.SaveAndReimport();
                    audioChanged++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        Debug.Log(
            string.Format(
                "[WebGLAssetCompressionWindow] Cancel done. Textures changed: {0}, Audio changed: {1}",
                textureChanged,
                audioChanged));
    }

    private string[] GetSearchFolders()
    {
        if (rootFolder == null)
        {
            return new[] { "Assets" };
        }

        string path = AssetDatabase.GetAssetPath(rootFolder);
        if (string.IsNullOrWhiteSpace(path) || !AssetDatabase.IsValidFolder(path))
        {
            return new[] { "Assets" };
        }

        return new[] { path };
    }

    private bool ShouldSkipTextureCompression(string path, TextureImporter importer)
    {
        return ShouldSkipTextureCompression(path, importer, skipSpriteTextures);
    }

    private static bool ShouldSkipTextureCompression(string path, TextureImporter importer, bool skipSprites)
    {
        if (string.IsNullOrWhiteSpace(path) || importer == null)
        {
            return true;
        }

        if (skipSprites && IsSpriteTexture(path, importer))
        {
            return true;
        }

        return false;
    }

    public static void ApplyAggressiveCompressionToFolder(string folderPath)
    {
        var window = CreateInstance<WebGLAssetCompressionWindow>();
        try
        {
            if (!string.IsNullOrWhiteSpace(folderPath))
            {
                window.rootFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(folderPath);
            }

            window.ApplyPreset(0);
            window.runTextureOps = true;
            window.runAudioOps = true;
            window.texOpWebGLOverride = true;
            window.audioOpWebGLOverride = true;
            window.texAutoCreateOverrideWhenNeeded = true;
            window.audioAutoCreateOverrideWhenNeeded = true;
            window.ApplyAll();
        }
        finally
        {
            DestroyImmediate(window);
        }
    }

    public static void ApplyAggressiveAudioCompressionToFolder(string folderPath)
    {
        var window = CreateInstance<WebGLAssetCompressionWindow>();
        try
        {
            if (!string.IsNullOrWhiteSpace(folderPath))
            {
                window.rootFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(folderPath);
            }

            window.ApplyPreset(0);
            window.runTextureOps = false;
            window.runAudioOps = true;
            window.audioOpWebGLOverride = true;
            window.audioAutoCreateOverrideWhenNeeded = true;
            window.ApplyAll();
        }
        finally
        {
            DestroyImmediate(window);
        }
    }

    public static void ApplyAggressiveCompressionToEnabledBuildDependencies()
    {
        ApplyAggressiveCompressionToAssetPaths(GetEnabledBuildDependencyPaths(), includeTextures: true, includeAudio: true);
    }

    public static void ApplyAggressiveAudioCompressionToEnabledBuildDependencies()
    {
        ApplyAggressiveCompressionToAssetPaths(GetEnabledBuildDependencyPaths(), includeTextures: false, includeAudio: true);
    }

    private static void ApplyAggressiveCompressionToAssetPaths(IEnumerable<string> assetPaths, bool includeTextures, bool includeAudio)
    {
        var window = CreateInstance<WebGLAssetCompressionWindow>();
        try
        {
            window.ApplyPreset(0);
            window.runTextureOps = includeTextures;
            window.runAudioOps = includeAudio;
            window.texOpWebGLOverride = true;
            window.audioOpWebGLOverride = true;
            window.texAutoCreateOverrideWhenNeeded = true;
            window.audioAutoCreateOverrideWhenNeeded = true;

            HashSet<string> uniquePaths = new HashSet<string>(assetPaths ?? Array.Empty<string>());
            int textureChanged = includeTextures ? window.ApplyTextureSettings(uniquePaths) : 0;
            int audioChanged = includeAudio ? window.ApplyAudioSettings(uniquePaths) : 0;

            Debug.Log(
                string.Format(
                    "[WebGLAssetCompressionWindow] Build dependencies apply done. Textures changed: {0}, Audio changed: {1}",
                    textureChanged,
                    audioChanged));
        }
        finally
        {
            DestroyImmediate(window);
        }
    }

    private static string[] GetEnabledBuildDependencyPaths()
    {
        List<string> enabledScenePaths = new List<string>();
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        for (int i = 0; i < scenes.Length; i++)
        {
            EditorBuildSettingsScene scene = scenes[i];
            if (scene != null && scene.enabled && !string.IsNullOrWhiteSpace(scene.path))
            {
                enabledScenePaths.Add(scene.path);
            }
        }

        if (enabledScenePaths.Count == 0)
        {
            Debug.LogWarning("[WebGLAssetCompressionWindow] No enabled build scenes found.");
            return Array.Empty<string>();
        }

        return AssetDatabase.GetDependencies(enabledScenePaths.ToArray(), true);
    }

    private int ApplyTextureSettings(IEnumerable<string> assetPaths)
    {
        if (!runTextureOps)
        {
            return 0;
        }

        HashSet<string> candidatePaths = CollectImporterPaths<TextureImporter>(assetPaths);
        int changedCount = 0;
        int maxSize = NormalizeMaxSize(textureMaxSize);
        string[] paths = new List<string>(candidatePaths).ToArray();

        try
        {
            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i];
                EditorUtility.DisplayProgressBar(
                    "Apply Texture WebGL Settings",
                    string.Format("{0}/{1} {2}", i + 1, paths.Length, path),
                    (float)i / Mathf.Max(1, paths.Length));

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null || ShouldSkipTextureCompression(path, importer))
                {
                    continue;
                }

                bool changed = false;
                TextureImporterPlatformSettings platformSettings = importer.GetPlatformTextureSettings(WebGLPlatform);
                platformSettings.name = WebGLPlatform;

                if (texOpWebGLOverride && !platformSettings.overridden)
                {
                    platformSettings.overridden = true;
                    changed = true;
                }

                if (texOpMaxSize && platformSettings.maxTextureSize != maxSize)
                {
                    platformSettings.maxTextureSize = maxSize;
                    changed = true;
                }

                int quality = Mathf.Clamp(textureQuality, 0, 100);
                if (texOpQuality && platformSettings.compressionQuality != quality)
                {
                    platformSettings.compressionQuality = quality;
                    changed = true;
                }

                if (texOpCompression && platformSettings.crunchedCompression != useCrunchedCompression)
                {
                    platformSettings.crunchedCompression = useCrunchedCompression;
                    changed = true;
                }

                if (texOpCompression && platformSettings.textureCompression != TextureImporterCompression.CompressedLQ)
                {
                    platformSettings.textureCompression = TextureImporterCompression.CompressedLQ;
                    changed = true;
                }

                TextureImporterFormat desiredFormat = TextureImporterFormat.Automatic;
#if !UNITY_2021_1_OR_NEWER
                if (texOpCompression && useCrunchedCompression)
                {
                    desiredFormat = importer.DoesSourceTextureHaveAlpha()
                        ? TextureImporterFormat.DXT5Crunched
                        : TextureImporterFormat.DXT1Crunched;
                }
#endif
                if (texOpCompression && platformSettings.format != desiredFormat)
                {
                    platformSettings.format = desiredFormat;
                    changed = true;
                }

                bool targetMipmapEnabled = !disableTextureMipmap;
                if (texOpDisableMipmap && importer.mipmapEnabled != targetMipmapEnabled)
                {
                    importer.mipmapEnabled = targetMipmapEnabled;
                    changed = true;
                }

                if (texOpPreserveAspect)
                {
                    TextureImporterNPOTScale targetNpotScale = preserveTextureAspect
                        ? TextureImporterNPOTScale.None
                        : TextureImporterNPOTScale.ToNearest;
                    if (importer.npotScale != targetNpotScale)
                    {
                        importer.npotScale = targetNpotScale;
                        changed = true;
                    }
                }

                if (!changed)
                {
                    continue;
                }

                importer.SetPlatformTextureSettings(platformSettings);
                importer.SaveAndReimport();
                changedCount++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        return changedCount;
    }

    private int ApplyAudioSettings(IEnumerable<string> assetPaths)
    {
        if (!runAudioOps)
        {
            return 0;
        }

        if (!(audioOpWebGLOverride || audioOpQuality || audioOpCompressionFormat || audioOpLoadType || audioOpSampleRate))
        {
            Debug.LogWarning("[WebGLAssetCompressionWindow] No audio operation is selected.");
            return 0;
        }

        HashSet<string> audioAssetPaths = CollectImporterPaths<AudioImporter>(assetPaths);
        int changedCount = 0;
        int failedCount = 0;
        string[] paths = new List<string>(audioAssetPaths).ToArray();

        try
        {
            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i];
                EditorUtility.DisplayProgressBar(
                    "Apply Audio WebGL Settings",
                    string.Format("{0}/{1} {2}", i + 1, paths.Length, path),
                    (float)i / Mathf.Max(1, paths.Length));

                AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null)
                {
                    continue;
                }

                AudioApplyStatus status = ApplyAudioSettingsToImporter(path, importer);
                if (status == AudioApplyStatus.Changed)
                {
                    changedCount++;
                }
                else if (status == AudioApplyStatus.Failed)
                {
                    failedCount++;
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        if (failedCount > 0)
        {
            Debug.LogWarning(string.Format("[WebGLAssetCompressionWindow] Failed to apply audio settings: {0}", failedCount));
        }

        return changedCount;
    }

    private static HashSet<string> CollectImporterPaths<TImporter>(IEnumerable<string> assetPaths)
        where TImporter : AssetImporter
    {
        HashSet<string> result = new HashSet<string>();
        if (assetPaths == null)
        {
            return result;
        }

        foreach (string path in assetPaths)
        {
            if (string.IsNullOrWhiteSpace(path) || AssetDatabase.IsValidFolder(path))
            {
                continue;
            }

            if (AssetImporter.GetAtPath(path) is TImporter)
            {
                result.Add(path);
            }
        }

        return result;
    }

    public static void RestoreSpriteTextureWebGLOverrides(string folderPath)
    {
        string[] searchFolders = string.IsNullOrWhiteSpace(folderPath) ? new[] { "Assets" } : new[] { folderPath };
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", searchFolders);
        int changedCount = 0;

        try
        {
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                EditorUtility.DisplayProgressBar(
                    "Restore Sprite WebGL Overrides",
                    string.Format("{0}/{1} {2}", i + 1, guids.Length, path),
                    (float)i / Mathf.Max(1, guids.Length));

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                if (!IsSpriteTexture(path, importer))
                {
                    continue;
                }

                TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(WebGLPlatform);
                if (!settings.overridden)
                {
                    continue;
                }

                importer.ClearPlatformTextureSettings(WebGLPlatform);
                importer.SaveAndReimport();
                changedCount++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        Debug.Log(string.Format("[WebGLAssetCompressionWindow] Restored sprite WebGL overrides: {0}", changedCount));
    }

    private static bool IsSpriteTexture(string path, TextureImporter importer)
    {
        if (string.IsNullOrWhiteSpace(path) || importer == null)
        {
            return false;
        }

        string normalizedPath = path.Replace('\\', '/');
        if (normalizedPath.StartsWith("Assets/Sprite/") || normalizedPath.StartsWith("Assets/_Sprites/"))
        {
            return true;
        }

        return importer.textureType == TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.None;
    }

    private static int DrawMaxSizePopup(string label, int value)
    {
        int current = NormalizeMaxSize(value);
        int index = FindIndexInOptions(current);
        index = EditorGUILayout.Popup(label, index, BuildMaxSizeOptionLabels());
        current = MaxSizeOptions[Mathf.Clamp(index, 0, MaxSizeOptions.Length - 1)];
        return current;
    }

    private static int NormalizeMaxSize(int value)
    {
        int clamped = Mathf.Clamp(value, MaxSizeOptions[0], MaxSizeOptions[MaxSizeOptions.Length - 1]);
        int nearest = MaxSizeOptions[0];
        int bestDistance = Mathf.Abs(clamped - nearest);

        for (int i = 1; i < MaxSizeOptions.Length; i++)
        {
            int candidate = MaxSizeOptions[i];
            int distance = Mathf.Abs(clamped - candidate);
            if (distance < bestDistance)
            {
                nearest = candidate;
                bestDistance = distance;
            }
        }

        return nearest;
    }

    private static int FindIndexInOptions(int maxSize)
    {
        for (int i = 0; i < MaxSizeOptions.Length; i++)
        {
            if (MaxSizeOptions[i] == maxSize)
            {
                return i;
            }
        }

        return 0;
    }

    private static string[] BuildMaxSizeOptionLabels()
    {
        string[] labels = new string[MaxSizeOptions.Length];
        for (int i = 0; i < MaxSizeOptions.Length; i++)
        {
            labels[i] = MaxSizeOptions[i].ToString();
        }

        return labels;
    }

    private static void SetSampleRateOverride(ref AudioImporterSampleSettings settings, int sampleRate)
    {
        int clamped = Mathf.Clamp(sampleRate, 8000, 96000);
        object boxed = settings;

        if (AudioSampleRateOverrideField != null)
        {
            if (AudioSampleRateOverrideField.FieldType == typeof(uint))
            {
                AudioSampleRateOverrideField.SetValue(boxed, (uint)clamped);
            }
            else
            {
                AudioSampleRateOverrideField.SetValue(boxed, clamped);
            }

            settings = (AudioImporterSampleSettings)boxed;
            return;
        }

        if (AudioSampleRateOverrideProperty != null && AudioSampleRateOverrideProperty.CanWrite)
        {
            if (AudioSampleRateOverrideProperty.PropertyType == typeof(uint))
            {
                AudioSampleRateOverrideProperty.SetValue(boxed, (uint)clamped, null);
            }
            else
            {
                AudioSampleRateOverrideProperty.SetValue(boxed, clamped, null);
            }

            settings = (AudioImporterSampleSettings)boxed;
        }
    }

    private static int GetSampleRateOverride(AudioImporterSampleSettings settings)
    {
        object boxed = settings;

        if (AudioSampleRateOverrideField != null)
        {
            object value = AudioSampleRateOverrideField.GetValue(boxed);
            if (value == null)
            {
                return 0;
            }

            if (value is uint uintValue)
            {
                return unchecked((int)uintValue);
            }

            return Convert.ToInt32(value);
        }

        if (AudioSampleRateOverrideProperty != null && AudioSampleRateOverrideProperty.CanRead)
        {
            object value = AudioSampleRateOverrideProperty.GetValue(boxed, null);
            if (value == null)
            {
                return 0;
            }

            if (value is uint uintValue)
            {
                return unchecked((int)uintValue);
            }

            return Convert.ToInt32(value);
        }

        return 0;
    }
}
