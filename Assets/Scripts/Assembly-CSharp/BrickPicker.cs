using UnityEngine;

public class BrickPicker : MonoBehaviour
{
	public BrickTable BrickTable;

	public BrickPile BrickPile;

	public LayerMask LayerMask;

	private void Update()
	{
		if (Input.GetMouseButtonDown(0) && !BrickTable.IsMoving && MouseScreenPosition.TryGet(out var mousePosition) && Physics.Raycast(Camera.main.ScreenPointToRay(mousePosition), out var hitInfo, float.MaxValue, LayerMask))
		{
			Brick componentInParent = hitInfo.collider.GetComponentInParent<Brick>();
			if (componentInParent != null && !componentInParent.IsPicked)
			{
				BrickTable.AddBrick(componentInParent);
				componentInParent.IsPicked = true;
				Object.Destroy(componentInParent.transform.GetComponent<Rigidbody>());
			}
		}
	}
}
