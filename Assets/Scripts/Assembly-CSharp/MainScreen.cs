using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MainScreen : Singleton<MainScreen>
{
	public List<MonitorScreen> Screens = new List<MonitorScreen>();

	public MonitorScreen MainScreenObject;

	public MonitorScreen ActiveScreen;

	public ConfirmationScreen ConfirmationScreen;

	public MonitorScreen RoomScreen;

	public MonitorScreen PrestigeScr;

	public GameObject SettingsScreen;

	public InputActionReference CancelAction;

	private void Start()
	{
		Activate(MainScreenObject);
		if (SettingsScreen == null)
		{
			return;
		}
		try
		{
			SettingsScreen.SetActive(value: true);
		}
		catch (System.Exception exception)
		{
			Debug.LogException(exception, SettingsScreen);
		}
		finally
		{
			SettingsScreen.SetActive(value: false);
		}
	}

	public void Activate(MonitorScreen target)
	{
		foreach (MonitorScreen screen in Screens)
		{
			if (screen == null)
			{
				continue;
			}
			if (screen == target)
			{
				screen.Activate();
				ActiveScreen = screen;
				Singleton<GameSession>.Current.IsInMenu = true;
			}
			else
			{
				screen.Hide();
			}
		}
		if (SettingsScreen != null && (target == null || target.gameObject != SettingsScreen))
		{
			SettingsScreen.SetActive(value: false);
		}
		if (target == null)
		{
			if (ActiveScreen != RoomScreen)
			{
				Activate(RoomScreen);
			}
			Singleton<GameSession>.Current.IsInMenu = false;
		}
	}

	private void Update()
	{
		if (CancelAction.action.WasPressedThisFrame() && Singleton<GameSession>.Current.IsGameStarted && !Singleton<PrestigeScreen>.Current.IsShoppingMode)
		{
			if (Singleton<ArtifactOverlayDisplay>.Current.IsOpen)
			{
				Singleton<ArtifactOverlayDisplay>.Current.Hide();
			}
			else if (!Singleton<GameSession>.Current.IsInMenu)
			{
				Singleton<GameSession>.Current.IsInMenu = true;
				Activate(MainScreenObject);
			}
			else if (Singleton<GameSession>.Current.IsInMenu && ActiveScreen == MainScreenObject)
			{
				Singleton<MainScreen>.Current.Activate(Singleton<MainScreen>.Current.RoomScreen);
				Singleton<GameSession>.Current.IsInMenu = false;
			}
		}
	}

	public void ShowConfirmationScreen(ConfirmationDialogTexts overWriteSaveTexts, MonitorScreen monitorScreen)
	{
		Activate(ConfirmationScreen.monitorScreen);
		ConfirmationScreen.Show(overWriteSaveTexts, monitorScreen);
	}
}
