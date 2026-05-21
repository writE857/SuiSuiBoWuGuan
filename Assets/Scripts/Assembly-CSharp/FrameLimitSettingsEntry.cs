using System.Collections.Generic;
using UnityEngine;

public class FrameLimitSettingsEntry : SettingsEntryLogic
{
	protected override void InitOptions()
	{
		options = new List<string> { "默认" };
		DefaultIndex = 0;
	}

	protected override void _Apply()
	{
		Application.targetFrameRate = -1;
	}
}
