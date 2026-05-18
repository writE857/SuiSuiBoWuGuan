using UnityEngine;

public class TrackMousePosition : MonoBehaviour
{
	private Vector3 defaultPosition;

	public Vector3 MouseAimPos;

	public float followRatio = 0.1f;

	public float maxDistance = 0.25f;

	private void Start()
	{
		defaultPosition = base.transform.position;
	}

	private void Update()
	{
		if (!MouseScreenPosition.TryGet(out var mousePosition))
		{
			return;
		}
		Ray ray = Camera.main.ScreenPointToRay(mousePosition);
		if (new Plane(Vector3.up, defaultPosition).Raycast(ray, out var enter))
		{
			MouseAimPos = ray.GetPoint(enter);
			Vector3 vector = Vector3.Lerp(defaultPosition, MouseAimPos, followRatio);
			Vector3 vector2 = vector - defaultPosition;
			vector = defaultPosition + Vector3.ClampMagnitude(vector2, maxDistance);
			base.transform.position = vector;
		}
	}
}
