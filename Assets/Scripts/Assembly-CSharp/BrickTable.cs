using System;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;

public class BrickTable : Singleton<BrickTable>
{
	public BrickPile BrickPile;

	public Transform Parent;

	public float transportTime = 1f;

	public Brick CurrentBrick;

	public bool IsMoving;

	public AudioResource brickDropSFX;

	public Transform tableTarget;

	[Header("Spawn Settings")]
	public float spawnHeight = 3f;

	public float horizontalRange = 1f;

	[Header("Fall Settings")]
	public float fallTime = 0.35f;

	public float rotateSpeed = 180f;

	private void Start()
	{
		Brick componentInChildren = Parent.GetComponentInChildren<Brick>(includeInactive: true);
		if (componentInChildren != null)
		{
			UnityEngine.Object.Destroy(componentInChildren.gameObject);
		}
		BrickSaveData brickSave = SaveManager.Current.SaveData.BrickSaveData;
		ArtifactGroup artifactGroup = Singleton<GameResources>.Current.Artifacts.Groups.FirstOrDefault((ArtifactGroup a) => a.GROUPID == brickSave.GroupID);
		if (artifactGroup != null)
		{
			Brick brick = Singleton<BrickPile>.Current.AddNewBrick(brickSave.seed, artifactGroup);
			brick.CubeModel.SetAlivePieces(brickSave.IndicesAlive);
			AddBrick(brick);
		}
		GameEvents current = Singleton<GameEvents>.Current;
		current.OnRestart = (UnityAction)Delegate.Combine(current.OnRestart, new UnityAction(OnRestart));
	}

	private void OnRestart()
	{
		if (CurrentBrick != null)
		{
			UnityEngine.Object.Destroy(CurrentBrick.gameObject);
		}
	}

	public void AddBrick(Brick brick)
	{
		RemoveBricks(brick);
		IsMoving = true;
		DropBrick(brick);
	}

	private void FinalizeBrick(Brick brick)
	{
		brick.transform.parent = Parent;
		brick.transform.localPosition = Vector3.zero;
		brick.transform.localRotation = Quaternion.identity;
		brick.ChangeState(_isFull: false);
		CurrentBrick = brick;
		IsMoving = false;
	}

	public void DropBrick(Brick brick)
	{
		Vector2 vector = UnityEngine.Random.insideUnitCircle * horizontalRange;
		Vector3 position = tableTarget.position + new Vector3(vector.x, spawnHeight, vector.y);
		brick.transform.position = position;
		brick.transform.rotation = Parent.rotation;
		AnimateBrickFall(brick);
	}

	private void AnimateBrickFall(Brick brick)
	{
		Singleton<Shaker>.Current.ShakeHeavyThud();
		Vector3 targetPos = tableTarget.position;
		Sequence s = DOTween.Sequence();
		s.Append(brick.transform.DOMove(targetPos, fallTime).SetEase(Ease.InQuad));
		s.Append(brick.transform.DOScale(Vector3.one, 0.12f).SetEase(Ease.OutBack));
		s.AppendCallback(delegate
		{
			FinalizeBrick(brick);
			Singleton<AudioPool>.Current.Play(brickDropSFX, targetPos);
		});
	}

	public void RemoveBricks(Brick newBrick = null)
	{
		Brick[] array = UnityEngine.Object.FindObjectsByType<Brick>(FindObjectsSortMode.None);
		foreach (Brick brick in array)
		{
			if (newBrick == null || brick != newBrick)
			{
				UnityEngine.Object.Destroy(brick.gameObject);
			}
		}
	}
}
