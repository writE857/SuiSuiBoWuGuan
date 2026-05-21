using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrickPile : Singleton<BrickPile>
{
	public BrickTable BrickTable;

	public Brick prefab;

	public Transform SpawnPoint;

	[Header("Cube Model Pool")]
	public bool UseCubeModelPool;

	public int InitialPoolSizePerGroup;

	public int MaxPoolSizePerGroup;

	public int WarmupsPerFrame = 1;

	private readonly Dictionary<ArtifactGroup, Queue<CubeModel>> cubeModelPool = new Dictionary<ArtifactGroup, Queue<CubeModel>>();

	private readonly Dictionary<ArtifactGroup, int> queuedWarmups = new Dictionary<ArtifactGroup, int>();

	private Transform poolRoot;

	private Coroutine warmupRoutine;

	private void OnEnable()
	{
		Brick[] componentsInChildren = base.transform.GetComponentsInChildren<Brick>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			UnityEngine.Object.Destroy(componentsInChildren[i].gameObject);
		}
	}

	private void Start()
	{
		if (UseCubeModelPool && InitialPoolSizePerGroup > 0 && MaxPoolSizePerGroup > 0)
		{
			QueueInitialWarmup();
		}
	}

	public Brick AddNewBrick(int seed, ArtifactGroup artifactGroup)
	{
		UnityEngine.Random.InitState(seed);
		CubeModel cubeModel = GenerateNewBrick(artifactGroup);
		Brick brick = UnityEngine.Object.Instantiate(prefab, SpawnPoint.position, SpawnPoint.rotation, SpawnPoint);
		brick.Init(cubeModel, seed);
		BrickTable.AddBrick(brick);
		brick.IsPicked = true;
		UnityEngine.Object.Destroy(brick.transform.GetComponent<Rigidbody>());
		return brick;
	}

	public Brick AddNewRandomBrick(ArtifactGroup artifactGroup)
	{
		int hashCode = Guid.NewGuid().GetHashCode();
		return AddNewBrick(hashCode, artifactGroup);
	}

	private CubeModel GenerateNewBrick(ArtifactGroup artifactGroup)
	{
		CubeModel cubeModel = GetPooledCubeModel(artifactGroup);
		if (cubeModel == null)
		{
			cubeModel = UnityEngine.Object.Instantiate(artifactGroup.prefab, base.transform);
		}
		cubeModel.SetTierData(artifactGroup, prepareBroken: false);
		cubeModel.transform.Reset();
		if (UseCubeModelPool && MaxPoolSizePerGroup > 0)
		{
			QueueWarmup(artifactGroup, 1);
		}
		return cubeModel;
	}

	private CubeModel GetPooledCubeModel(ArtifactGroup artifactGroup)
	{
		if (!UseCubeModelPool || artifactGroup == null)
		{
			return null;
		}
		Queue<CubeModel> queue;
		if (!cubeModelPool.TryGetValue(artifactGroup, out queue))
		{
			return null;
		}
		while (queue.Count > 0)
		{
			CubeModel cubeModel = queue.Dequeue();
			if (cubeModel == null)
			{
				continue;
			}
			cubeModel.transform.SetParent(base.transform, worldPositionStays: false);
			cubeModel.gameObject.SetActive(value: true);
			return cubeModel;
		}
		return null;
	}

	private void QueueInitialWarmup()
	{
		GameResources resources = Singleton<GameResources>.Current;
		if (resources == null || resources.Artifacts == null || resources.Artifacts.Groups == null)
		{
			return;
		}
		for (int i = 0; i < resources.Artifacts.Groups.Count; i++)
		{
			QueueWarmup(resources.Artifacts.Groups[i], InitialPoolSizePerGroup);
		}
	}

	private void QueueWarmup(ArtifactGroup artifactGroup, int count)
	{
		if (!UseCubeModelPool || artifactGroup == null || artifactGroup.prefab == null || count <= 0 || MaxPoolSizePerGroup <= 0)
		{
			return;
		}
		int pooledCount = GetPooledCount(artifactGroup);
		int queuedCount = 0;
		queuedWarmups.TryGetValue(artifactGroup, out queuedCount);
		int capacityLeft = Mathf.Max(0, MaxPoolSizePerGroup - pooledCount - queuedCount);
		int countToQueue = Mathf.Min(count, capacityLeft);
		if (countToQueue <= 0)
		{
			return;
		}
		queuedWarmups[artifactGroup] = queuedCount + countToQueue;
		if (warmupRoutine == null)
		{
			warmupRoutine = StartCoroutine(WarmupQueuedModels());
		}
	}

	private IEnumerator WarmupQueuedModels()
	{
		int warmedThisFrame = 0;
		yield return null;
		while (queuedWarmups.Count > 0)
		{
			ArtifactGroup artifactGroup = null;
			foreach (KeyValuePair<ArtifactGroup, int> pair in queuedWarmups)
			{
				if (pair.Value > 0)
				{
					artifactGroup = pair.Key;
					break;
				}
			}
			if (artifactGroup == null)
			{
				break;
			}
			queuedWarmups[artifactGroup]--;
			if (queuedWarmups[artifactGroup] <= 0)
			{
				queuedWarmups.Remove(artifactGroup);
			}
			WarmupOne(artifactGroup);
			warmedThisFrame++;
			if (warmedThisFrame >= Mathf.Max(1, WarmupsPerFrame))
			{
				warmedThisFrame = 0;
				yield return null;
			}
		}
		warmupRoutine = null;
	}

	private void WarmupOne(ArtifactGroup artifactGroup)
	{
		if (artifactGroup == null || artifactGroup.prefab == null || GetPooledCount(artifactGroup) >= MaxPoolSizePerGroup)
		{
			return;
		}
		EnsurePoolRoot();
		CubeModel cubeModel = UnityEngine.Object.Instantiate(artifactGroup.prefab, poolRoot);
		cubeModel.SetTierData(artifactGroup, prepareBroken: false);
		cubeModel.transform.Reset();
		cubeModel.gameObject.SetActive(value: false);
		GetPoolQueue(artifactGroup).Enqueue(cubeModel);
	}

	private int GetPooledCount(ArtifactGroup artifactGroup)
	{
		Queue<CubeModel> queue;
		if (!cubeModelPool.TryGetValue(artifactGroup, out queue))
		{
			return 0;
		}
		return queue.Count;
	}

	private Queue<CubeModel> GetPoolQueue(ArtifactGroup artifactGroup)
	{
		Queue<CubeModel> queue;
		if (!cubeModelPool.TryGetValue(artifactGroup, out queue))
		{
			queue = new Queue<CubeModel>();
			cubeModelPool.Add(artifactGroup, queue);
		}
		return queue;
	}

	private void EnsurePoolRoot()
	{
		if (poolRoot != null)
		{
			return;
		}
		GameObject gameObject = new GameObject("Cube Model Pool");
		poolRoot = gameObject.transform;
		poolRoot.SetParent(base.transform, worldPositionStays: false);
		poolRoot.Reset();
	}
}
