using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Fix.Editor
{
    public class AnimConvertEuler : FixEditorWindow
    {
        private const string TipName = "拖入需要修改的动画";
        private const string Title = "修改动画";
        private static readonly Type AssetType = typeof(AnimationClip);
        private static readonly string AssetTypeName = AssetType.Name;

        [FixEditor(FixRoot + nameof(AnimationClip) + "/" + Title)]
        private static void ShowWindow()
        {
            GetWindowWithRect<AnimConvertEuler>(new Rect(Screen.width / 2, Screen.height / 2, 500, 500)).titleContent =
                new GUIContent(Title);
        }


        private readonly ISet<AnimationClip> assets = new HashSet<AnimationClip>();
        private bool convertRotate2Quaternion = true;
        private bool normalizeQuaternions = true;

        private Vector2 pos;

        private void Awake() => assets.Clear();

        private void OnDestroy() => assets.Clear();

        private void OnGUI()
        {
            HorizontalRegion(() =>
            {
                if (!GUILayout.Button(new GUIContent($"扫描全部{AssetTypeName}", "默认不扫描导出文件夹"))) return;
                assets.Clear();
                var paths = AssetDatabase
                    .FindAssets($"t:{AssetTypeName}", new string[] {"Assets"})
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .ToArray();
                int c = 0, total = paths.Length;
                try
                {
                    foreach (var path in paths)
                    {
                        EditorUtility.DisplayProgressBar($"加载{AssetTypeName}", path, (float) c++ / total);
                        assets.Add(AssetDatabase.LoadAssetAtPath<AnimationClip>(path));
                    }
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }
            });
            //! 实现拖拽
            DragRegion(path =>
            {
                assets.AddRange(GetTotalFiles(path)
                    .Distinct()
                    .Where(e => ".anim".Equals(Path.GetExtension(e), StringComparison.InvariantCultureIgnoreCase))
                    .Select(AssetDatabase.LoadAssetAtPath<AnimationClip>));
            }, TipName, GUILayout.MinHeight(64), GUILayout.MinWidth(512));

            Space(10);
            HorizontalRegion(() => { Toggle(ref convertRotate2Quaternion, "将旋转改为标准四元数"); });
            HorizontalRegion(() => { Toggle(ref normalizeQuaternions, "规范化四元数"); });
            Space(10);
            HorizontalRegion(() =>
                ColorRegion(new Color(0, 1, 072f, 0.5f), () =>
                    GUILayout.Label($"选择了 {assets.Count} 个文件", new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleLeft
                    })));
            Space(20);
            HorizontalRegion(() =>
            {
                if (GUILayout.Button("清除", GUILayout.Width(256), GUILayout.Height(40))) Clear();
                if (GUILayout.Button("执行", GUILayout.Width(256), GUILayout.Height(40))) Execute();
            });
        }

        private void Clear() => assets.Clear();

        private void Execute()
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                int c = 0, total = assets.Count;
                foreach (var asset in assets)
                {
                    if (EditorUtility.DisplayCancelableProgressBar(
                        "开始处理",
                        $"{c + 1}/{total}",
                        (float) c / total))
                        break;
                    c++;
                    ConvertOne(asset);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private void ConvertOne(AnimationClip targetClip)
        {
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(targetClip);
            // 收集所有欧拉角曲线
            Dictionary<string, EulerCurves> eulerCurvesMap = new Dictionary<string, EulerCurves>();

            foreach (var binding in bindings)
            {
                if (!binding.propertyName.Contains("localEulerAngles")) continue;

                string basePath = binding.path;

                if (!eulerCurvesMap.TryGetValue(basePath, out var eulerCurves))
                    eulerCurvesMap.Add(basePath, eulerCurves = new EulerCurves());

                var curve = AnimationUtility.GetEditorCurve(targetClip, binding);

                switch (binding.propertyName)
                {
                    case "localEulerAnglesRaw.x":
                    case "localEulerAngles.x":
                        eulerCurves.x = curve;
                        eulerCurves.xProperty = binding.propertyName;
                        break;
                    case "localEulerAnglesRaw.y":
                    case "localEulerAngles.y":
                        eulerCurves.y = curve;
                        eulerCurves.yProperty = binding.propertyName;
                        break;
                    case "localEulerAnglesRaw.z":
                    case "localEulerAngles.z":
                        eulerCurves.z = curve;
                        eulerCurves.zProperty = binding.propertyName;
                        break;
                }
            }

            // 转换为四元数
            foreach (var kvp in eulerCurvesMap)
            {
                string path = kvp.Key;
                EulerCurves eulerCurves = kvp.Value;

                if (!eulerCurves.IsValid()) continue;
                // 创建四元数曲线
                var quaternionCurves =
                    ConvertEulerCurvesToQuaternion(eulerCurves, normalizeQuaternions);
                // 删除原欧拉角曲线
                AnimationUtility.SetEditorCurve(targetClip,
                    new EditorCurveBinding
                        {path = path, propertyName = eulerCurves.xProperty, type = typeof(Transform)}, null);
                AnimationUtility.SetEditorCurve(targetClip,
                    new EditorCurveBinding
                        {path = path, propertyName = eulerCurves.yProperty, type = typeof(Transform)}, null);
                AnimationUtility.SetEditorCurve(targetClip,
                    new EditorCurveBinding
                        {path = path, propertyName = eulerCurves.zProperty, type = typeof(Transform)}, null);

                // 添加四元数曲线
                foreach (var pair in quaternionCurves)
                {
                    AnimationUtility.SetEditorCurve(targetClip,
                        new EditorCurveBinding
                        {
                            path = path,
                            propertyName = pair.Key,
                            type = typeof(Transform)
                        },
                        pair.Value);
                }
            }

            EditorUtility.SetDirty(targetClip);
        }

        private Dictionary<string, AnimationCurve> ConvertEulerCurvesToQuaternion(
            EulerCurves eulerCurves, bool normalize)
        {
            Dictionary<string, AnimationCurve> quaternionCurves = new Dictionary<string, AnimationCurve>();

            AnimationCurve qxCurve = new AnimationCurve();
            AnimationCurve qyCurve = new AnimationCurve();
            AnimationCurve qzCurve = new AnimationCurve();
            AnimationCurve qwCurve = new AnimationCurve();

            // 收集所有关键帧时间点
            HashSet<float> keyframeTimes = new HashSet<float>();
            AddKeyframeTimes(eulerCurves.x, keyframeTimes);
            AddKeyframeTimes(eulerCurves.y, keyframeTimes);
            AddKeyframeTimes(eulerCurves.z, keyframeTimes);

            // 按时间排序
            List<float> sortedTimes = new List<float>(keyframeTimes);
            sortedTimes.Sort();

            foreach (float time in sortedTimes)
            {
                float x = eulerCurves.x.Evaluate(time);
                float y = eulerCurves.y.Evaluate(time);
                float z = eulerCurves.z.Evaluate(time);

                // 转换为四元数
                var quaternion = Quaternion.Euler(x, y, z);

                if (normalize) quaternion.Normalize();

                // 添加关键帧
                qxCurve.AddKey(new Keyframe(time, quaternion.x));
                qyCurve.AddKey(new Keyframe(time, quaternion.y));
                qzCurve.AddKey(new Keyframe(time, quaternion.z));
                qwCurve.AddKey(new Keyframe(time, quaternion.w));
            }

            quaternionCurves["m_LocalRotation.x"] = qxCurve;
            quaternionCurves["m_LocalRotation.y"] = qyCurve;
            quaternionCurves["m_LocalRotation.z"] = qzCurve;
            quaternionCurves["m_LocalRotation.w"] = qwCurve;

            return quaternionCurves;
        }

        void AddKeyframeTimes(AnimationCurve curve, HashSet<float> times)
        {
            if (curve == null) return;

            foreach (var keyframe in curve.keys)
            {
                times.Add(keyframe.time);
            }
        }

        private class EulerCurves
        {
            public AnimationCurve x;
            public AnimationCurve y;
            public AnimationCurve z;
            public string xProperty;
            public string yProperty;
            public string zProperty;

            public bool IsValid()
            {
                return x != null && y != null && z != null;
            }
        }
    }
}