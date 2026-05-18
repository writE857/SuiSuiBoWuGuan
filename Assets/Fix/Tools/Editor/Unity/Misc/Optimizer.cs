using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fix.Editor
{
    public class Optimizer : FixEditorBase
    {
        [FixEditor(FixRoot + nameof(Optimizer) + "/" + "关闭渲染器接受阴影")]
        public static void SetRenderer()
        {
            FixEditorExtension.ForEachSceneAndPrefab<Renderer>(renderer =>
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            });
        }

        [FixEditor(FixRoot + nameof(Optimizer) + "/" + "关闭光照阴影")]
        public static void SetLight()
        {
            FixEditorExtension.ForEachSceneAndPrefab<Light>(light => { light.shadows = LightShadows.None; });
        }
    }
}