using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
	public MonitorScreen monitorScreen;

	public Button NewGameButton;

	public Button ContinueGameButton;

	public Button SettingsButton;

	public Button PrestigeButton;

	public Button QuitButton;

	public ConfirmationDialogTexts QuitConfirmTexts;

	public ConfirmationDialogTexts OverWriteSaveTexts;

	public MonitorScreen SettingsScreen;

	public MonitorScreen PrestigeScreen;

	private void Start()
	{
		NewGameButton.onClick.AddListener(OnNewGameClicked);
		ContinueGameButton.onClick.AddListener(OnContinueGameClicked);
		QuitButton.onClick.AddListener(OnQuitClicked);
		SettingsButton.onClick.AddListener(OnSettingsClicked);
		PrestigeButton.onClick.AddListener(OnPrestigeClicked);
	}

	private void OnQuitClicked()
	{
		Singleton<MainScreen>.Current.ShowConfirmationScreen(QuitConfirmTexts, monitorScreen);
		ConfirmationScreen confirmationScreen = Singleton<MainScreen>.Current.ConfirmationScreen;
		confirmationScreen.OnYesClicked = (UnityAction)Delegate.Combine(confirmationScreen.OnYesClicked, new UnityAction(OnQuitYesClicked));
	}

	private void OnQuitYesClicked()
	{
		SaveManager.Current.Save();
		Application.Quit();
	}

	private void OnContinueGameClicked()
	{
		Singleton<GameSession>.Current.ContinueSavedGame();
		Singleton<MainScreen>.Current.Activate(Singleton<MainScreen>.Current.RoomScreen);
		Singleton<GameSession>.Current.IsInMenu = false;
	}

	private void OnSettingsClicked()
	{
		Singleton<MainScreen>.Current.Activate(SettingsScreen);
	}

	private void OnPrestigeClicked()
	{
		Singleton<MainScreen>.Current.Activate(PrestigeScreen);
	}

	private void OnNewGameClicked()
	{
		if (SaveManager.Current.SaveData.IsGameStarted)
		{
			Singleton<MainScreen>.Current.ShowConfirmationScreen(OverWriteSaveTexts, monitorScreen);
			ConfirmationScreen confirmationScreen = Singleton<MainScreen>.Current.ConfirmationScreen;
			confirmationScreen.OnYesClicked = (UnityAction)Delegate.Combine(confirmationScreen.OnYesClicked, new UnityAction(OnOverwriteSaveYesClicked));
		}
		else
		{
			SaveManager.Current.FULL_ClearSave();
			Singleton<GameSession>.Current.StartNewGame();
			Singleton<MainScreen>.Current.Activate(Singleton<MainScreen>.Current.RoomScreen);
			Singleton<GameSession>.Current.IsInMenu = false;
		}
	}

	private void OnOverwriteSaveYesClicked()
	{
		SaveManager.Current.FULL_ClearSave();
		Singleton<GameSession>.Current.StartNewGame();
		Singleton<MainScreen>.Current.Activate(Singleton<MainScreen>.Current.RoomScreen);
		Singleton<GameSession>.Current.IsInMenu = false;
	}

	private void Update()
	{
		if (SaveManager.Current.SaveData.IsGameStarted)
		{
			ContinueGameButton.gameObject.SetActiveSmart(newState: true);
		}
		else
		{
			ContinueGameButton.gameObject.SetActiveSmart(newState: false);
		}
		EventSystem.current.SetSelectedGameObject(null);
		PrestigeButton.gameObject.SetActiveSmart(SaveManager.Current.SaveData.TicketCount > 0);
	}
}
