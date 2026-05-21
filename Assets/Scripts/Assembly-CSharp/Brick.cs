using System.Collections;
using UnityEngine;

public class Brick : MonoBehaviour
{
	public CubeModel Instance;

	public bool IsFull = true;

	public bool IsPicked;

	public int Seed = -1;

	public LootGenerator LootGenerator;

	public Transform LootParent;

	public Rigidbody Rigidbody;

	public CubeModel CubeModel;

	public bool IsLootGenerated;

	public void ChangeState(bool _isFull)
	{
		ChangeState(_isFull, checkInitialLootOverlap: true);
	}

	public void ChangeState(bool _isFull, bool checkInitialLootOverlap)
	{
		IsFull = _isFull;
		UpdateVisuals();
		if (checkInitialLootOverlap)
		{
			CheckInitialLootOverlaps();
		}
	}

	public void CheckInitialLootOverlaps()
	{
		for (int i = 0; i < LootGenerator.Loots.Count; i++)
		{
			LootGenerator.Loots[i].CheckInitialOverlap();
		}
	}

	public void UpdateVisuals()
	{
		Instance.Preview.SetActiveSmart(IsFull);
		Instance.Broken.SetActiveSmart(!IsFull);
		if (!IsFull)
		{
			if (Rigidbody != null)
			{
				Rigidbody.isKinematic = true;
			}
			for (int i = 0; i < LootGenerator.Loots.Count; i++)
			{
				LootGenerator.Loots[i].didInit = true;
			}
			CubeModel.BeginBreakPreparation(Singleton<Hammer>.Current != null ? Singleton<Hammer>.Current.PieceDeath : null);
		}
	}

	public void Init(CubeModel cubeModel, int seed)
	{
		Seed = seed;
		Instance = cubeModel;
		Instance.transform.parent = base.transform;
		Instance.transform.Reset();
		IsFull = true;
		UpdateVisuals();
		LootGenerator = Instance.GetComponentInChildren<LootGenerator>();
		LootGenerator.ArtifactGroup = Instance.ArtifactGroup;
		LootGenerator.BoundsObject = Instance.Preview.transform;
		LootGenerator.Parent = LootParent;
		LootGenerator.BricksBoughtAtGeneration = SaveManager.Current != null ? SaveManager.Current.SaveData.BricksBought : -1;
		CubeModel = Instance;
	}

	public void GenerateLoot()
	{
		if (IsLootGenerated)
		{
			return;
		}
		UnityEngine.Random.InitState(Seed);
		LootGenerator.GenerateLoot();
		IsLootGenerated = true;
	}

	public IEnumerator GenerateLootAsync(int itemsPerFrame)
	{
		if (IsLootGenerated)
		{
			yield break;
		}
		UnityEngine.Random.InitState(Seed);
		yield return LootGenerator.GenerateLootAsync(itemsPerFrame);
		IsLootGenerated = true;
	}
}
