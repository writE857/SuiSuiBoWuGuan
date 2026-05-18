using System;
using UnityEngine;

public class BrickPile : Singleton<BrickPile>
{
	public BrickTable BrickTable;

	public Brick prefab;

	public Transform SpawnPoint;

	private void OnEnable()
	{
		Brick[] componentsInChildren = base.transform.GetComponentsInChildren<Brick>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			UnityEngine.Object.Destroy(componentsInChildren[i].gameObject);
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
		CubeModel cubeModel = UnityEngine.Object.Instantiate(artifactGroup.prefab, base.transform);
		cubeModel.SetTierData(artifactGroup);
		cubeModel.transform.Reset();
		return cubeModel;
	}
}
