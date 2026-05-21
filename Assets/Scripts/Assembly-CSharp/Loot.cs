using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;

public class Loot : MonoBehaviour
{
	public string DisplayName;

	public bool didInit;

	public bool IsFree;

	public Rigidbody Rigidbody;

	public int SellValue = 10;

	private int _finalValue = 10;

	public float RangeMultiplier = 1.2f;

	public MeshCollider MeshCollider;

	private int LastOverlapCount = -1;

	public AudioResource HammerHitSFX;

	public float Scale = 1f;

	public float ValueScale = 1f;

	public LayerMask LayerMask;

	public bool CanBeTaken;

	public Artifact Artifact;

	public UnityAction OnCollected;

	public List<Collider> others = new List<Collider>();

	public List<Collider> othersRaw = new List<Collider>();

	public bool didHaveAnyOverlap;

	public PrestigeSkill LootValueSkill;

	private int audioPriority = 240;

	private float soundsInterval = 0.1f;

	private float nextSound;

	private int since;

	public int FinalValue
	{
		get
		{
			if (!(LootValueSkill == null))
			{
				return Mathf.RoundToInt((float)_finalValue * LootValueSkill.FinalValue);
			}
			return _finalValue;
		}
	}

	public void SetItUp()
	{
		MeshCollider = GetComponentInChildren<MeshCollider>();
		if (MeshCollider == null)
		{
			MeshCollider = GetComponentInChildren<MeshRenderer>().gameObject.AddComponent<MeshCollider>();
			MeshCollider.convex = true;
		}
		Rigidbody = GetComponentInChildren<Rigidbody>();
		LastOverlapCount = int.MaxValue;
		MeshCollider.isTrigger = true;
	}

	private bool IsOverlap()
	{
		return others.Count > 0;
	}

	private void OnDrawGizmosSelected()
	{
		CheckBlockers();
		Gizmos.color = Color.yellow;
		for (int i = 0; i < others.Count; i++)
		{
			Gizmos.DrawWireSphere(others[i].transform.position, 0.001f);
		}
		Gizmos.color = Color.white;
		for (int j = 0; j < othersRaw.Count; j++)
		{
			Gizmos.DrawWireSphere(othersRaw[j].transform.position, 0.0005f);
		}
	}

	public void CheckInitialOverlap()
	{
		StartCoroutine(DelayedCheck());
	}

	public void CheckInitialOverlapNow()
	{
		CheckBlockers();
		if (others.Count == 0)
		{
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		LastOverlapCount = others.Count;
		didHaveAnyOverlap = true;
	}

	private IEnumerator DelayedCheck()
	{
		yield return new WaitForFixedUpdate();
		CheckInitialOverlapNow();
	}

	private void CheckBlockers()
	{
		others = GetCollidersInside();
	}

	public List<Collider> GetCollidersInside()
	{
		Bounds bounds = MeshCollider.bounds;
		others.Clear();
		othersRaw.Clear();
		BrickTable brickTable = Singleton<BrickTable>.Current;
		Brick currentBrick = brickTable != null ? brickTable.CurrentBrick : null;
		CubeModel cubeModel = currentBrick != null ? currentBrick.CubeModel : null;
		if (cubeModel != null)
		{
			List<CubePiece> pieces = cubeModel.GetActiveBrokenPieces();
			for (int i = 0; pieces != null && i < pieces.Count; i++)
			{
				CubePiece component = pieces[i];
				if (component == null || !component.IsAlive)
				{
					continue;
				}
				BoxCollider col = component.BoxCollider;
				if (col == null)
				{
					continue;
				}
				Vector3 point = component.HitPointFor(MeshCollider.bounds.center);
				if (!bounds.Contains(point))
				{
					continue;
				}
				othersRaw.Add(col);
				if (IsPointInsideMeshVolume(MeshCollider, point))
				{
					others.Add(col);
					component.OnBroken = (UnityAction)Delegate.Combine(component.OnBroken, (UnityAction)delegate
					{
						OnStonePieceBroken(col);
					});
				}
			}
			return others;
		}
		Collider[] array = Physics.OverlapBox(bounds.center, bounds.extents, Quaternion.identity, LayerMask, QueryTriggerInteraction.Ignore);
		Collider[] array2 = array;
		foreach (Collider col in array2)
		{
			if (col == MeshCollider)
			{
				continue;
			}
			othersRaw.Add(col);
			if (IsPointInsideMeshVolume(MeshCollider, col.transform.position))
			{
				others.Add(col);
				CubePiece component = col.GetComponent<CubePiece>();
				if (component == null)
				{
					continue;
				}
				component.OnBroken = (UnityAction)Delegate.Combine(component.OnBroken, (UnityAction)delegate
				{
					OnStonePieceBroken(col);
				});
			}
		}
		return others;
	}

	private void OnStonePieceBroken(Collider collider)
	{
		others.Remove(collider);
		if (nextSound < Time.time)
		{
			Singleton<AudioPool>.Current.Play(HammerHitSFX, base.transform.position, audioPriority);
			nextSound = Time.time + soundsInterval;
		}
		if (others.Count == 0)
		{
			BreakOut();
		}
	}

	private bool IsPointInsideMeshVolume(MeshCollider mesh, Vector3 point)
	{
		return Vector3.Distance(mesh.ClosestPoint(point), point) < 0.001f;
	}

	private void OnEnable()
	{
		since = Time.frameCount;
		MeshCollider = GetComponentInChildren<MeshCollider>();
		if (!Singleton<LootManager>.Current.AllLoot.Contains(this))
		{
			Singleton<LootManager>.Current.AllLoot.Add(this);
		}
	}

	private void BreakOut()
	{
		base.enabled = false;
		IsFree = true;
		Singleton<GameEvents>.Current.OnLootExtracted?.Invoke(this);
		Rigidbody.isKinematic = false;
		base.transform.parent = Singleton<LootManager>.Current.UncollectedParent;
		MeshCollider.isTrigger = false;
		Singleton<LootExpulsor>.Current.ShootOut(this);
		Singleton<Shaker>.Current.ShakeHammerHit();
	}

	public void Collected()
	{
		OnCollected?.Invoke();
	}

	internal void SetValue(float scale, float valueScale)
	{
		Scale = scale;
		ValueScale = valueScale;
		base.transform.localScale = Vector3.one * Scale;
		_finalValue = Mathf.RoundToInt((float)SellValue * valueScale);
	}
}
