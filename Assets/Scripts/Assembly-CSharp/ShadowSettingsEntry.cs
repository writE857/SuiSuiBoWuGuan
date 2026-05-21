using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ShadowSettingsEntry : SettingsEntryLogic
{
	protected override void InitOptions()
	{
		options = new List<string> { "开启" };
		DefaultIndex = 0;
	}

	protected override void _Apply()
	{
		if (Camera.main != null && Camera.main.TryGetComponent<UniversalAdditionalCameraData>(out var cameraData))
		{
			cameraData.renderShadows = true;
		}
	}
}
