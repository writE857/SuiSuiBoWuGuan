using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RenderScaleSettingsEntry : SettingsEntryLogic
{
	protected override void InitOptions()
	{
		options = new List<string> { "100%" };
		DefaultIndex = 0;
	}

	protected override void _Apply()
	{
		UniversalRenderPipelineAsset universalRenderPipelineAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
		if (universalRenderPipelineAsset != null)
		{
			universalRenderPipelineAsset.renderScale = 1f;
		}
		else
		{
			Debug.LogWarning("URP is not active — cannot apply renderScale.");
		}
	}
}
