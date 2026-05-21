using System.Collections.Generic;
using UnityEngine;

public class ResolutionSettingsEntry : SettingsEntryLogic
{
	public List<ResolutionData> Resolutions = new List<ResolutionData>();

	protected override void InitOptions()
	{
		Resolutions.Clear();
		options = new List<string> { "默认" };
		DefaultIndex = 0;
	}

	protected override void _Apply()
	{
		// Use the platform/native resolution. Runtime SetResolution is unsafe for WebGL and mobile wrappers.
	}

	public List<string> GetAvailableResolutions()
	{
		Resolution[] resolutions = Screen.resolutions;
		List<string> list = new List<string>();
		Resolutions.Clear();
		Resolution[] array = resolutions;
		for (int i = 0; i < array.Length; i++)
		{
			Resolution resolution = array[i];
			string text = $"{resolution.width} × {resolution.height}";
			ResolutionData data = new ResolutionData();
			data.Width = resolution.width;
			data.Height = resolution.height;
			data.displayString = text;
			if (!Resolutions.Exists((ResolutionData a) => a.Width == data.Width && a.Height == data.Height))
			{
				list.Add(text);
				Resolutions.Add(data);
			}
		}
		return list;
	}
}
