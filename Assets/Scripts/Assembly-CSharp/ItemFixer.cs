using UnityEngine;

public class ItemFixer : MonoBehaviour
{
	private Rigidbody Rigidbody;

	private MeshCollider MeshCollider;

	public float maxVelocity = 1f;

	public int PreviewLayerIndex;

	public int SolidLayerIndex;

	public CubeModel CubeModel;

	private void SetItUp()
	{
		CubeModel = GetComponent<CubeModel>();
		if (CubeModel == null)
		{
			CubeModel = base.gameObject.AddComponent<CubeModel>();
		}
		foreach (Transform item in base.transform.Children())
		{
			if (item.name.ToLower().Contains("preview"))
			{
				CubeModel.Preview = item.gameObject;
			}
			if (item.name.ToLower().Contains("content"))
			{
				CubeModel.Broken = item.gameObject;
			}
		}
		PreviewLayerIndex = LayerMask.NameToLayer("Preview");
		SolidLayerIndex = LayerMask.NameToLayer("Solid");
		SetLayerRecursive(CubeModel.Preview.transform, PreviewLayerIndex);
		SetLayerRecursive(CubeModel.Broken.transform, SolidLayerIndex);
		AddBoxColliders(CubeModel.Broken.transform);
		AddPieces(CubeModel.Broken.transform);
		AddSingleBoxCollider(CubeModel.Preview.transform);
	}

	private void SetLayerRecursive(Transform transform, int layerIndex)
	{
		transform.gameObject.layer = layerIndex;
		transform.Children().ForEach(delegate(Transform a)
		{
			a.gameObject.layer = layerIndex;
		});
	}

	private void AddSingleBoxCollider(Transform target)
	{
		BoxCollider component = target.GetComponent<BoxCollider>();
		if (component != null)
		{
			Object.DestroyImmediate(component);
		}
		if (component == null)
		{
			component = target.gameObject.AddComponent<BoxCollider>();
		}
	}

	private void AddBoxColliders(Transform target)
	{
		Collider component = target.GetComponent<Collider>();
		if (component != null)
		{
			Object.DestroyImmediate(component);
		}
		BoxCollider[] componentsInChildren = target.GetComponentsInChildren<BoxCollider>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			Object.DestroyImmediate(componentsInChildren[i]);
		}
		MeshRenderer[] componentsInChildren2 = target.GetComponentsInChildren<MeshRenderer>();
		for (int j = 0; j < componentsInChildren2.Length; j++)
		{
			if (componentsInChildren2[j].GetComponent<BoxCollider>() == null)
			{
				componentsInChildren2[j].gameObject.AddComponent<BoxCollider>();
			}
		}
	}

	private void AddPieces(Transform target)
	{
		MeshRenderer[] componentsInChildren = target.GetComponentsInChildren<MeshRenderer>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			PieceDeath componentInChildren = target.GetComponentInChildren<PieceDeath>(includeInactive: true);
			if (componentInChildren != null)
			{
				Object.DestroyImmediate(componentInChildren);
			}
			if (componentsInChildren[i].GetComponent<CubePiece>() == null)
			{
				componentsInChildren[i].gameObject.AddComponent<CubePiece>();
			}
		}
	}
}
