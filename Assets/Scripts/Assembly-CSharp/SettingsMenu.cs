using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
	public List<SettingsEntryLogic> Settings = new List<SettingsEntryLogic>();

	public Button ResetProfileButton;

	public ConfirmationDialogTexts ResetProfileConfirmTexts;

	public MonitorScreen MonitorScreen;

	private void Start()
	{
		ResetProfileButton.onClick.AddListener(OnResetProfileClicked);
	}

	private void OnResetProfileClicked()
	{
		Singleton<MainScreen>.Current.ShowConfirmationScreen(ResetProfileConfirmTexts, MonitorScreen);
		ConfirmationScreen confirmationScreen = Singleton<MainScreen>.Current.ConfirmationScreen;
		confirmationScreen.OnYesClicked = (UnityAction)Delegate.Combine(confirmationScreen.OnYesClicked, new UnityAction(OnResetProfileYesClicked));
	}

	private void OnResetProfileYesClicked()
	{
		SaveManager.Current.FULL_ClearSave();
		SceneManager.LoadScene(0);
	}

	private void OnEnable()
	{
		GetComponentsInChildren(includeInactive: true, Settings);
		Settings.ForEach(delegate(SettingsEntryLogic a)
		{
			try
			{
				a.Load();
			}
			catch (Exception exception)
			{
				Debug.LogException(exception, a);
			}
		});
	}
}
