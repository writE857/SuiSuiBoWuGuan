using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ConfirmationScreen : MonoBehaviour
{
	public MonitorScreen monitorScreen;

	public GameObject ConfirmationObject;

	public TMP_Text titleText;

	public TMP_Text yesText;

	public TMP_Text noText;

	public Button yesButton;

	public Button noButton;

	public UnityAction OnYesClicked;

	public UnityAction OnNoClicked;

	private MonitorScreen originScreen;

	private void Start()
	{
		yesButton.onClick.AddListener(YesClicked);
		noButton.onClick.AddListener(NoClicked);
		originScreen = null;
	}

	public void Show(ConfirmationDialogTexts confirmationDialogTexts, MonitorScreen originScreen)
	{
		this.originScreen = originScreen;
		Singleton<MainScreen>.Current.Activate(monitorScreen);
		ConfirmationObject.SetActive(value: false);
		titleText.text = confirmationDialogTexts.Title;
		yesText.text = confirmationDialogTexts.Yes;
		noText.text = confirmationDialogTexts.No;
		ConfirmationObject.SetActive(value: true);
	}

	private void YesClicked()
	{
		GoBackTo(originScreen);
		OnYesClicked?.Invoke();
		OnYesClicked = null;
		OnNoClicked = null;
	}

	private void NoClicked()
	{
		OnNoClicked?.Invoke();
		OnYesClicked = null;
		OnNoClicked = null;
		GoBackTo(originScreen);
	}

	private void GoBackTo(MonitorScreen screen)
	{
		Singleton<MainScreen>.Current.Activate(screen);
		originScreen = null;
	}
}
