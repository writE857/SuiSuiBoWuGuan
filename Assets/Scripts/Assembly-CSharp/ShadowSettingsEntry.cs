using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ShadowSettingsEntry : SettingsEntryLogic
{
	protected override void InitOptions()
	{
		options = new List<string> { "开启", "关闭" };
		DefaultIndex = 0;
	}

	protected override void _Apply()
	{
		Camera.main.GetComponent<UniversalAdditionalCameraData>().renderShadows = SaveIndex == 0;
		Debug.Log("Shadows setting applied: " + ((SaveIndex == 0) ? "On" : "Off"));
	}
}
