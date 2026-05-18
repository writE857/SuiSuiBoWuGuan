using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ScrollToSelectedSettingsEntry : MonoBehaviour
{
	public ScrollRect scrollRect;

	private SettingsEntry lastSelected;

	private void Update()
	{
		GameObject currentSelectedGameObject = EventSystem.current.currentSelectedGameObject;
		if (currentSelectedGameObject == null)
		{
			return;
		}
		SettingsEntry componentInParent = currentSelectedGameObject.GetComponentInParent<SettingsEntry>();
		if (!(componentInParent == lastSelected))
		{
			if (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame || Mouse.current.middleButton.wasPressedThisFrame))
			{
				lastSelected = componentInParent;
				return;
			}
			ScrollTo(currentSelectedGameObject.GetComponent<RectTransform>());
			lastSelected = componentInParent;
		}
	}

	private void ScrollTo(RectTransform target)
	{
		RectTransform viewport = scrollRect.viewport;
		RectTransform content = scrollRect.content;
		Vector2 vector = content.InverseTransformPoint(target.position);
		float height = content.rect.height;
		float height2 = viewport.rect.height;
		float verticalNormalizedPosition = Mathf.Clamp01(1f - (Mathf.Abs(vector.y) - target.rect.height * 0.5f) / (height - height2));
		scrollRect.verticalNormalizedPosition = verticalNormalizedPosition;
	}
}
