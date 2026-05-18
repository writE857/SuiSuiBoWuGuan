using System.Collections.Generic;
using UnityEngine;

public class CubeModel : MonoBehaviour
{
	public GameObject Preview;

	public GameObject Broken;

	public ArtifactGroup ArtifactGroup;

	private CubePiece[] brokenPieces;

	private MeshRenderer[] previewRenderers;

	private MeshRenderer[] brokenRenderers;

	public int PieceCount => brokenPieces?.Length ?? 0;

	private void Awake()
	{
		CacheComponents();
	}

	private void CacheComponents()
	{
		if (Preview != null && previewRenderers == null)
		{
			previewRenderers = Preview.GetComponentsInChildren<MeshRenderer>(includeInactive: true);
		}
		if (Broken != null && brokenRenderers == null)
		{
			brokenRenderers = Broken.GetComponentsInChildren<MeshRenderer>(includeInactive: true);
		}
		if (Broken != null && brokenPieces == null)
		{
			brokenPieces = Broken.GetComponentsInChildren<CubePiece>(includeInactive: true);
		}
	}

	private void ApplyMaterial(Material material)
	{
		ApplyMaterial(Preview, material);
		ApplyMaterial(Broken, material);
	}

	public void SetHP()
	{
		CacheComponents();
		for (int i = 0; i < brokenPieces.Length; i++)
		{
			brokenPieces[i].SetIndex();
		}
		int currentMaxHP = ArtifactGroup.CurrentMaxHP;
		for (int j = 0; j < brokenPieces.Length; j++)
		{
			brokenPieces[j].hp = currentMaxHP;
			brokenPieces[j].maxHp = currentMaxHP;
		}
	}

	public void SetTierData(ArtifactGroup group)
	{
		ArtifactGroup = group;
		ApplyMaterial(group.Material);
		ApplySize();
		SetHP();
	}

	private void ApplySize()
	{
		Preview.transform.localScale = ArtifactGroup.CurrentScale;
		Broken.transform.localScale = ArtifactGroup.CurrentScale;
	}

	private void ApplyMaterial(GameObject target, Material material)
	{
		CacheComponents();
		MeshRenderer[] array = ((target == Preview) ? previewRenderers : brokenRenderers);
		for (int i = 0; i < array.Length; i++)
		{
			array[i].sharedMaterial = material;
		}
	}

	public List<int> GetAlivePieceIndices()
	{
		CacheComponents();
		List<int> list = new List<int>(brokenPieces.Length);
		for (int i = 0; i < brokenPieces.Length; i++)
		{
			CubePiece cubePiece = brokenPieces[i];
			if (!(cubePiece == null))
			{
				list.Add(cubePiece.index);
			}
		}
		return list;
	}

	public void SetAlivePieces(List<int> indices)
	{
		CacheComponents();
		HashSet<int> hashSet = new HashSet<int>(indices);
		for (int i = 0; i < brokenPieces.Length; i++)
		{
			CubePiece cubePiece = brokenPieces[i];
			if (!(cubePiece == null))
			{
				cubePiece.SetIndex();
				if (!hashSet.Contains(cubePiece.index))
				{
					Object.Destroy(cubePiece.gameObject);
					brokenPieces[i] = null;
				}
			}
		}
	}
}
