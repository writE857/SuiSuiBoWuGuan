using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FrameLimitSettingsEntry : SettingsEntryLogic
{
	protected override void InitOptions()
	{
		List<int> source = (from r in Screen.resolutions.Select((Resolution r) => r.refreshRate).Distinct()
			orderby r
			select r).ToList();
		options = source.Select((int r) => r + " 帧").ToList();
		options.Insert(0, "关闭");
		DefaultIndex = options.Count - 1;
	}

	protected override void _Apply()
	{
		if (SaveIndex == 0)
		{
			Application.targetFrameRate = -1;
		}
		else
		{
			Application.targetFrameRate = int.Parse(options[SaveIndex].Split(' ')[0]);
		}
		Debug.Log("Frame limit set to: " + Application.targetFrameRate);
	}
}
