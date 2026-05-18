using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Fix.Editor
{
    public class SetFont : FixEditorWindow
    {
        static SetFont()
        {
            Name2FontAssetType.Add(typeof(UnityEngine.Font));
            if ("TMPro.TMP_FontAsset".TryGetType(out var type)) Name2FontAssetType.Add(type);
        }

        private const string Title = "设置字体";
        private static readonly string[] FindRoot = new[] {"Assets"};

        private static readonly ISet<Type> Name2FontAssetType = new HashSet<Type>();

        [MenuItem(FixRoot + Title, priority = 103)]
        [FixEditor(FixRoot + nameof(Font) + "/" + Title)]
        private static void ShowWindow()
        {
            GetWindowWithRect<SetFont>(new Rect(Screen.width / 2, Screen.height / 2, 500, 500)).titleContent =
                new GUIContent(Title);
        }


        private readonly List<Entry> type2Asset = new List<Entry>();

        private void OnEnable()
        {
            foreach (var type in Name2FontAssetType)
            {
                type2Asset.Add(new Entry(type, default));
            }
        }

        private void OnGUI()
        {
            HorizontalRegion(() => { GUILayout.Label("选择字体"); });

            GUILayout.Space(20);
            VerticalRegion(() =>
            {
                foreach (var pair in type2Asset)
                {
                    HorizontalRegion(() =>
                    {
                        pair.asset = EditorGUILayout.ObjectField(pair.type.FullName, pair.asset,
                            pair.type, false);
                    });
                }
            });
            GUILayout.Space(40);
            HorizontalRegion(() =>
            {
                GUILayout.Space(150);
                if (GUILayout.Button("更换字体", GUILayout.Height(40), GUILayout.Width(200)))
                {
                    DoSetFont();
                }
            });
        }

        private void DoSetFont()
        {
            if (type2Asset.All(e => e.asset == null))
            {
                ShowNotification(new GUIContent("未选择字体"));
                return;
            }

            try
            {
                var mapper = type2Asset.Where(e => e.asset != null).ToDictionary(e => e.asset, e => new List<Object>());
                var t2mk = type2Asset.Where(e => e.asset != null).ToDictionary(e => e.type, e => e.asset);

                foreach (var o in
                    Name2FontAssetType.SelectMany(FindProjectFonts))
                {
                    if (mapper.ContainsKey(o) || !t2mk.ContainsKey(o.GetType())) continue;
                    mapper[t2mk[o.GetType()]].Add(o);
                }

                var list = new List<KeyValuePair<string, string>>();
                foreach (var pair in mapper)
                {
                    if (!pair.Key.TryGetRefString(out var @new)) throw new Exception("Invalid asset : " + pair.Key);

                    foreach (var o in pair.Value)
                    {
                        if (!o.TryGetRefString(out var @old)) throw new Exception("Invalid asset : " + pair.Key);
                        list.Add(new KeyValuePair<string, string>(@old, @new));
                    }
                }

                FixEditorExtension.BatchReplaceRef(list);
            }
            finally
            {
                ShowNotification(new GUIContent("替换结束"));
            }
        }


        private static IEnumerable<Object> FindProjectFonts(Type type)
        {
            return AssetDatabase.FindAssets($"t:{type.Name}", new string[] {"Assets"})
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(e => AssetDatabase.GetMainAssetTypeAtPath(e) == type)
                .Select(AssetDatabase.LoadAssetAtPath<Object>)
                .Concat(Resources.FindObjectsOfTypeAll(type))
                .Distinct();
        }

        private class Entry
        {
            public Type type;
            public Object asset;

            public Entry(Type type, Object asset)
            {
                this.type = type;
                this.asset = asset;
            }

            public override string ToString()
            {
                return $"{nameof(type)}: {type}, {nameof(asset)}: {asset}";
            }
        }
    }
}