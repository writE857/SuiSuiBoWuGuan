using System.Collections.Generic;
using UnityEngine;

public class WindowModeSettingsEntry : SettingsEntryLogic
{
	private List<string> values = new List<string> { "默认全屏" };

	protected override void InitOptions()
	{
		options = values;
		DefaultIndex = 0;
	}

	protected override void _Apply()
	{
		// Project/player settings own fullscreen behavior for WebGL wrappers and APK builds.
	}
}
