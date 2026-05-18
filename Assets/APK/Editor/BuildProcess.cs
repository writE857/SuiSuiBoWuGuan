using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.SceneManagement;

namespace APK.Editor
{
    public class BuildProcess : IPreprocessBuildWithReport, IProcessSceneWithReport
    {
        public int callbackOrder { get; }

        public void OnPreprocessBuild(BuildReport report)
        {
        }

        private bool modify;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (Application.isPlaying) return;
            if (modify || scene.buildIndex != 0) return;
            modify = true;
            var settings = scene
                .GetRootGameObjects()
                .Select(e => e.GetComponentInChildren<AutoAdSettings>())
                .First(e => e != null);
            switch (PlayerSettings.defaultInterfaceOrientation)
            {
                case UIOrientation.Portrait:
                case UIOrientation.PortraitUpsideDown:
                    settings
                        .Orientation = (int) Ori.Portrait;
                    break;
                case UIOrientation.LandscapeRight:
                case UIOrientation.LandscapeLeft:
                case UIOrientation.AutoRotation:
                    settings
                        .Orientation = (int) Ori.Landscape;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            EditorUtility.SetDirty(settings);
        }
    }
}