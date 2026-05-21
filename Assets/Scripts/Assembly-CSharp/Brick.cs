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

	public int initialLootOverlapChecksPerFrame = 2;

	private Coroutine initialLootOverlapRoutine;

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
		if (initialLootOverlapRoutine != null)
		{
			StopCoroutine(initialLootOverlapRoutine);
		}
		initialLootOverlapRoutine = StartCoroutine(CheckInitialLootOverlapsAsync());
	}

	private IEnumerator CheckInitialLootOverlapsAsync()
	{
		yield return new WaitForFixedUpdate();
		System.Collections.Generic.List<Loot> loots = new System.Collections.Generic.List<Loot>(LootGenerator.Loots);
		int checksThisFrame = 0;
		for (int i = 0; i < loots.Count; i++)
		{
			Loot loot = loots[i];
			if (loot != null)
			{
				loot.CheckInitialOverlapNow();
			}
			checksThisFrame++;
			if (checksThisFrame >= initialLootOverlapChecksPerFrame)
			{
				checksThisFrame = 0;
				yield return null;
			}
		}
		initialLootOverlapRoutine = null;
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
