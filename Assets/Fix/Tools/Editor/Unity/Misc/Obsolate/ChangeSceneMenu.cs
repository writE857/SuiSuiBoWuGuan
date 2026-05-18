#if false
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fix.Editor
{
    public class ChangeSceneMenu : FixEditorWindow
    {
        private const string Title = "切换场景";
        private const int Width = 500, Height = 500;

        [MenuItem(FixRoot + Title, priority = 200)]
        public static void ShowWindow()
        {
            GetWindowWithRect<ChangeSceneMenu>(new Rect(Screen.width / 2, Screen.height / 2,
                    Width, Height)).titleContent =
                new GUIContent(Title);
        }

        private static readonly Color DraggingItemColor = new Color(0.854902f, 0.4392157f, 0.8392157f, 0.4f);
        private static readonly Color ItemColor = new Color(0.4f, 0.8039216f, 0.6666667f, 0.4f);
        private static readonly Color DropLineColor = Color.magenta;
        private const float ItemHeight = 30;
        private const float SpaceHeight = 4;
        private Vector2 pos;
        private bool isDraggingScene;
        private int dragSceneIndex;
        private int insertIndex;


        public void OnGUI()
        {
            var scenes = EditorBuildSettings.scenes;
            var scenesLength = scenes.Length;
            var list = scenes.Select(scene => new EditorBuildSettingsScene(scene.guid, scene.enabled)).ToList();
            VerticalRegion(drawRect =>
            {
                UnityEngine.Event currentEvent = UnityEngine.Event.current;

                //拖拽范围内
                if (drawRect.Contains(currentEvent.mousePosition))
                {
                    switch (currentEvent.type)
                    {
                        case EventType.DragUpdated:
                            DragAndDrop.visualMode = DragAndDropVisualMode.Generic; //到达目标区域的显示方式
                            break;
                        case EventType.DragPerform:
                            list.AddRange(GetTotalFiles(DragAndDrop.paths)
                                .Select(e => e.Replace("\\", "/"))
                                .Distinct()
                                .Where(e => !scenes.Any(ee => Equals(ee.path, e)))
                                .Where(e => AssetDatabase.LoadAssetAtPath<SceneAsset>(e) != null)
                                .Select(path => new EditorBuildSettingsScene(path, true)));
                            break;
                    }
                }

                if (scenesLength == 0)
                {
                    HorizontalRegion(() => GUILayout.Label("当前BuildSetting 中没有场景", new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleCenter
                    }));
                    return;
                }

                HorizontalRegion(() => GUILayout.Label($"当前场景 {SceneManager.GetActiveScene().name}",
                    new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fixedHeight = 20
                    }));
                GUILayout.Space(20);
                HorizontalRegion(rect =>
                {
                    const float height = ItemHeight + SpaceHeight;
                    var current = Event.current;

                    var mousePosition = current.mousePosition;

                    var selectedIndex =
                        Mathf.FloorToInt((mousePosition.y - rect.yMin + pos.y) / height);
                    var clampIndex = Mathf.Clamp(selectedIndex, 0, scenesLength);
                    switch (current.type)
                    {
                        case EventType.MouseDown:
                            if (DragAndDrop.paths.Length == 0
                                && selectedIndex >= 0
                                && selectedIndex < scenesLength)
                            {
                                isDraggingScene = true;
                                dragSceneIndex = selectedIndex;
                                this.Repaint();
                            }

                            break;
                        case EventType.MouseDrag:
                            if (isDraggingScene && drawRect.Contains(mousePosition))
                            {
                                if (clampIndex != insertIndex) this.Repaint();
                                insertIndex = clampIndex;
                            }

                            break;
                        case EventType.MouseUp:
                            if (isDraggingScene)
                            {
                                if (insertIndex >= 0 && insertIndex <= scenesLength)
                                {
                                    list.Insert(insertIndex, scenes[dragSceneIndex]);
                                    list.RemoveAt(insertIndex < dragSceneIndex ? dragSceneIndex + 1 : dragSceneIndex);
                                }

                                this.Repaint();
                            }

                            isDraggingScene = false;
                            break;
                        case EventType.Ignore:
                            isDraggingScene = false;
                            break;
                    }

                    pos = GUILayout.BeginScrollView(pos, GUILayout.MaxHeight(400), GUILayout.MinHeight(20));
                    var itemLabel = new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        fixedWidth = 120,
                        fixedHeight = 20
                    };
                    var itemBtn = new GUIStyle(GUI.skin.button)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fixedWidth = 60,
                        fixedHeight = ItemHeight
                    };
                    Action<int> spaceItem = index =>
                    {
                        HorizontalRegion(
                            spaceRect =>
                            {
                                EditorGUI.DrawRect(
                                    new Rect(spaceRect.x, spaceRect.y - SpaceHeight / 2, spaceRect.width,
                                        SpaceHeight), index == insertIndex ? DropLineColor : Color.clear);
                            }, GUILayout.Height(SpaceHeight), GUILayout.ExpandWidth(true));
                    };
                    Action delay = default;
                    for (var i = 0; i < scenesLength; i++)
                    {
                        var scene = list[i];
                        int index = i;
                        spaceItem.Invoke(index);

                        HorizontalRegion(itemRect =>
                        {
                            EditorGUI.DrawRect(itemRect, isDraggingScene && index == dragSceneIndex
                                ? DraggingItemColor
                                : ItemColor);
                            var sceneName = Path.GetFileNameWithoutExtension(scene.path);
                            GUILayout.Label(sceneName, itemLabel);
                            StateRegion(File.Exists(scene.path), () =>
                            {
                                scene.enabled = GUILayout.Toggle(scene.enabled, "激活");
                                if (GUILayout.Button("加载", itemBtn)) OpenScene(scene.path, OpenSceneMode.Single);
                                if (GUILayout.Button("附加", itemBtn)) OpenScene(scene.path, OpenSceneMode.Additive);
                            });
                            if (GUILayout.Button("删除", itemBtn) && EditorUtility.DisplayDialog("删除场景",
                                $"将要删除:\n{sceneName}", "确认"))
                                delay += () => list.RemoveAt(index);
                        }, GUILayout.Height(ItemHeight), GUILayout.ExpandWidth(true));
                    }

                    delay?.Invoke();
                    spaceItem.Invoke(scenesLength);
                    GUILayout.Space(SpaceHeight);
                    GUILayout.EndScrollView();
                });

            }, GUILayout.Width(Width), GUILayout.Height(Height));
            if (!isDraggingScene) insertIndex = -1;

            if (!scenes.SequenceEqual(list, EqualComparer.Instance)) EditorBuildSettings.scenes = list.ToArray();
        }

        private static void OpenScene(string scenePath, OpenSceneMode mode)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(scenePath, mode);
            }
        }

        private class EqualComparer : IEqualityComparer<EditorBuildSettingsScene>
        {
            public static readonly IEqualityComparer<EditorBuildSettingsScene> Instance = new EqualComparer();

            public bool Equals(EditorBuildSettingsScene x, EditorBuildSettingsScene y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.enabled == y.enabled && x.path == y.path && x.guid.Equals(y.guid);
            }

            public int GetHashCode(EditorBuildSettingsScene obj)
            {
                unchecked
                {
                    var hashCode = obj.enabled.GetHashCode();
                    hashCode = (hashCode * 397) ^ (obj.path != null ? obj.path.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ obj.guid.GetHashCode();
                    return hashCode;
                }
            }
        }
    }
}
#endif