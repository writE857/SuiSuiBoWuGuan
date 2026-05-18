using UnityEngine;
using UnityEngine.EventSystems;

public class UIDragger : MonoBehaviour, IDragHandler, IEventSystemHandler, IScrollHandler, IBeginDragHandler
{
	public RectTransform Target;

	public float MinScale = 0.5f;

	public float MaxScale = 1.5f;

	public float zoomStep = 0.1f;

	public float zoomSpeed = 10f;

	private float targetScale = 1f;

	private float currentScale = 1f;

	private Canvas Canvas;

	private Vector3 lastWorldPos;

	private void Start()
	{
		Canvas = GetComponentInParent<Canvas>();
	}

	private void OnEnable()
	{
		Target.anchoredPosition = Vector3.zero;
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		RectTransformUtility.ScreenPointToWorldPointInRectangle(Canvas.transform as RectTransform, eventData.position, Camera.main, out lastWorldPos);
	}

	public void OnDrag(PointerEventData eventData)
	{
		if (RectTransformUtility.ScreenPointToWorldPointInRectangle(Canvas.transform as RectTransform, eventData.position, Camera.main, out var worldPoint))
		{
			Vector3 vector = worldPoint - lastWorldPos;
			lastWorldPos = worldPoint;
			Vector3 vector2 = Canvas.transform.InverseTransformVector(vector);
			Target.anchoredPosition += (Vector2)vector2;
		}
	}

	public void OnScroll(PointerEventData eventData)
	{
		float y = eventData.scrollDelta.y;
		if (!Mathf.Approximately(y, 0f))
		{
			float num = targetScale;
			targetScale += y * zoomStep;
			targetScale = Mathf.Round(targetScale / zoomStep) * zoomStep;
			targetScale = Mathf.Clamp(targetScale, MinScale, MaxScale);
			float num2 = targetScale / num;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(Target, eventData.position, (Canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : Camera.main, out var localPoint);
			Target.anchoredPosition += localPoint * (1f - num2);
		}
	}

	private void Update()
	{
		currentScale = targetScale;
		Target.localScale = Vector3.one * currentScale;
	}
}
