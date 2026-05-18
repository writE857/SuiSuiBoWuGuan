using UnityEngine;

public class FullScreenChecker : MonoBehaviour
{
	public WindowModeSettingsEntry WindowModeSettingsEntry;

	private void Update()
	{
		if (WindowModeSettingsEntry.SaveIndex == 1)
		{
			if (Screen.fullScreenMode == FullScreenMode.FullScreenWindow)
			{
				WindowModeSettingsEntry.SaveIndex = 0;
				WindowModeSettingsEntry.SettingsEntry.OnSelectedIndexChanged?.Invoke(0);
			}
		}
		else if (WindowModeSettingsEntry.SaveIndex == 0 && Screen.fullScreenMode == FullScreenMode.Windowed)
		{
			WindowModeSettingsEntry.SaveIndex = 1;
			WindowModeSettingsEntry.SettingsEntry.OnSelectedIndexChanged?.Invoke(1);
		}
	}
}
