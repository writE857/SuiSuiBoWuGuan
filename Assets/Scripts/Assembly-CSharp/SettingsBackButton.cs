using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SettingsBackButton : MonoBehaviour
{
	public MonitorScreen mainMenu;

	public Button Button;

	public InputActionReference CancelAction;

	private void Start()
	{
		Button.onClick.AddListener(OnBackClicked);
	}

	private void Update()
	{
		if (CancelAction.action.WasPressedThisFrame() && Button.IsInteractable())
		{
			OnBackClicked();
		}
	}

	private void OnBackClicked()
	{
		Singleton<MainScreen>.Current.Activate(mainMenu);
	}
}
