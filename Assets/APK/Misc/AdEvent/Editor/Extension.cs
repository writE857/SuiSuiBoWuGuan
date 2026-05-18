using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace AdEvent.Editor
{
    public static class Extension
    {
        public static T Log<T>(this T t)
        {
            Debug.Log(t);
            return t;
        }
        public static string ParentPath(this string propertyPath)
        {
            int indexOf;
            return (indexOf = propertyPath.LastIndexOf('.')) != -1 ? propertyPath.Substring(0, indexOf) : string.Empty;
        }
 
        public static object Find(this object obj, string actions) =>
            actions.Split('.').Aggregate(obj, (o, action) => o.InternalFind(action));
 
        private static readonly Regex Indexer = new Regex(@"(?<=data\[).*(?=\])");
 
        private static object InternalFind(this object obj, string action)
        {
            if (string.IsNullOrWhiteSpace(action)) return obj;
            switch (Indexer.IsMatch(action))
            {
                case true:
                    var index = int.Parse(Indexer.Match(action).Value);
                    var array = (Array)obj;
                    return array.GetValue(index);
                case false:
                    switch (action)
                    {
                        case "Array":
                            return ((IEnumerable) obj).OfType<object>().ToArray();
                        default:
                            var type = obj.GetType();
                        {
                            var value = type.GetField(action,
                                BindingFlags.Instance
                                | BindingFlags.Public
                                | BindingFlags.NonPublic
                                | BindingFlags.Default
                                | BindingFlags.GetField)?.GetValue(obj);
                            if (value != null) return value;
                        }
                        {
                            var value = type.GetProperty(action,
                                BindingFlags.Instance
                                | BindingFlags.Public
                                | BindingFlags.NonPublic
                                | BindingFlags.Default
                                | BindingFlags.GetProperty)?.GetValue(obj);
                            if (value != null) return value;
                        }
                            return null;
                    }
                    default: return null;
            }
        }

        public static bool TryGet<T>(this IList<T> list, Predicate<T> predicate, out T result)
        {
            foreach (var t in list)
            {
                if (!predicate(t)) continue;
                result = t;
                return true;
            }

            result = default;
            return false;
        }
    }
}