using UnityEngine;
using UnityEngine.UI;

public class MobilePauseButton : MonoBehaviour
{
	public Button Button;

	public CanvasGroup VisibilityGroup;

	public RectTransform RectTransform;

	public Canvas ParentCanvas;

	public Vector2 SafeAreaPadding = new Vector2(32f, 32f);

	public bool HideWhenMenuOpen = true;

	public bool AutoBindButton;

	public bool ApplySafeAreaPadding;

	private void Awake()
	{
		if (Button == null)
		{
			Button = GetComponent<Button>();
		}
		if (VisibilityGroup == null)
		{
			VisibilityGroup = GetComponent<CanvasGroup>();
		}
		if (RectTransform == null)
		{
			RectTransform = transform as RectTransform;
		}
		if (ParentCanvas == null)
		{
			ParentCanvas = GetComponentInParent<Canvas>();
		}
	}

	private void OnEnable()
	{
		if (AutoBindButton && Button != null)
		{
			Button.onClick.AddListener(OnPauseClicked);
		}
		RefreshVisibility();
		if (ApplySafeAreaPadding)
		{
			ApplySafeArea();
		}
	}

	private void OnDisable()
	{
		if (AutoBindButton && Button != null)
		{
			Button.onClick.RemoveListener(OnPauseClicked);
		}
	}

	private void Update()
	{
		RefreshVisibility();
		if (ApplySafeAreaPadding)
		{
			ApplySafeArea();
		}
	}

	public void OnPauseClicked()
	{
		GameSession session = Singleton<GameSession>.Current;
		MainScreen mainScreen = Singleton<MainScreen>.Current;
		if (session == null || mainScreen == null || !session.IsGameStarted)
		{
			return;
		}
		PrestigeScreen prestigeScreen = Singleton<PrestigeScreen>.Current;
		if (prestigeScreen != null && prestigeScreen.IsShoppingMode)
		{
			return;
		}
		ArtifactOverlayDisplay overlayDisplay = Singleton<ArtifactOverlayDisplay>.Current;
		if (overlayDisplay != null && overlayDisplay.IsOpen)
		{
			overlayDisplay.Hide();
			return;
		}
		if (!session.IsInMenu)
		{
			session.IsInMenu = true;
			mainScreen.Activate(mainScreen.MainScreenObject);
			SaveManager.Current.Save();
		}
	}

	private void RefreshVisibility()
	{
		if (!Application.isPlaying)
		{
			return;
		}

		GameSession session = Singleton<GameSession>.Current;
		bool visible = session != null && session.IsGameStarted;
		if (visible && HideWhenMenuOpen && session.IsInMenu)
		{
			visible = false;
		}
		SetVisible(visible);
	}

	private void SetVisible(bool visible)
	{
		if (VisibilityGroup == null)
		{
			return;
		}
		VisibilityGroup.alpha = visible ? 1f : 0f;
		VisibilityGroup.interactable = visible;
		VisibilityGroup.blocksRaycasts = visible;
	}

	private void ApplySafeArea()
	{
		if (RectTransform == null)
		{
			return;
		}
		Rect safeArea = Screen.safeArea;
		float scaleFactor = ParentCanvas != null ? Mathf.Max(ParentCanvas.scaleFactor, 0.0001f) : 1f;
		float rightInset = Mathf.Max(Screen.width - safeArea.xMax, 0f) / scaleFactor;
		float topInset = Mathf.Max(Screen.height - safeArea.yMax, 0f) / scaleFactor;
		RectTransform.anchoredPosition = new Vector2(-(SafeAreaPadding.x + rightInset), -(SafeAreaPadding.y + topInset));
	}
}
