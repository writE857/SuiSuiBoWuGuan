using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RenderScaleSettingsEntry : SettingsEntryLogic
{
	protected override void InitOptions()
	{
		options = new List<string>();
		for (float num = 0.5f; num <= 1.6f; num += 0.1f)
		{
			options.Add(num.ToString("0%"));
		}
		DefaultIndex = options.IndexOf(1.ToString("0%"));
	}

	protected override void _Apply()
	{
		UniversalRenderPipelineAsset universalRenderPipelineAsset = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
		if (universalRenderPipelineAsset != null)
		{
			Debug.Log("Render scale set to: " + (universalRenderPipelineAsset.renderScale = float.Parse(options[SaveIndex].Replace("%", "")) / 100f));
		}
		else
		{
			Debug.LogWarning("URP is not active — cannot apply renderScale.");
		}
	}
}
