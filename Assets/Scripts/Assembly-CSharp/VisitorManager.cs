using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class VisitorManager : Singleton<VisitorManager>
{
	public float visitorVariation = 0.5f;

	public float VisitorsNeeded;

	public Transform parent;

	public Visitor prefab;

	public Visitor goldenPrefab;

	public List<Visitor> Visitors = new List<Visitor>();

	public List<Visitor> VisitorsToRemove = new List<Visitor>();

	public float minX;

	public float maxX;

	public Vector2 yStartPos;

	public Vector2 xStartPos;

	public float RageMultiplier = 1f;

	public float VisitorSpeedMultiplier = 1f;

	public float VisitorRageSpeedMultiplier = 3f;

	public Transform SpriteTop;

	public Transform SpriteBottom;

	public float TopZ;

	public float BotZ;

	[Header("Prestige")]
	public PrestigeSkill GoldenVisitorSkill;

	public UnityAction<Visitor, float> OnVisitorStopped;

	private int cachedCollectionValue;

	private int cachedCurrentFee;

	private int cachedIncomePerMinute;

	private float cachedCurrentAverageVisitorsPerMinute;

	private float cachedRageRatio;

	private int cachedEconomicsFrame = -1;

	public int AverageVisitorsPerMinute => Singleton<Upgrader>.Current.VisitorsPerMinute;

	public float AverageVisitorsPerSecond => (float)AverageVisitorsPerMinute / 60f;

	public float CurrentAverageVisitorsPerMinute
	{
		get
		{
			RefreshEconomicsIfNeeded();
			return cachedCurrentAverageVisitorsPerMinute;
		}
	}

	public float EntryFeeMultiplier => Singleton<Upgrader>.Current.EntryFee;

	public int CollectionValue
	{
		get
		{
			RefreshEconomicsIfNeeded();
			return cachedCollectionValue;
		}
	}

	public int CurrentFee
	{
		get
		{
			RefreshEconomicsIfNeeded();
			return cachedCurrentFee;
		}
	}

	public int IncomePerMinute
	{
		get
		{
			RefreshEconomicsIfNeeded();
			return cachedIncomePerMinute;
		}
	}

	public int CollectionFinalValue(ArtifactGroup group)
	{
		return (int)((float)group.Artifacts.Sum((Artifact a) => a.CurrentValue) * EntryFeeMultiplier);
	}

	private void Start()
	{
		StartCoroutine(Do_GenerateVisitors());
		TopZ = SpriteTop.position.z;
		BotZ = SpriteBottom.position.z;
		parent.Clear();
		GameEvents current = Singleton<GameEvents>.Current;
		current.OnRestart = (UnityAction)Delegate.Combine(current.OnRestart, new UnityAction(OnRestart));
		int visitorCount = SaveManager.Current.SaveData.visitorCount;
		for (int i = 0; i < visitorCount; i++)
		{
			AddVisitorRandomly();
		}
		int goldenVisitorCount = SaveManager.Current.SaveData.goldenVisitorCount;
		for (int j = 0; j < goldenVisitorCount; j++)
		{
			AddGoldVisitorRandomly();
		}
	}

	private void OnRestart()
	{
		for (int i = 0; i < Visitors.Count; i++)
		{
			UnityEngine.Object.Destroy(Visitors[i].gameObject);
		}
		Visitors.Clear();
	}

	private void OnValidate()
	{
		TopZ = SpriteTop.position.z;
		BotZ = SpriteBottom.position.z;
	}

	private void AddVisitorRandomly()
	{
		Visitor visitor = UnityEngine.Object.Instantiate(prefab, parent);
		visitor.minX = minX;
		visitor.maxX = maxX;
		visitor.transform.localPosition = new Vector3(UnityEngine.Random.Range(minX, maxX), yStartPos.GetRandomBetweenXY(), 0f);
		visitor.localPosition = visitor.transform.localPosition;
		visitor.OnEnter = (UnityAction<Visitor>)Delegate.Combine(visitor.OnEnter, new UnityAction<Visitor>(OnVisitorEnter));
		visitor.OnExit = (UnityAction<Visitor>)Delegate.Combine(visitor.OnExit, new UnityAction<Visitor>(OnVisitorExit));
		visitor.IsEntered = true;
		Visitors.Add(visitor);
	}

	private void AddGoldVisitorRandomly()
	{
		Visitor visitor = UnityEngine.Object.Instantiate(goldenPrefab, parent);
		visitor.minX = minX;
		visitor.maxX = maxX;
		visitor.transform.localPosition = new Vector3(UnityEngine.Random.Range(minX, maxX), yStartPos.GetRandomBetweenXY(), 0f);
		visitor.localPosition = visitor.transform.localPosition;
		visitor.OnEnter = (UnityAction<Visitor>)Delegate.Combine(visitor.OnEnter, new UnityAction<Visitor>(OnVisitorEnter));
		visitor.OnExit = (UnityAction<Visitor>)Delegate.Combine(visitor.OnExit, new UnityAction<Visitor>(OnVisitorExit));
		visitor.IsEntered = true;
		Visitors.Add(visitor);
	}

	private void Update()
	{
		RefreshEconomicsIfNeeded();
		float num = Time.deltaTime * AverageVisitorsPerSecond * UnityEngine.Random.Range(1f - visitorVariation, 1f + visitorVariation);
		num *= 1f + cachedRageRatio * RageMultiplier;
		VisitorsNeeded += num;
		UpdateVisitors();
		VisitorSpeedMultiplier = 1f + cachedRageRatio * VisitorRageSpeedMultiplier;
	}

	private void UpdateVisitors()
	{
		for (int i = 0; i < VisitorsToRemove.Count; i++)
		{
			Visitors.Remove(VisitorsToRemove[i]);
		}
		VisitorsToRemove.Clear();
		for (int j = 0; j < Visitors.Count; j++)
		{
			Visitors[j].ManualUpdate(Time.deltaTime);
		}
	}

	private void RefreshEconomicsIfNeeded()
	{
		int frameCount = Time.frameCount;
		float ratio = Singleton<RageBar>.Current != null ? Singleton<RageBar>.Current.Ratio : 0f;
		if (cachedEconomicsFrame == frameCount && Mathf.Approximately(cachedRageRatio, ratio))
		{
			return;
		}
		cachedRageRatio = ratio;
		cachedCollectionValue = Singleton<GameResources>.Current.Artifacts.Entries.Sum((Artifact a) => a.CurrentValue);
		cachedCurrentAverageVisitorsPerMinute = (float)AverageVisitorsPerMinute * (1f + ratio * RageMultiplier);
		cachedCurrentFee = (int)(EntryFeeMultiplier * (float)cachedCollectionValue);
		cachedIncomePerMinute = (int)(cachedCurrentAverageVisitorsPerMinute * (float)cachedCurrentFee);
		cachedEconomicsFrame = frameCount;
	}

	private IEnumerator Do_GenerateVisitors()
	{
		while (true)
		{
			if (!Singleton<GameSession>.Current.IsGameStarted)
			{
				yield return null;
				continue;
			}
			yield return null;
			while (VisitorsNeeded > 1f)
			{
				Visitor original = prefab;
				if (UnityEngine.Random.value < GoldenVisitorSkill.FinalValue / 100f)
				{
					original = goldenPrefab;
				}
				Visitor visitor = UnityEngine.Object.Instantiate(original, parent);
				visitor.minX = minX;
				visitor.maxX = maxX;
				visitor.transform.localPosition = new Vector3(xStartPos.GetRandomBetweenXY(), yStartPos.GetRandomBetweenXY(), 0f);
				visitor.localPosition = visitor.transform.localPosition;
				visitor.OnEnter = (UnityAction<Visitor>)Delegate.Combine(visitor.OnEnter, new UnityAction<Visitor>(OnVisitorEnter));
				visitor.OnExit = (UnityAction<Visitor>)Delegate.Combine(visitor.OnExit, new UnityAction<Visitor>(OnVisitorExit));
				Visitors.Add(visitor);
				VisitorsNeeded -= 1f;
			}
		}
	}

	private void OnVisitorEnter(Visitor visitor)
	{
		Singleton<GameEvents>.Current.OnVisitorFeePaid?.Invoke();
		Singleton<LootManager>.Current.AddMoney(CurrentFee * visitor.FeeMultiplier);
	}

	private void OnVisitorExit(Visitor visitor)
	{
		VisitorsToRemove.Add(visitor);
	}
}
