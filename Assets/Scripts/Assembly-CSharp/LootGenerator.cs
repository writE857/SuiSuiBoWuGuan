using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LootGenerator : Singleton<LootGenerator>
{
	public ArtifactGroup ArtifactGroup;

	public Transform BoundsObject;

	public Transform Parent;

	public MeshRenderer MeshRenderer;

	internal List<Loot> Loots = new List<Loot>();

	internal List<Artifact> Artifacts = new List<Artifact>();

	private List<Loot> preLoots = new List<Loot>();

	public Vector3 BoundsMargin;

	public Bounds placementBounds;

	public Vector2 LootScale = new Vector2(0.5f, 1f);

	public Vector2 LootValueRange = new Vector2(0.5f, 2f);

	public int StartAllLootCount;

	public int StartCollectibleCount;

	[System.NonSerialized]
	public int BricksBoughtAtGeneration = -1;

	private List<Vector3> PointsLeft = new List<Vector3>();

	public Vector3 artifactRotationRange = new Vector3(25f, 360f, 25f);

	public Vector3 coinRotationRange = new Vector3(45f, 360f, 45f);

	public Vector3 ticketRotationRange = new Vector3(10f, 360f, 10f);

	public Vector3 mineralRotationRange = new Vector3(360f, 360f, 360f);

	public int gridX = 6;

	public int gridY = 3;

	public int gridZ = 4;

	[Tooltip("0 = no randomness, 1 = full cell size")]
	public float randomOffset = 0.5f;

	[Header("Prestige")]
	public PrestigeSkill LuckyCoinChanceSkill;

	public PrestigeSkill TicketChanceSkill;

	public PrestigeSkill DoubleCoinSpawnSkill;

	public PrestigeSkill MinMineralsSkill;

	public PrestigeSkill MaxMineralsSkill;

	public PrestigeSkill TicketDropCount;

	public PrestigeSkill TicketFromStart;

	public PrestigeSkill ArtifactCountSkill;

	public int AllLootCount => Loots.Count;

	public int CollectibleLootCount => Artifacts.Count;

	public float LootValue => Loots.Sum((Loot a) => a.FinalValue);

	public void GenerateLoot()
	{
		MeshRenderer = BoundsObject.GetComponent<MeshRenderer>();
		placementBounds = MeshRenderer.bounds;
		Vector3 size = placementBounds.size;
		size -= BoundsMargin;
		placementBounds.size = size;
		Parent.Clear();
		Loots.Clear();
		Artifacts.Clear();
		preLoots.Clear();
		PointsLeft = GeneratePoints();
		AddCollectionLoot();
		AddRandomLoot();
		AddLuckyCoin();
		int num = Mathf.Min(Random.Range(1, Mathf.RoundToInt(TicketDropCount.FinalValue) + 1), Random.Range(1, Mathf.RoundToInt(TicketDropCount.FinalValue) + 1));
		for (int i = 0; i < num; i++)
		{
			AddTicket();
		}
		StartAllLootCount = Loots.Count;
		Artifacts.AddRange(from a in Loots
			where a.Artifact != null
			select a.Artifact);
		StartCollectibleCount = Artifacts.Count;
	}

	public IEnumerator GenerateLootAsync(int itemsPerFrame)
	{
		itemsPerFrame = Mathf.Max(1, itemsPerFrame);
		MeshRenderer = BoundsObject.GetComponent<MeshRenderer>();
		placementBounds = MeshRenderer.bounds;
		Vector3 size = placementBounds.size;
		size -= BoundsMargin;
		placementBounds.size = size;
		Parent.Clear();
		Loots.Clear();
		Artifacts.Clear();
		preLoots.Clear();
		PointsLeft = GeneratePoints();
		yield return AddCollectionLootAsync(itemsPerFrame);
		yield return AddRandomLootAsync(itemsPerFrame);
		yield return AddLuckyCoinAsync(itemsPerFrame);
		int num = Mathf.Min(Random.Range(1, Mathf.RoundToInt(TicketDropCount.FinalValue) + 1), Random.Range(1, Mathf.RoundToInt(TicketDropCount.FinalValue) + 1));
		int spawnedThisFrame = 0;
		for (int i = 0; i < num; i++)
		{
			AddTicket();
			spawnedThisFrame++;
			if (spawnedThisFrame >= itemsPerFrame)
			{
				spawnedThisFrame = 0;
				yield return null;
			}
		}
		StartAllLootCount = Loots.Count;
		for (int j = 0; j < Loots.Count; j++)
		{
			if (Loots[j].Artifact != null)
			{
				Artifacts.Add(Loots[j].Artifact);
			}
		}
		StartCollectibleCount = Artifacts.Count;
	}

	private IEnumerator AddCollectionLootAsync(int itemsPerFrame)
	{
		List<Artifact> availableArtifacts = ArtifactGroup.AvailableArtifacts;
		if (availableArtifacts.Count == 0)
		{
			yield break;
		}
		int minInclusive = 1;
		int num = 3;
		num += Mathf.RoundToInt(ArtifactCountSkill.FinalValue);
		int num2 = Mathf.Min(Random.Range(minInclusive, num + 1), Random.Range(minInclusive, num) + 1);
		int spawnedThisFrame = 0;
		for (int i = 0; i < num2; i++)
		{
			Artifact artifact = Object.Instantiate(availableArtifacts.GetRandom(), Parent);
			artifact.Loot.SetItUp();
			preLoots.Add(artifact.Loot);
			Place(artifact.transform, artifactRotationRange);
			Loots.Add(artifact.Loot);
			spawnedThisFrame++;
			if (spawnedThisFrame >= itemsPerFrame)
			{
				spawnedThisFrame = 0;
				yield return null;
			}
		}
	}

	private IEnumerator AddRandomLootAsync(int itemsPerFrame)
	{
		if (ArtifactGroup.HighestLoot == null)
		{
			yield break;
		}
		int num = Singleton<GameResources>.Current.Loots.Entries.IndexOf(ArtifactGroup.HighestLoot);
		num += ArtifactGroup.MaxLootTierExtra;
		List<Loot> list = new List<Loot>();
		int minLootTierExtra = ArtifactGroup.MinLootTierExtra;
		num = Mathf.Clamp(num, 0, Singleton<GameResources>.Current.Loots.Entries.Count);
		for (int i = Mathf.Clamp(minLootTierExtra, 0, num); i <= num; i++)
		{
			Loot loot = Singleton<GameResources>.Current.Loots.Entries.ElementAtOrDefault(i);
			if (loot == null)
			{
				Debug.LogError(ArtifactGroup);
			}
			else
			{
				list.Add(loot);
			}
		}
		int num2 = ArtifactGroup.MineralCount.ElementAtOrDefault(ArtifactGroup.Level - 1);
		int num3 = num2;
		num2 += Mathf.RoundToInt(MinMineralsSkill.FinalValue) + ArtifactGroup.ExtraMinLootCount;
		num3 += Mathf.RoundToInt(MaxMineralsSkill.FinalValue) + ArtifactGroup.ExtraMaxLootCount;
		int num4 = Mathf.Min(Random.Range(num2, num3 + 1), Random.Range(num2, num3) + 1);
		int spawnedThisFrame = 0;
		for (int j = 0; j < num4; j++)
		{
			Loot loot2 = Object.Instantiate(list.GetRandom(), Parent);
			loot2.SetItUp();
			float value = Random.value;
			float scale = Mathf.Lerp(LootScale.x, LootScale.y, value);
			float valueScale = Mathf.Lerp(LootValueRange.x, LootValueRange.y, value);
			loot2.SetValue(scale, valueScale);
			preLoots.Add(loot2);
			Place(loot2.transform, mineralRotationRange);
			Loots.Add(loot2);
			spawnedThisFrame++;
			if (spawnedThisFrame >= itemsPerFrame)
			{
				spawnedThisFrame = 0;
				yield return null;
			}
		}
	}

	private IEnumerator AddLuckyCoinAsync(int itemsPerFrame)
	{
		float value = Random.value;
		float num = LuckyCoinChanceSkill.FinalValue / 100f;
		num *= ArtifactGroup.CoinChanceMultiplier;
		if (value > num)
		{
			yield break;
		}
		int num2 = ((!(Random.value < DoubleCoinSpawnSkill.FinalValue / 100f)) ? 1 : 2);
		int spawnedThisFrame = 0;
		for (int i = 0; i < num2; i++)
		{
			Loot loot = Object.Instantiate(Singleton<GameResources>.Current.Loots.CoinLoot, Parent);
			loot.SetItUp();
			preLoots.Add(loot);
			Place(loot.transform, coinRotationRange);
			Loots.Add(loot);
			spawnedThisFrame++;
			if (spawnedThisFrame >= itemsPerFrame)
			{
				spawnedThisFrame = 0;
				yield return null;
			}
		}
	}

	private void OnDrawGizmosSelected()
	{
		placementBounds = MeshRenderer.bounds;
		Vector3 size = placementBounds.size;
		size -= BoundsMargin;
		placementBounds.size = size;
		Gizmos.DrawWireCube(placementBounds.center, placementBounds.size);
	}

	private void Update()
	{
		for (int i = Loots.Count - 1; i >= 0; i--)
		{
			Loot loot = Loots[i];
			if (loot == null || loot.IsFree)
			{
				Loots.RemoveAt(i);
			}
		}
		for (int j = Artifacts.Count - 1; j >= 0; j--)
		{
			Artifact artifact = Artifacts[j];
			if (artifact == null || artifact.Loot == null || artifact.Loot.IsFree)
			{
				Artifacts.RemoveAt(j);
			}
		}
	}

	private void AddCollectionLoot()
	{
		List<Artifact> availableArtifacts = ArtifactGroup.AvailableArtifacts;
		if (availableArtifacts.Count != 0)
		{
			int minInclusive = 1;
			int num = 3;
			num += Mathf.RoundToInt(ArtifactCountSkill.FinalValue);
			int num2 = Mathf.Min(Random.Range(minInclusive, num + 1), Random.Range(minInclusive, num) + 1);
			for (int i = 0; i < num2; i++)
			{
				Artifact artifact = Object.Instantiate(availableArtifacts.GetRandom(), Parent);
				artifact.Loot.SetItUp();
				preLoots.Add(artifact.Loot);
				Place(artifact.transform, artifactRotationRange);
				Loots.Add(artifact.Loot);
			}
		}
	}

	private void AddRandomLoot()
	{
		if (ArtifactGroup.HighestLoot == null)
		{
			return;
		}
		int num = Singleton<GameResources>.Current.Loots.Entries.IndexOf(ArtifactGroup.HighestLoot);
		num += ArtifactGroup.MaxLootTierExtra;
		List<Loot> list = new List<Loot>();
		int minLootTierExtra = ArtifactGroup.MinLootTierExtra;
		num = Mathf.Clamp(num, 0, Singleton<GameResources>.Current.Loots.Entries.Count);
		for (int i = Mathf.Clamp(minLootTierExtra, 0, num); i <= num; i++)
		{
			Loot loot = Singleton<GameResources>.Current.Loots.Entries.ElementAtOrDefault(i);
			if (loot == null)
			{
				Debug.LogError(ArtifactGroup);
			}
			else
			{
				list.Add(loot);
			}
		}
		int num2 = ArtifactGroup.MineralCount.ElementAtOrDefault(ArtifactGroup.Level - 1);
		int num3 = num2;
		num2 += Mathf.RoundToInt(MinMineralsSkill.FinalValue) + ArtifactGroup.ExtraMinLootCount;
		num3 += Mathf.RoundToInt(MaxMineralsSkill.FinalValue) + ArtifactGroup.ExtraMaxLootCount;
		int num4 = Mathf.Min(Random.Range(num2, num3 + 1), Random.Range(num2, num3) + 1);
		for (int j = 0; j < num4; j++)
		{
			Loot loot2 = Object.Instantiate(list.GetRandom(), Parent);
			loot2.SetItUp();
			float value = Random.value;
			float scale = Mathf.Lerp(LootScale.x, LootScale.y, value);
			float valueScale = Mathf.Lerp(LootValueRange.x, LootValueRange.y, value);
			loot2.SetValue(scale, valueScale);
			preLoots.Add(loot2);
			Place(loot2.transform, mineralRotationRange);
			Loots.Add(loot2);
		}
	}

	private void AddLuckyCoin()
	{
		float value = Random.value;
		float num = LuckyCoinChanceSkill.FinalValue / 100f;
		num *= ArtifactGroup.CoinChanceMultiplier;
		if (!(value > num))
		{
			int num2 = ((!(Random.value < DoubleCoinSpawnSkill.FinalValue / 100f)) ? 1 : 2);
			for (int i = 0; i < num2; i++)
			{
				Loot loot = Object.Instantiate(Singleton<GameResources>.Current.Loots.CoinLoot, Parent);
				loot.SetItUp();
				preLoots.Add(loot);
				Place(loot.transform, coinRotationRange);
				Loots.Add(loot);
			}
		}
	}

	private void AddTicket()
	{
		float num = Random.value;
		int bricksBought = BricksBoughtAtGeneration >= 0 ? BricksBoughtAtGeneration : SaveManager.Current.SaveData.BricksBought;
		if (bricksBought == 3)
		{
			num = 0f;
		}
		float num2 = TicketChanceSkill.FinalValue / 100f;
		num2 *= ArtifactGroup.TicketChanceMultiplier;
		if (!(num > num2))
		{
			Loot loot = Object.Instantiate(Singleton<GameResources>.Current.Loots.Ticket, Parent);
			loot.SetItUp();
			preLoots.Add(loot);
			Place(loot.transform, ticketRotationRange);
			Loots.Add(loot);
		}
	}

	private void Place(Transform loot, Vector3 rotationRange)
	{
		int index = Random.Range(0, PointsLeft.Count);
		Vector3 position = PointsLeft[index];
		PointsLeft.RemoveAt(index);
		loot.transform.position = position;
		Vector3 euler = new Vector3
		{
			x = Random.Range(0f - rotationRange.x, rotationRange.x),
			y = Random.Range(0f - rotationRange.y, rotationRange.y),
			z = Random.Range(0f - rotationRange.z, rotationRange.z)
		};
		loot.transform.rotation = Quaternion.Euler(euler);
	}

	public List<Vector3> GeneratePoints()
	{
		List<Vector3> list = new List<Vector3>();
		Vector3 vector = new Vector3(placementBounds.size.x / (float)gridX, placementBounds.size.y / (float)gridY, placementBounds.size.z / (float)gridZ);
		Vector3 min = placementBounds.min;
		for (int i = 0; i < gridX; i++)
		{
			for (int j = 0; j < gridY; j++)
			{
				for (int k = 0; k < gridZ; k++)
				{
					Vector3 vector2 = new Vector3(min.x + ((float)i + 0.5f) * vector.x, min.y + ((float)j + 0.5f) * vector.y, min.z + ((float)k + 0.5f) * vector.z);
					float x = Random.Range((0f - vector.x) * randomOffset, vector.x * randomOffset);
					float y = Random.Range((0f - vector.y) * randomOffset, vector.y * randomOffset);
					float z = Random.Range((0f - vector.z) * randomOffset, vector.z * randomOffset);
					Vector3 item = vector2 + new Vector3(x, y, z);
					list.Add(item);
				}
			}
		}
		return list;
	}
}
