using System.Collections.Generic;
using UnityEngine;

public class WindowModeSettingsEntry : SettingsEntryLogic
{
	private List<string> values = new List<string> { "全屏", "窗口" };

	protected override void InitOptions()
	{
		options = values;
		DefaultIndex = 0;
	}

	protected override void _Apply()
	{
		if (SaveIndex == 0)
		{
			Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
		}
		else if (SaveIndex == 1)
		{
			Screen.fullScreenMode = FullScreenMode.Windowed;
		}
	}
}
