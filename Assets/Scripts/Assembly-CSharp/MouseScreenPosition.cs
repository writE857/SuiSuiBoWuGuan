using UnityEngine;
using UnityEngine.InputSystem;

public static class MouseScreenPosition
{
	public static bool TryGet(out Vector3 position)
	{
		if (TryGetLegacyMouse(out position))
		{
			return true;
		}
		return TryGetInputSystem(out position);
	}

	public static bool TryGet(InputActionReference actionReference, Camera camera, out Vector3 position)
	{
		if (TryGetLegacyMouse(out position) && IsInsideCamera(camera, position))
		{
			return true;
		}
		if (TryGetFromPointAction(actionReference, out position) && IsInsideCamera(camera, position))
		{
			return true;
		}
		if (TryGetInputSystem(out position) && IsInsideCamera(camera, position))
		{
			return true;
		}
		position = default;
		return false;
	}

	public static bool TryGetPosition(InputActionReference actionReference, out Vector3 position)
	{
		if (TryGetFromPointAction(actionReference, out position))
		{
			return true;
		}
		if (TryGetInputSystem(out position))
		{
			return true;
		}
		return TryGetLegacyMouse(out position);
	}

	public static bool TryGetInputSystem(out Vector3 position)
	{
		Pointer pointer = Pointer.current;
		if (pointer != null)
		{
			Vector2 pointerPosition = pointer.position.ReadValue();
			position = new Vector3(pointerPosition.x, pointerPosition.y, 0f);
			if (IsValid(position))
			{
				return true;
			}
		}
		if (Mouse.current == null)
		{
			position = default;
			return false;
		}
		Vector2 mousePosition = Mouse.current.position.ReadValue();
		position = new Vector3(mousePosition.x, mousePosition.y, 0f);
		return IsValid(position);
	}

	private static bool TryGetFromPointAction(InputActionReference actionReference, out Vector3 position)
	{
		InputAction inputAction = actionReference != null ? actionReference.action : null;
		InputAction pointAction = inputAction?.actionMap?.FindAction("Point", false);
		if (pointAction == null || !pointAction.enabled)
		{
			position = default;
			return false;
		}
		Vector2 value = pointAction.ReadValue<Vector2>();
		position = new Vector3(value.x, value.y, 0f);
		return IsValid(position);
	}

	private static bool TryGetLegacyMouse(out Vector3 position)
	{
		position = Input.mousePosition;
		return IsValid(position);
	}

	private static bool IsInsideCamera(Camera camera, Vector3 position)
	{
		if (camera == null || !IsValid(position))
		{
			return false;
		}
		return camera.pixelRect.Contains(new Vector2(position.x, position.y));
	}

	public static bool IsValid(Vector3 position)
	{
		return IsFinite(position.x)
			&& IsFinite(position.y)
			&& IsFinite(position.z);
	}

	private static bool IsFinite(float value)
	{
		return !float.IsNaN(value) && !float.IsInfinity(value);
	}
}
