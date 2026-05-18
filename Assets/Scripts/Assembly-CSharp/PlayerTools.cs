using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerTools : MonoBehaviour
{
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
			bool canHit = HasBrick && !IsPointerOverUI();
			PlaceOnSurface();
			VisualObjects.SetActiveSmart(!Singleton<GameSession>.Current.IsInMenu);
			LootMagnet.IsPulling = true;
			if (canHit && clickStarted)
			{
				Hammer.Use();
			}
			else
			{
				Hammer.IsHitting = canHit && clickPressed;
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
		if (mainCamera == null || !MouseScreenPosition.TryGetPosition(ClickAction, out var mousePosition))
		{
			return;
		}
		Ray cameraRay = mainCamera.ScreenPointToRay(mousePosition);
		if (Physics.Raycast(cameraRay, out var hitInfo, float.MaxValue, LayerMask, QueryTriggerInteraction.Ignore))
		{
			base.transform.position = hitInfo.point;
		}
		else if (new Plane(Vector3.up, base.transform.position).Raycast(cameraRay, out var enter))
		{
			base.transform.position = cameraRay.GetPoint(enter);
		}
		Ray ray = new Ray(base.transform.position + Vector3.up * 1f, Vector3.down * 3f);
		if (Physics.SphereCast(ray, SphereCollider.WorldRadius(), out hitInfo, float.MaxValue, LayerMask2, QueryTriggerInteraction.Ignore))
		{
			base.transform.position = ray.origin + ray.direction * (hitInfo.distance + SphereCollider.WorldRadius());
		}
	}
}
