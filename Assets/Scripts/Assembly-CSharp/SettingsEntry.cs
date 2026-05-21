using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class SettingsEntry : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, IPointerDownHandler, IPointerClickHandler, ISelectHandler, IDeselectHandler
{
	public int SelectedIndex;

	public UnityAction<int> OnSelectedIndexChanged;

	public CheckMark SettingsDotPrefab;

	public Transform SettingsDotParent;

	public TMP_Text ValueText;

	public CanvasGroup LeftCG;

	public CanvasGroup RightCG;

	public CanvasGroup DotsCG;

	public EventTriggerRelay LeftButton;

	public EventTriggerRelay RightButton;

	public bool IsFocused;

	public List<string> Options = new List<string>();

	public List<CheckMark> Dots = new List<CheckMark>();

	public AudioResource SelectedSFX;

	public AudioResource ValueChangeSFX;

	public float slideInterval = 0.15f;

	private float nextSlide;

	public InputActionReference NavigationAction;

	private void Start()
	{
		EventTriggerRelay leftButton = LeftButton;
		leftButton.onPointerClick = (UnityAction)Delegate.Combine(leftButton.onPointerClick, new UnityAction(LeftClicked));
		EventTriggerRelay rightButton = RightButton;
		rightButton.onPointerClick = (UnityAction)Delegate.Combine(rightButton.onPointerClick, new UnityAction(RightClicked));
		SetBlurred();
	}

	public void SetData(List<string> options, int startingIndex)
	{
		Options = options ?? new List<string>();
		SettingsDotParent.Clear();
		Dots.Clear();
		if (Options.Count == 0)
		{
			SelectedIndex = -1;
			ValueText.text = string.Empty;
			return;
		}
		SelectedIndex = Mathf.Clamp(startingIndex, 0, Options.Count - 1);
		ValueText.text = Options[SelectedIndex];
		for (int i = 0; i < Options.Count; i++)
		{
			CheckMark checkMark = UnityEngine.Object.Instantiate(SettingsDotPrefab, SettingsDotParent);
			checkMark.Selected.SetActiveSmart(newState: false);
			if (i == SelectedIndex)
			{
				checkMark.Selected.SetActiveSmart(newState: true);
			}
		}
	}

	private void Update()
	{
		if (!IsFocused)
		{
			return;
		}
		if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
		{
			nextSlide = Time.time + slideInterval;
			LeftClicked();
		}
		else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
		{
			nextSlide = Time.time + slideInterval;
			RightClicked();
		}
		else if (nextSlide < Time.time)
		{
			if (NavigationAction.action.ReadValue<Vector2>().x < 0f)
			{
				nextSlide = Time.time + slideInterval;
				LeftClicked();
			}
			if (NavigationAction.action.ReadValue<Vector2>().x > 0f)
			{
				nextSlide = Time.time + slideInterval;
				RightClicked();
			}
		}
	}

	private void LeftClicked()
	{
		if (Options.Count == 0)
		{
			return;
		}
		SelectedIndex--;
		SelectedIndex = Mathf.Clamp(SelectedIndex, 0, Options.Count - 1);
		OnSelectedIndexChanged?.Invoke(SelectedIndex);
		SetData(Options, SelectedIndex);
		Singleton<AudioPool>.Current.Play(ValueChangeSFX, base.transform.position);
	}

	private void RightClicked()
	{
		if (Options.Count == 0)
		{
			return;
		}
		SelectedIndex++;
		SelectedIndex = Mathf.Clamp(SelectedIndex, 0, Options.Count - 1);
		OnSelectedIndexChanged?.Invoke(SelectedIndex);
		SetData(Options, SelectedIndex);
		Singleton<AudioPool>.Current.Play(ValueChangeSFX, base.transform.position);
	}

	public void OnSelect(BaseEventData eventData)
	{
		SetFocused();
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		SelectThis(eventData);
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		SelectThis(eventData);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if (!IsSelected())
		{
			SetBlurred();
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		SetFocused();
	}

	public void OnDeselect(BaseEventData eventData)
	{
		SetBlurred();
	}

	private void SelectThis(BaseEventData eventData)
	{
		EventSystem eventSystem = EventSystem.current;
		if (eventSystem != null && eventSystem.currentSelectedGameObject != base.gameObject)
		{
			eventSystem.SetSelectedGameObject(base.gameObject, eventData);
			return;
		}
		SetFocused();
	}

	private bool IsSelected()
	{
		EventSystem eventSystem = EventSystem.current;
		return eventSystem != null && eventSystem.currentSelectedGameObject == base.gameObject;
	}

	private void SetFocused()
	{
		if (!IsFocused)
		{
			Singleton<AudioPool>.Current.Play(SelectedSFX, base.transform.position);
		}
		IsFocused = true;
		LeftCG.alpha = 1f;
		LeftCG.blocksRaycasts = true;
		LeftCG.interactable = true;
		RightCG.alpha = 1f;
		RightCG.blocksRaycasts = true;
		RightCG.interactable = true;
		DotsCG.alpha = 1f;
	}

	private void SetBlurred()
	{
		IsFocused = false;
		LeftCG.alpha = 0f;
		LeftCG.blocksRaycasts = false;
		LeftCG.interactable = false;
		RightCG.alpha = 0f;
		RightCG.blocksRaycasts = false;
		RightCG.interactable = false;
		DotsCG.alpha = 0f;
	}
}
