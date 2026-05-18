using UnityEngine;
using UnityEngine.UI;

public class PrestigeLine : MonoBehaviour
{
	public RectTransform RectTransform;

	public Image Image;

	public PrestigeNode To;

	public PrestigeNode From;

	public CanvasGroup CanvasGroup;

	public float thickness = 2f;

	private Vector2 lastFrom;

	private Vector2 lastTo;

	public void Init()
	{
		if (To == null || From == null)
		{
			base.gameObject.SetActiveSmart(newState: false);
			return;
		}
		Vector2 anchoredPosition = From.boundsRect.anchoredPosition;
		Vector2 normalized = (To.boundsRect.anchoredPosition - anchoredPosition).normalized;
		Vector2 edgePoint = GetEdgePoint(From.boundsRect, normalized);
		Vector2 edgePoint2 = GetEdgePoint(To.boundsRect, -normalized);
		Vector2 anchoredPosition2 = (edgePoint + edgePoint2) * 0.5f;
		float x = Vector2.Distance(edgePoint, edgePoint2);
		float z = Mathf.Atan2(edgePoint2.y - edgePoint.y, edgePoint2.x - edgePoint.x) * 57.29578f;
		RectTransform.anchoredPosition = anchoredPosition2;
		RectTransform.sizeDelta = new Vector2(x, thickness);
		RectTransform.localEulerAngles = new Vector3(0f, 0f, z);
	}

	private void Update()
	{
		if (Image.color != To.CurrentColor)
		{
			Image.color = To.CurrentColor;
		}
		if (CanvasGroup.alpha != To.CurrentAlpha)
		{
			CanvasGroup.alpha = To.CurrentAlpha;
		}
		Init();
	}

	public static Vector2 GetEdgePoint(RectTransform rect, Vector2 direction)
	{
		Rect rect2 = rect.rect;
		direction.Normalize();
		float num = rect2.width * 0.5f;
		float num2 = rect2.height * 0.5f;
		float a = num / Mathf.Abs(direction.x);
		float b = num2 / Mathf.Abs(direction.y);
		float num3 = Mathf.Min(a, b);
		return rect.anchoredPosition + direction * num3;
	}
}
