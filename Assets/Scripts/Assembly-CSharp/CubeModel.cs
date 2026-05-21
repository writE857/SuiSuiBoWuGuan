using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeModel : MonoBehaviour
{
	private const int PrepareBatchSize = 24;

	public GameObject Preview;

	public GameObject Broken;

	public ArtifactGroup ArtifactGroup;

	private CubePiece[] brokenPieces;

	private MeshRenderer[] previewRenderers;

	private MeshRenderer[] brokenRenderers;

	private BoxCollider[] brokenColliders;

	private Coroutine prepareBreakComponentsRoutine;

	public int PieceCount => brokenPieces?.Length ?? 0;

	private void Awake()
	{
		CacheComponents();
		SetBrokenCollidersEnabled(value: false);
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
		if (Broken != null && brokenColliders == null)
		{
			brokenColliders = Broken.GetComponentsInChildren<BoxCollider>(includeInactive: true);
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
		if (brokenPieces == null)
		{
			return;
		}
		for (int i = 0; i < brokenPieces.Length; i++)
		{
			if (brokenPieces[i] != null)
			{
				brokenPieces[i].SetIndex();
			}
		}
		int currentMaxHP = ArtifactGroup.CurrentMaxHP;
		for (int j = 0; j < brokenPieces.Length; j++)
		{
			if (brokenPieces[j] != null)
			{
				brokenPieces[j].hp = currentMaxHP;
				brokenPieces[j].maxHp = currentMaxHP;
			}
		}
	}

	public void SetTierData(ArtifactGroup group, bool prepareBroken = true)
	{
		ArtifactGroup = group;
		ApplyMaterial(Preview, group.Material);
		ApplySize();
		if (prepareBroken)
		{
			PrepareBrokenVisuals();
		}
	}

	public void PrepareBrokenVisuals()
	{
		ApplyMaterial(Broken, ArtifactGroup.Material);
		SetHP();
	}

	public IEnumerator PrepareBrokenVisualsAsync(int itemsPerFrame)
	{
		CacheComponents();
		int batchSize = Mathf.Max(1, itemsPerFrame);
		int processedThisFrame = 0;
		if (brokenRenderers != null)
		{
			Material material = ArtifactGroup.Material;
			for (int i = 0; i < brokenRenderers.Length; i++)
			{
				if (brokenRenderers[i] != null)
				{
					brokenRenderers[i].sharedMaterial = material;
				}
				processedThisFrame++;
				if (processedThisFrame >= batchSize)
				{
					processedThisFrame = 0;
					yield return null;
				}
			}
		}
		if (brokenPieces == null)
		{
			yield break;
		}
		int currentMaxHP = ArtifactGroup.CurrentMaxHP;
		for (int j = 0; j < brokenPieces.Length; j++)
		{
			CubePiece cubePiece = brokenPieces[j];
			if (cubePiece != null)
			{
				cubePiece.SetIndex();
				cubePiece.hp = currentMaxHP;
				cubePiece.maxHp = currentMaxHP;
			}
			processedThisFrame++;
			if (processedThisFrame >= batchSize)
			{
				processedThisFrame = 0;
				yield return null;
			}
		}
	}

	public void SetBrokenCollidersEnabled(bool value)
	{
		CacheComponents();
		if (brokenColliders == null)
		{
			return;
		}
		for (int i = 0; i < brokenColliders.Length; i++)
		{
			if (brokenColliders[i] != null)
			{
				brokenColliders[i].enabled = value;
			}
		}
	}

	public CubePiece[] GetBrokenPieces()
	{
		CacheComponents();
		return brokenPieces;
	}

	public BoxCollider[] GetBrokenColliders()
	{
		CacheComponents();
		return brokenColliders;
	}

	public void BeginBreakPreparation(PieceDeath template)
	{
		CacheComponents();
		if (prepareBreakComponentsRoutine != null)
		{
			return;
		}
		prepareBreakComponentsRoutine = StartCoroutine(PrepareBreakComponentsAsync(template));
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

	private System.Collections.IEnumerator PrepareBreakComponentsAsync(PieceDeath template)
	{
		List<CubePiece> list = new List<CubePiece>(brokenPieces.Length);
		for (int i = 0; i < brokenPieces.Length; i++)
		{
			CubePiece cubePiece = brokenPieces[i];
			if (cubePiece != null)
			{
				list.Add(cubePiece);
			}
		}
		list.Sort((a, b) => b.transform.position.y.CompareTo(a.transform.position.y));
		int preparedThisFrame = 0;
		for (int j = 0; j < list.Count; j++)
		{
			list[j].PrepareBreakComponents(template);
			preparedThisFrame++;
			if (preparedThisFrame >= PrepareBatchSize)
			{
				preparedThisFrame = 0;
				yield return null;
			}
		}
		prepareBreakComponentsRoutine = null;
	}
}
