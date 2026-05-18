using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;


namespace Fix.Editor
{
    public class ColliderFix : FixEditorBase
    {
        static ColliderFix()
        {
            Settings = new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = UnityConverters.Converters.Append(new ColliderFixExtension.ObjectConverter()).ToList()
            };
        }

        private static JsonSerializerSettings Settings { get; }

        private sealed class ColliderData
        {
            public string id;
            public Dictionary<string, string> kv = new Dictionary<string, string>();

            public static ColliderData From(Component component, bool precise)
            {
                var data = new ColliderData
                {
                    id = component.GetIdentify(precise)
                };
                foreach (var propertyInfo in component
                    .GetType()
                    .GetProperties()
                    .Where(e => e.CanRead && e.CanWrite)
                    .IgnoreUnityObject()
                )
                {
                    data.kv.Add(propertyInfo.Name,
                        JsonConvert.SerializeObject(propertyInfo.GetValue(component), Settings));
                }

                return data;
            }

            public void Apply(Component component)
            {
                var type = component.GetType();
                var dictionary = type
                    .GetProperties()
                    .Where(e => e.CanRead && e.CanWrite)
                    .IgnoreUnityObject()
                    .ToDictionary(e => e.Name);
                foreach (var pair in kv)
                {
                    if (!dictionary.TryGetValue(pair.Key, out var value)) continue;
                    try
                    {
                        value.SetValue(component,
                            JsonConvert.DeserializeObject(pair.Value, value.PropertyType, Settings));
                    }
                    catch
                    {
                        Debug.LogWarning($"Apply prop {pair.Key} failure");
                    }
                }
            }

            private bool Equals(ColliderData other)
            {
                return id == other.id;
            }

            public override bool Equals(object obj)
            {
                return ReferenceEquals(this, obj) || obj is ColliderData other && Equals(other);
            }

            public override int GetHashCode()
            {
                return (id != null ? id.GetHashCode() : 0);
            }
        }

        private class SaveData
        {
            public bool precise;
            public List<ColliderData> list = new List<ColliderData>();
        }

        private static string DataFilePath => $"{nameof(ColliderFix).GetWorkspaceFolder()}/Data.json";

        [FixEditor(FixRoot + "碰撞体" + "/" + nameof(Save))]
        public static void Save()
        {
            bool precise = EditorUtility.DisplayDialog("", "是否使用使用更准确的保存方式?\n精准的方式更花时间", "确认", "取消");

            var set = new HashSet<ColliderData>();
            FixEditorExtension.ForEachSceneAndPrefab(() =>
            {
                set.AddRange(Resources.FindObjectsOfTypeAll<Collider2D>().Select(e => ColliderData.From(e, precise)));

                set.AddRange(Resources.FindObjectsOfTypeAll<Collider>().Select(e => ColliderData.From(e, precise)));
            });
            var data = new SaveData() {precise = precise, list = set.ToList()};
            Path.GetDirectoryName(DataFilePath).Mkdir();
            File.WriteAllText(DataFilePath, JsonConvert.SerializeObject(data, Formatting.Indented));
        }

        [FixEditor(FixRoot + "碰撞体" + "/" + nameof(Load), true)]
        private static bool CanLoad()
        {
            return File.Exists(DataFilePath);
        }


        [FixEditor(FixRoot + "碰撞体" + "/" + nameof(Load), false)]
        public static void Load()
        {
            if (!File.Exists(DataFilePath)) throw new Exception("未检测到文件");
            var sd = JsonConvert.DeserializeObject<SaveData>(File.ReadAllText(DataFilePath));
            if (sd == null) throw new Exception("解析文件失败");
            var list = sd.list;
            var precise = sd.precise;
            var dictionary = new Dictionary<string, ColliderData>();
            foreach (var data in list)
            {
                if (!dictionary.TryGetValue(data.id, out var value)) dictionary.Add(data.id, data);
                else
                {
                    string d1 = JsonConvert.SerializeObject(data.kv), d2 = JsonConvert.SerializeObject(value.kv);
                    if (d1 != d2) throw new Exception($"Invalid data :\n{JsonConvert.SerializeObject(data)}");
                }
            }

            FixEditorExtension.ForEachSceneAndPrefab(() =>
            {
                foreach (var comp in Resources.FindObjectsOfTypeAll<Collider2D>().OfType<Component>()
                    .Concat(Resources.FindObjectsOfTypeAll<Collider>()))
                {
                    try
                    {
                        if (dictionary.TryGetValue(comp.GetIdentify(precise), out var data))
                            data.Apply(comp);
                    }
                    catch
                    {
                    }
                }
            }, true);
        }
    }

    internal static class ColliderFixExtension
    {
        internal static IEnumerable<PropertyInfo> IgnoreUnityObject(this IEnumerable<PropertyInfo> infos)
        {
            foreach (var info in infos)
            {
                if (!typeof(Object).IsAssignableFrom(info.PropertyType)) yield return info;
            }
        }

        internal static string GetIdentify(this Component obj, bool precise)
        {
            if (!precise) return $"{GlobalObjectId.GetGlobalObjectIdSlow(obj).targetObjectId}";
            var stepTransformString = FixEditorUtils.StepTransform(
                obj.transform,
                e => GlobalObjectId.GetGlobalObjectIdSlow(obj).targetObjectId.ToString(),
                FixEditorUtils.TabConfig.NoTab);
            var sourceKey = $"{obj.gameObject.scene.buildIndex} {stepTransformString}";

            return sourceKey.ReformatKey();
        }

        public class ObjectConverter : JsonConverter<Object>
        {
            public override void WriteJson(JsonWriter writer, Object value, JsonSerializer serializer)
            {
                writer.WriteNull();
            }

            public override Object ReadJson(JsonReader reader, Type objectType, Object existingValue,
                bool hasExistingValue,
                JsonSerializer serializer)
            {
                return null;
            }
        }
    }
}