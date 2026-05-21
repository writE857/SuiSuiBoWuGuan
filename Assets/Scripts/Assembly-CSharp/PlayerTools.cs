using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerTools : MonoBehaviour
{
	private const float MaxVisualLift = 0.035f;

	public Hammer Hammer;

	public LootMagnet LootMagnet;

	public LayerMask LayerMask;

	public LayerMask LayerMask2;

	public SphereCollider SphereCollider;

	public GameObject VisualObjects;

	public InputActionReference ClickAction;

	private int lastFrame;

	public bool HasBrick => Singleton<BrickTable>.Current != null && Singleton<BrickTable>.Current.CurrentBrick != null;

	private void OnEnable()
	{
		Hammer.IsHitting = false;
		ClickAction?.action?.Enable();
		ClickAction?.action?.actionMap?.FindAction("Point", false)?.Enable();
	}

	private void Update()
	{
		if (lastFrame != Time.frameCount)
		{
			lastFrame = Time.frameCount;
			InputAction clickAction = ClickAction != null ? ClickAction.action : null;
			bool clickStarted = (clickAction != null && clickAction.WasPressedThisFrame()) || Input.GetMouseButtonDown(0);
			bool clickPressed = (clickAction != null && clickAction.IsPressed()) || Input.GetMouseButton(0);
			bool canUseTool = !IsPointerOverUI();
			PlaceOnSurface();
			VisualObjects.SetActiveSmart(!Singleton<GameSession>.Current.IsInMenu);
			if (LootMagnet != null)
			{
				LootMagnet.IsPulling = canUseTool && clickPressed;
			}
			if (canUseTool && clickStarted)
			{
				Hammer.Use();
			}
			else
			{
				Hammer.IsHitting = canUseTool && clickPressed;
			}
		}
	}

	private bool IsPointerOverUI()
	{
		return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
	}

	private void PlaceOnSurface()
	{
		Camera mainCamera = Camera.main;
		if (mainCamera == null || !MouseScreenPosition.TryGet(out var mousePosition))
		{
			return;
		}
		Ray cameraRay = mainCamera.ScreenPointToRay(mousePosition);
		Vector3 visualPosition = base.transform.position;
		int visualMask = LayerMask.value & ~LayerMask2.value;
		if (visualMask != 0 && Physics.Raycast(cameraRay, out var hitInfo, float.MaxValue, visualMask, QueryTriggerInteraction.Ignore))
		{
			visualPosition = hitInfo.point;
		}
		else if (new Plane(Vector3.up, base.transform.position).Raycast(cameraRay, out var enter))
		{
			visualPosition = cameraRay.GetPoint(enter);
		}
		base.transform.position = visualPosition;
		Vector3 damageOrigin = visualPosition;
		if (Physics.Raycast(cameraRay, out hitInfo, float.MaxValue, LayerMask, QueryTriggerInteraction.Ignore))
		{
			damageOrigin = hitInfo.point;
		}
		Ray ray = new Ray(damageOrigin + Vector3.up * 1f, Vector3.down * 3f);
		if (Physics.SphereCast(ray, SphereCollider.WorldRadius(), out hitInfo, float.MaxValue, LayerMask2, QueryTriggerInteraction.Ignore))
		{
			damageOrigin = ray.origin + ray.direction * (hitInfo.distance + SphereCollider.WorldRadius());
		}
		base.transform.position = Vector3.Lerp(visualPosition, damageOrigin, Mathf.Clamp01(MaxVisualLift / Mathf.Max(Vector3.Distance(visualPosition, damageOrigin), 0.0001f)));
		Hammer.SetDamageOrigin(damageOrigin);
	}
}
