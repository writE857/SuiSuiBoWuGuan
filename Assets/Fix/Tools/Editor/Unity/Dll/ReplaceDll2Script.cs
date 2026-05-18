using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Fix.Editor
{
    public class ReplaceDll2Script : FixEditorWindow
    {
        private const string Title = "Dll->Script";

        [MenuItem(FixRoot + Title, priority = 103)]
        [FixEditor(FixRoot + "Dll/" + Title)]
        private static void ShowWindow()
        {
            GetWindowWithRect<ReplaceDll2Script>(new Rect(Screen.width / 2, Screen.height / 2, 500, 500)).titleContent =
                new GUIContent(Title);
        }

        private static readonly string[] FindRoot = new[] {"Assets"};


        private static string WorkspaceFolder => GetWorkspaceFolder(nameof(ReplaceDll2Script));
        private string supportType = "";


        private readonly List<IReplaceModule> modules = new List<IReplaceModule>()
        {
            new DllModule(),
            new CSharpModule()
        };


        private Vector2 dragPos, unSelectedPos, selectedPos;

        private void OnEnable()
        {
            Refresh();
        }

        private void Refresh()
        {
            supportType = string.Join("/", modules.Select(e => e.FileType));
            foreach (var module in modules)
                module.Refresh();
        }

        private void OnGUI()
        {
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontSize = 12,
                    stretchWidth = true
                },
                buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fixedWidth = 24,
                    fixedHeight = 24,
                    fontSize = 12
                };

            DragRegion(
                paths =>
                {
                    var files = paths.GetTotalFiles();
                    modules.ForEach(e => e.AddFiles(files));
                }, $"拖入{supportType}文件",
                GUILayout.MinHeight(64), GUILayout.MinWidth(512));
            HorizontalRegion(() =>
            {
                GUILayout.Label($"拖入的{supportType}:");
                if (GUILayout.Button("保存", GUILayout.Width(80))) Save();
            });
            VerticalRegion(() =>
            {
                EditorGUI.indentLevel++;

                dragPos = EditorGUILayout.BeginScrollView(dragPos, GUILayout.MaxHeight(80));
                foreach (var module in modules) module.LayoutFile(labelStyle, buttonStyle);
                EditorGUILayout.EndScrollView();

                EditorGUI.indentLevel--;
            });


            HorizontalRegion(rect =>
            {
                Space(8);
                EditorGUI.DrawRect(new Rect(rect.x, rect.y - 2, rect.width, 4), new Color(0.0f, 1f, 1f, 0.4f));
            }, GUILayout.Height(4), GUILayout.ExpandWidth(true));
            HorizontalRegion(() =>
            {
                GUILayout.Label($"已保存的{supportType}:", labelStyle);
                if (GUILayout.Button("添加全部", GUILayout.Width(80)))
                    foreach (var module in modules)
                        module.SelectAll();
            });
            HorizontalRegion(() =>
            {
                unSelectedPos = EditorGUILayout.BeginScrollView(unSelectedPos, GUILayout.MaxHeight(80));
                foreach (var module in modules)
                    module.LayoutUnSelected(labelStyle, buttonStyle);

                EditorGUILayout.EndScrollView();
            });

            HorizontalRegion(rect =>
            {
                Space(4);
                EditorGUI.DrawRect(new Rect(rect.x, rect.y - 2, rect.width, 4), new Color(0.0f, 0.4f, 1f, 0.4f));
            }, GUILayout.Height(4), GUILayout.ExpandWidth(true));
            HorizontalRegion(() => GUILayout.Label($"用于替换的{supportType}:"));


            HorizontalRegion(() =>
            {
                selectedPos = EditorGUILayout.BeginScrollView(selectedPos, GUILayout.MaxHeight(80));
                foreach (var module in modules) module.LayoutSelected(labelStyle, buttonStyle);

                EditorGUILayout.EndScrollView();
            });
            Space(30);
            HorizontalRegion(() =>
            {
                if (GUILayout.Button("替换", GUILayout.Width(240), GUILayout.Height(40))) Replace();
                Space(8);
                if (GUILayout.Button("打开存档文件夹", GUILayout.Width(240), GUILayout.Height(40)))
                {
                    WorkspaceFolder.Mkdir();
                    ShowInExplorer(WorkspaceFolder);
                }
            });
        }


        private void Save()
        {
            var list = new List<Exception>();
            foreach (var module in modules)
            {
                try
                {
                    module.Save();
                }
                catch (Exception e)
                {
                    list.Add(e);
                }
            }

            ShowNotification(new GUIContent("保存成功"));
            if (list.Count > 0)
            {
                ShowNotification(new GUIContent("保存时发生错误,查看控制台"));
                foreach (var e in list) Debug.LogException(e);
            }

            Refresh();
        }

        private void Replace()
        {
            var list = new List<Exception>();
            var repList = new List<KeyValuePair<string, string>>();
            foreach (var module in modules)
            {
                try
                {
                    module.HandleReplace(repList);
                }
                catch (Exception e)
                {
                    list.Add(e);
                }
            }

            FixEditorExtension.BatchReplaceRef(repList);
            ShowNotification(new GUIContent("替换完成"));
            Refresh();
            if (list.Count > 0)
            {
                ShowNotification(new GUIContent("替换时发生错误,查看控制台"));
                foreach (var e in list) Debug.LogException(e);
            }
        }

        private static IDictionary<string, List<MonoScript>> GetMonoScriptDictionary()
        {
            var dictionary = new Dictionary<string, List<MonoScript>>();
            foreach (var script in AssetDatabase
                .FindAssets("t:MonoScript")
                .Select(AssetDatabase.GUIDToAssetPath)
                .SelectMany(AssetDatabase.LoadAllAssetsAtPath)
                .OfType<MonoScript>()
                .Where(e => e != null))
            {
                var fullName = script.GetClass()?.FullName;
                if (fullName == null) continue;
                if (!dictionary.TryGetValue(fullName, out var list))
                    dictionary.Add(fullName, list = new List<MonoScript>());
                list.Add(script);
            }

            return dictionary;
        }

        private interface IReplaceModule
        {
            string FileType { get; }
            void Refresh();
            void AddFiles(IEnumerable<string> paths);
            void LayoutFile(GUIStyle labelStyle, GUIStyle buttonStyle);
            void LayoutUnSelected(GUIStyle labelStyle, GUIStyle buttonStyle);
            void LayoutSelected(GUIStyle labelStyle, GUIStyle buttonStyle);
            void SelectAll();
            void Save();
            void HandleReplace(List<KeyValuePair<string, string>> repList);
        }

        private class DllModule : IReplaceModule
        {
            private const string Extension = ".dll";
            public string FileType => "Dll";
            private string Workspace => $"{WorkspaceFolder}/{FileType}";

            private List<string>
                files = new List<string>(),
                existFiles = new List<string>(),
                selectedFiles = new List<string>();

            private Color color = new Color(0.4f, 0.5f, 0.9f);


            public void Refresh()
            {
                files.Clear();
                existFiles.Clear();
                selectedFiles.Clear();
                if (Directory.Exists(Workspace))
                    existFiles.AddRange(Directory.GetFiles(Workspace));
            }

            public void AddFiles(IEnumerable<string> paths)
            {
                foreach (var file in paths
                    .Where(e =>
                        Extension.Equals(Path.GetExtension(e), StringComparison.CurrentCultureIgnoreCase))
                    .Where(e => !files.Contains(e)))
                    files.Add(file);
            }

            public void LayoutFile(GUIStyle labelStyle, GUIStyle buttonStyle)
            {
                ColorRegion(color, () =>
                {
                    var array = files.ToArray();
                    for (var i = 0; i < array.Length; i++)
                    {
                        int index = i;
                        HorizontalRegion(() =>
                        {
                            var dragDll = array[index];
                            GUILayout.Label(Path.GetFileNameWithoutExtension(dragDll), labelStyle);
                            if (GUILayout.Button("x", buttonStyle)) files.RemoveAt(index);
                        });
                    }
                });
            }

            public void LayoutUnSelected(GUIStyle labelStyle, GUIStyle buttonStyle)
            {
                ColorRegion(color, () =>
                {
                    foreach (var existedDll in existFiles)
                        HorizontalRegion(() =>
                        {
                            GUILayout.Label(Path.GetFileNameWithoutExtension(existedDll), labelStyle);
                            if (GUILayout.Button("+", buttonStyle)) AddSelectedItem(existedDll);
                        });
                });
            }

            public void LayoutSelected(GUIStyle labelStyle, GUIStyle buttonStyle)
            {
                ColorRegion(color, () =>
                {
                    var array = selectedFiles.ToArray();
                    for (var i = 0; i < array.Length; i++)
                    {
                        int index = i;
                        HorizontalRegion(() =>
                        {
                            var dllPath = array[index];
                            GUILayout.Label(Path.GetFileNameWithoutExtension(dllPath), labelStyle);
                            if (GUILayout.Button("x", buttonStyle)) selectedFiles.RemoveAt(index);
                        });
                    }
                });
            }

            public void Save()
            {
                Workspace.Mkdir();
                foreach (var dllPath in files)
                {
                    var savePath = GetSavePath(dllPath);
                    var entries = new Dictionary<string, string>();

                    foreach (var script in AssetDatabase.LoadAllAssetRepresentationsAtPath(dllPath)
                        .OfType<MonoScript>())
                    {
                        string fullName;
                        using (var so = new SerializedObject(script))
                        {
                            var @namespace = so.FindProperty("m_Namespace").stringValue;
                            fullName = string.IsNullOrWhiteSpace(@namespace)
                                ? so.FindProperty("m_ClassName").stringValue
                                : $"{@namespace}.{so.FindProperty("m_ClassName").stringValue}";
                        }

                        if (script.TryGetRefString(out var s))
                            entries.Add(fullName, s);
                    }

                    URPFix(dllPath, entries);
                    File.WriteAllText(savePath, JsonConvert.SerializeObject(entries, Formatting.Indented));
                }
            }

            private void URPFix(string dllPath, IDictionary<string, string> entries)
            {
                if (Path.GetFileName(dllPath) != "Unity.RenderPipelines.Universal.Runtime.dll") return;
                if (!entries.ContainsKey("UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset"))
                    entries.Add("UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset",
                        "{fileID: -549186028, guid: c88ab7b37c4f350242674d2efd621c19, type: 3}");
            }

            public void HandleReplace(List<KeyValuePair<string, string>> repList)
            {
                var dictionary = GetMonoScriptDictionary();

                foreach (var dllPath in selectedFiles)
                {
                    if (!File.Exists(dllPath)) throw new Exception($"未找到{dllPath}的信息文件");
                    var oldEntries =
                        JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(dllPath));
                    if (oldEntries == null) throw new Exception($"{dllPath}的信息文件损坏");
                    var newEntries = new Dictionary<string, string>();
                    foreach (var script in dictionary.SelectMany(e => e.Value))
                    {
                        string key;
                        using (var so = new SerializedObject(script))
                        {
                            var @namespace = so.FindProperty("m_Namespace").stringValue;
                            key = string.IsNullOrWhiteSpace(@namespace)
                                ? so.FindProperty("m_ClassName").stringValue
                                : $"{@namespace}.{so.FindProperty("m_ClassName").stringValue}";
                        }

                        if (oldEntries.TryGetValue(key, out var @oldStr)
                            && script.TryGetRefString(out var @newStr)
                            && @oldStr != @newStr)
                            newEntries.Add(key, @newStr);
                    }

                    var notMatch = newEntries.Where(e => !oldEntries.ContainsKey(e.Key)).Select(e => e.Key).ToArray();
                    if (notMatch.Length > 0)
                    {
                        if (!EditorUtility.DisplayDialog("存在未匹配的内容", string.Join("\n", notMatch), "继续替换", "取消替换"))
                            return;
                    }

                    foreach (var pair in newEntries)
                        if (oldEntries.TryGetValue(pair.Key, out var @oldStr))
                            repList.Add(new KeyValuePair<string, string>(@oldStr, pair.Value));
                }
            }


            public void SelectAll()
            {
                existFiles.ForEach(AddSelectedItem);
            }

            private void AddSelectedItem(string path)
            {
                if (!selectedFiles.Contains(path)) selectedFiles.Add(path);
            }

            private string GetSavePath(string path) =>
                $"{Workspace}/{Path.GetFileNameWithoutExtension(path)}.json";

            private class TypeEntry
            {
                public string name;
                public string fullName;
                public string refString;

                protected bool Equals(TypeEntry other)
                {
                    return refString == other.refString;
                }

                public override bool Equals(object obj)
                {
                    if (ReferenceEquals(null, obj)) return false;
                    if (ReferenceEquals(this, obj)) return true;
                    if (obj.GetType() != this.GetType()) return false;
                    return Equals((TypeEntry) obj);
                }

                public override int GetHashCode()
                {
                    return (refString != null ? refString.GetHashCode() : 0);
                }
            }
        }

        private class CSharpModule : IReplaceModule
        {
            public string FileType => "C#";

            private List<string>
                files = new List<string>(),
                existTypeFullName = new List<string>(),
                selectedTypeFullName = new List<string>();


            private Color color = new Color(0.8f, 0.34f, 0.8f);

            private const string Extension = ".cs";
            private string Workspace => $"{WorkspaceFolder}/{FileType}";


            public void Refresh()
            {
                files.Clear();
                existTypeFullName.Clear();
                selectedTypeFullName.Clear();
                var savePath = GetSavePath();
                if (File.Exists(savePath))
                {
                    var dictionary =
                        JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(savePath));
                    if (dictionary != null)
                        existTypeFullName.AddRange(dictionary.Keys);
                }
            }

            public void AddFiles(IEnumerable<string> paths)
            {
                foreach (var file in paths
                    .Where(e =>
                        Extension.Equals(Path.GetExtension(e), StringComparison.CurrentCultureIgnoreCase))
                    .Where(e => !files.Contains(e)))
                    files.Add(file);
            }

            private bool fileFoldout;

            public void LayoutFile(GUIStyle labelStyle, GUIStyle buttonStyle)
            {
                ColorRegion(color, () =>
                {
                    if (files.Count > 0)
                    {
                        HorizontalRegion(() =>
                        {
                            if (GUILayout.Button(FileType, labelStyle, GUILayout.Width(60)))
                                fileFoldout = !fileFoldout;
                            fileFoldout = EditorGUILayout.Foldout(fileFoldout, "", true);
                            if (GUILayout.Button("x", buttonStyle)) files.Clear();
                        });

                        if (fileFoldout)
                        {
                            IndexLevelRegion(() =>
                            {
                                VerticalRegion(() =>
                                {
                                    var array = files.ToArray();
                                    for (var i = 0; i < array.Length; i++)
                                    {
                                        int index = i;
                                        HorizontalRegion(() =>
                                        {
                                            var dragDll = array[index];
                                            GUILayout.Label(Path.GetFileNameWithoutExtension(dragDll), labelStyle);
                                            if (GUILayout.Button("x", buttonStyle)) files.RemoveAt(index);
                                        });
                                    }
                                });
                            });
                        }
                    }
                });
            }

            private bool unSelectedFoldout;

            public void LayoutUnSelected(GUIStyle labelStyle, GUIStyle buttonStyle)
            {
                ColorRegion(color, () =>
                {
                    if (existTypeFullName.Count > 0)
                    {
                        HorizontalRegion(() =>
                        {
                            if (GUILayout.Button(FileType, labelStyle, GUILayout.Width(60)))
                                unSelectedFoldout = !unSelectedFoldout;
                            unSelectedFoldout = EditorGUILayout.Foldout(unSelectedFoldout, "", true);
                            if (GUILayout.Button("+", buttonStyle)) SelectAll();
                        });

                        if (unSelectedFoldout)
                        {
                            IndexLevelRegion(() =>
                            {
                                VerticalRegion(() =>
                                {
                                    foreach (var fullName in existTypeFullName)
                                        HorizontalRegion(() =>
                                        {
                                            GUILayout.Label(fullName, labelStyle);
                                            if (GUILayout.Button("+", buttonStyle)) AddSelectedItem(fullName);
                                        });
                                });
                            });
                        }
                    }
                });
            }

            private bool selectedFoldout;

            public void LayoutSelected(GUIStyle labelStyle, GUIStyle buttonStyle)
            {
                ColorRegion(color, () =>
                {
                    if (selectedTypeFullName.Count > 0)
                    {
                        HorizontalRegion(() =>
                        {
                            if (GUILayout.Button(FileType, labelStyle, GUILayout.Width(60)))
                                selectedFoldout = !selectedFoldout;
                            selectedFoldout = EditorGUILayout.Foldout(selectedFoldout, "", true);
                            if (GUILayout.Button("-", buttonStyle)) selectedTypeFullName.Clear();
                        });

                        if (selectedFoldout)
                        {
                            IndexLevelRegion(() =>
                            {
                                VerticalRegion(() =>
                                {
                                    var array = selectedTypeFullName.ToArray();
                                    for (var i = 0; i < array.Length; i++)
                                    {
                                        int index = i;
                                        HorizontalRegion(() =>
                                        {
                                            var fullName = array[index];
                                            GUILayout.Label(fullName, labelStyle);
                                            if (GUILayout.Button("x", buttonStyle))
                                                selectedTypeFullName.RemoveAt(index);
                                        });
                                    }
                                });
                            });
                        }
                    }
                });
            }

            public void Save()
            {
                Workspace.Mkdir();
                var savePath = GetSavePath();
                Dictionary<string, string> entries = null;
                if (File.Exists(savePath))
                {
                    var dictionary =
                        JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(savePath));
                    if (dictionary != null)
                        entries = dictionary;
                }

                if (entries == null) entries = new Dictionary<string, string>();
                foreach (var script in files.Select(AssetDatabase.LoadAssetAtPath<MonoScript>))
                {
                    string fullName;
                    if (script.TryGetRefString(out var s)
                        && (fullName = script.GetClass()?.FullName) != null)
                        entries.Add(fullName, s);
                }

                File.WriteAllText(savePath, JsonConvert.SerializeObject(entries, Formatting.Indented));
            }

            public void HandleReplace(List<KeyValuePair<string, string>> repList)
            {
                var savePath = GetSavePath();
                if (!File.Exists(savePath)) throw new Exception($"未找到{FileType}的信息文件");

                var oldEntries =
                    JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(savePath));
                if (oldEntries == null) throw new Exception($"{FileType}的信息文件损坏");
                var dictionary = GetMonoScriptDictionary();
                foreach (var fullName in selectedTypeFullName)
                {
                    if (!oldEntries.TryGetValue(fullName, out var @oldStr) ||
                        !dictionary.TryGetValue(fullName, out var list)) continue;
                    foreach (var script in list)
                    {
                        if (script.TryGetRefString(out var @newStr)
                            && @oldStr != @newStr) repList.Add(new KeyValuePair<string, string>(@oldStr, @newStr));
                    }
                }
            }


            public void SelectAll()
            {
                existTypeFullName.ForEach(AddSelectedItem);
            }

            private void AddSelectedItem(string path)
            {
                if (!selectedTypeFullName.Contains(path)) selectedTypeFullName.Add(path);
            }

            private string GetSavePath() =>
                $"{Workspace}/Assembly-CSharp.json";
        }
    }
}