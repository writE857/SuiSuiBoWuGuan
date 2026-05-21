using System.Collections.Generic;
using UnityEngine;

public class VsyncSettingsEntry : SettingsEntryLogic
{
	protected override void InitOptions()
	{
		options = new List<string> { "关闭" };
		DefaultIndex = 0;
	}

	protected override void _Apply()
	{
		QualitySettings.vSyncCount = 0;
	}
}
