using System.Collections.Generic;
using UnityEngine;

public class VsyncSettingsEntry : SettingsEntryLogic
{
	protected override void InitOptions()
	{
		options = new List<string> { "关闭", "开启（每帧）", "开启（隔帧）" };
		DefaultIndex = 1;
	}

	protected override void _Apply()
	{
		QualitySettings.vSyncCount = SaveIndex;
		Debug.Log("Vsync set to: " + SaveIndex);
	}
}
