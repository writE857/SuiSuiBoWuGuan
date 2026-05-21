using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;

public class Hammer : Singleton<Hammer>
{
	private struct DamageData
	{
		public CubePiece piece;

		public float damage;

		public float distanceRatio;

		public Vector3 distV;

		public Vector3 distVForce;

		public float damageHPRatio;
	}

	private struct DamageTarget
	{
		public CubePiece piece;

		public Vector3 hitPoint;

		public float horizontalDistance;

		public float depth;
	}

	public float debugInterval = 0.01f;

	public float debugDamage = 5f;

	private float lastTime = -1f;

	public Vector2 forceRange = new Vector2(0.2f, 1f);

	public int BrokenLayer;

	public LayerMask HammerLayerMask;

	public LayerMask HammerLootLayerMask;

	public SphereCollider SphereCollider;

	public Transform forceSource;

	public float moveUpDistance = 0.02f;

	public PieceDeath PieceDeath;

	private Rigidbody Rigidbody;

	public float nonLethalMoveRate = 0.001f;

	public float nonLethalRotateAngle = 1f;

	public AnimationCurve DamageCurve;

	public bool IsHitting;

	private Vector3 damageOrigin;

	private bool hasDamageOrigin;

	public float flyForce = 1f;

	public bool drawGizmo;

	public AudioResource BrickHitSFX;

	public HammerAnimator HammerAnimator;

	public UnityAction OnHit;

	public HammerStats HammerStats;

	[Header("Prestige")]
	public PrestigeSkill AutoHammerSkill;

	public PrestigeSkill DoubleDamageSkill;

	public PrestigeSkill DoubleSizeSkill;

	public PrestigeSkill MoneyHitSkill;

	private RaycastHit[] hits = new RaycastHit[100];

	private Collider[] overlapHits = new Collider[1024];

	private List<Vector3> RayStartPoints = new List<Vector3>();

	private readonly List<DamageTarget> damageTargetsBuffer = new List<DamageTarget>(256);

	private readonly Dictionary<CubePiece, int> damageTargetIndexBuffer = new Dictionary<CubePiece, int>(256);

	private readonly List<DamageData> damageBuffer = new List<DamageData>(256);

	private const float RayOriginHeight = 1f;

	private const float SurfacePadding = 0.02f;

	private const float MinimumEdgeDamageRatio = 0.15f;

	public float resolution = 0.1f;

	public int rayCount;

	private float lastRadius = -10f;

	private float lastResolution = -10f;

	public float interval => 1f / HammerStats.Speed;

	public float Damage => HammerStats.Damage;

	public float maxDepth => HammerStats.Depth;

	public float radius => SphereCollider.WorldRadius();

	public Vector3 DamageOrigin => hasDamageOrigin ? damageOrigin : base.transform.position;

	public void SetDamageOrigin(Vector3 origin)
	{
		damageOrigin = origin;
		hasDamageOrigin = true;
	}

	private void Start()
	{
		Rigidbody = GetComponent<Rigidbody>();
		int hammerLayer = SphereCollider != null ? SphereCollider.gameObject.layer : gameObject.layer;
		int brokenLayer = LayerMask.NameToLayer("Broken");
		if (brokenLayer >= 0)
		{
			BrokenLayer = brokenLayer;
		}
		else if (BrokenLayer < 0 || BrokenLayer > 31)
		{
			BrokenLayer = 7;
		}
		if (hammerLayer >= 0 && BrokenLayer >= 0)
		{
			Physics.IgnoreLayerCollision(hammerLayer, BrokenLayer, true);
		}
		lastTime = Time.time;
		HammerAnimator hammerAnimator = HammerAnimator;
		hammerAnimator.OnHitEnd = (UnityAction)Delegate.Combine(hammerAnimator.OnHitEnd, new UnityAction(OnHammerHitEnd));
	}

	private void Update()
	{
		if (IsHitting && AutoHammerSkill.IsUnlocked)
		{
			float num = interval;
			if (Application.isEditor && Input.GetKey(KeyCode.LeftShift))
			{
				num = debugInterval;
			}
			if (Time.time - lastTime > num)
			{
				HitIt();
			}
		}
	}

	public void Use()
	{
		HitIt();
	}

	private void HitIt()
	{
		if (!Singleton<GameSession>.Current.IsInMenu && !Singleton<ArtifactOverlayDisplay>.Current.IsOpen)
		{
			lastTime = Time.time;
			OnHit?.Invoke();
			HammerAnimator.HitNow();
			SaveManager.Current.SaveData.HammerHitCount++;
		}
	}

	private void OnHammerHitEnd()
	{
		DealDamage();
	}

	private void DealDamage()
	{
		if (UnityEngine.Random.value < DoubleSizeSkill.FinalValue / 100f)
		{
			SphereCollider.radius = HammerStats.Size * 2f;
		}
		else
		{
			SphereCollider.radius = HammerStats.Size * 1f;
		}
		List<DamageTarget> damageTargets = GetDamageTargets();
		List<DamageData> damages = GetDamages(damageTargets);
		float num = 0f;
		foreach (DamageData item in damages)
		{
			item.piece.transform.position += item.distV.normalized * item.damageHPRatio * nonLethalMoveRate;
			item.piece.transform.eulerAngles += item.distV.normalized * item.damageHPRatio * nonLethalRotateAngle;
			float num2 = item.damage;
			if (UnityEngine.Random.value < DoubleDamageSkill.FinalValue / 100f)
			{
				num2 *= 2f;
			}
			if (Application.isEditor && Input.GetKey(KeyCode.LeftShift))
			{
				float num3 = Mathf.Min(item.piece.hp, num2 + debugDamage);
				item.piece.Hit(num2 + debugDamage);
				num += num3;
			}
			else
			{
				float num4 = Mathf.Min(item.piece.hp, num2);
				if (item.piece.HitAnyway)
				{
					item.piece.Hit(1f);
				}
				else
				{
					item.piece.Hit(num2);
				}
				num += num4;
			}
			if (item.piece.hp <= 0f)
			{
				item.piece.gameObject.layer = BrokenLayer;
				if (item.piece.body != null)
				{
					Mathf.Lerp(forceRange.x, forceRange.y, item.damageHPRatio);
					UnityEngine.Random.Range(0.75f, 1.25f);
					item.piece.body.MovePosition(item.piece.transform.position + moveUpDistance * Vector3.up);
					item.piece.body.velocity = item.distVForce.normalized * forceRange.GetRandomBetweenXY() * flyForce;
				}
			}
		}
		int num5 = Mathf.RoundToInt(num * (MoneyHitSkill.FinalValue / 100f));
		if (num5 > 0)
		{
			Singleton<GameEvents>.Current.OnHammerIncome?.Invoke(num5);
			Singleton<LootManager>.Current.AddMoney(num5);
			Singleton<FloatingTextPool>.Current.SpawnMoney("$" + NumFormat.ToM1Decimal(num5), DamageOrigin);
		}
		Singleton<GameEvents>.Current.OnHammerHit?.Invoke(num);
		if (num >= 1f)
		{
			Singleton<FloatingTextPool>.Current.SpawnDamage(NumFormat.ToM1Decimal(num) ?? "", DamageOrigin);
		}
		if (damages.Count > 0)
		{
			Singleton<AudioPool>.Current.Play(BrickHitSFX, DamageOrigin);
		}
	}

	private List<DamageTarget> GetDamageTargets()
	{
		damageTargetsBuffer.Clear();
		Vector3 position = DamageOrigin;
		float activeRadius = Mathf.Max(radius, 0.001f);
		float activeDepth = Mathf.Max(maxDepth, 0.001f);
		BrickTable brickTable = Singleton<BrickTable>.Current;
		Brick currentBrick = brickTable != null ? brickTable.CurrentBrick : null;
		CubeModel cubeModel = currentBrick != null ? currentBrick.CubeModel : null;
		if (cubeModel != null && !currentBrick.IsFull)
		{
			List<CubePiece> brokenPieces = cubeModel.GetActiveBrokenPieces();
			for (int i = 0; brokenPieces != null && i < brokenPieces.Count; i++)
			{
				CubePiece piece = brokenPieces[i];
				if (piece != null)
				{
					AddDamageTargetUnchecked(piece, piece.HitPointFor(position), position, activeRadius, activeDepth);
				}
			}
			return damageTargetsBuffer;
		}
		damageTargetIndexBuffer.Clear();
		int count = Physics.OverlapSphereNonAlloc(position, activeRadius + SurfacePadding, overlapHits, HammerLayerMask, QueryTriggerInteraction.Ignore);
		for (int i = 0; i < count; i++)
		{
			Collider collider = overlapHits[i];
			if (collider == null)
			{
				continue;
			}
			AddDamageTarget(collider.GetComponentInParent<CubePiece>(), collider.ClosestPoint(position), position, activeRadius, activeDepth);
		}
		return damageTargetsBuffer;
	}

	private void AddDamageTargetUnchecked(CubePiece piece, Vector3 hitPoint, Vector3 origin, float activeRadius, float activeDepth)
	{
		if (piece == null || !piece.IsAlive)
		{
			return;
		}
		Vector3 offset = hitPoint - origin;
		float horizontalDistance = offset.X0Z().magnitude;
		float depth = Mathf.Max(0f, origin.y - hitPoint.y);
		if (!piece.HitAnyway && (horizontalDistance > activeRadius + SurfacePadding || depth > activeDepth + SurfacePadding))
		{
			return;
		}
		damageTargetsBuffer.Add(new DamageTarget
		{
			piece = piece,
			hitPoint = hitPoint,
			horizontalDistance = horizontalDistance,
			depth = depth
		});
	}

	private void AddDamageTarget(CubePiece piece, Vector3 hitPoint, Vector3 origin, float activeRadius, float activeDepth)
	{
		if (piece == null || !piece.IsAlive)
		{
			return;
		}
		Vector3 offset = hitPoint - origin;
		float horizontalDistance = offset.X0Z().magnitude;
		float depth = Mathf.Max(0f, origin.y - hitPoint.y);
		if (!piece.HitAnyway && (horizontalDistance > activeRadius + SurfacePadding || depth > activeDepth + SurfacePadding))
		{
			return;
		}
		DamageTarget target = new DamageTarget
		{
			piece = piece,
			hitPoint = hitPoint,
			horizontalDistance = horizontalDistance,
			depth = depth
		};
		if (!damageTargetIndexBuffer.TryGetValue(piece, out int index))
		{
			damageTargetIndexBuffer.Add(piece, damageTargetsBuffer.Count);
			damageTargetsBuffer.Add(target);
			return;
		}
		DamageTarget current = damageTargetsBuffer[index];
		float currentScore = current.horizontalDistance + current.depth * 2f;
		float targetScore = target.horizontalDistance + target.depth * 2f;
		if (targetScore < currentScore)
		{
			damageTargetsBuffer[index] = target;
		}
	}

	private List<DamageData> GetDamages(List<DamageTarget> targets)
	{
		damageBuffer.Clear();
		float activeRadius = Mathf.Max(radius, 0.001f);
		float activeDepth = Mathf.Max(maxDepth, 0.001f);
		foreach (DamageTarget target in targets)
		{
			CubePiece piece = target.piece;
			Vector3 distVForce = piece.transform.position - forceSource.position;
			Vector3 vector = piece.transform.position - target.hitPoint;
			float value = 1f - target.horizontalDistance / activeRadius;
			float value2 = 1f - target.depth / activeDepth;
			value2 = Mathf.Clamp01(value2);
			value = Mathf.Clamp01(value);
			value *= value2;
			value = DamageCurve.Evaluate(value);
			float num2 = Damage * value;
			if (piece.HitAnyway)
			{
				num2 = Damage;
			}
			else if (num2 <= 0f)
			{
				num2 = Damage * MinimumEdgeDamageRatio;
			}
			float damageHPRatio = Mathf.Clamp01(num2 / piece.maxHp);
			damageBuffer.Add(new DamageData
			{
				piece = piece,
				damage = num2,
				distanceRatio = value,
				distV = vector,
				distVForce = distVForce,
				damageHPRatio = damageHPRatio
			});
		}
		return damageBuffer;
	}

	private void OnDrawGizmosSelected()
	{
		if (!drawGizmo || radius <= 0f || resolution <= 0f)
		{
			return;
		}
		Vector3 position = DamageOrigin;
		rayCount = 0;
		CalculateRayStartPoints();
		foreach (Vector3 rayStartPoint in RayStartPoints)
		{
			Vector3 vector = position + rayStartPoint;
			float magnitude = rayStartPoint.magnitude;
			float t = Mathf.Pow(magnitude / radius, 2.2f);
			t = Mathf.Lerp(0f, 0.5f, t);
			Gizmos.color = Color.HSVToRGB(1f - t, 1f, 1f);
			if (magnitude <= radius)
			{
				Vector3 to = vector + Vector3.down * 1f;
				Gizmos.DrawLine(vector, to);
				rayCount++;
			}
		}
	}

	private void CalculateRayStartPoints()
	{
		if (lastRadius == radius && lastResolution == resolution)
		{
			return;
		}
		lastRadius = radius;
		lastResolution = resolution;
		RayStartPoints.Clear();
		RayStartPoints.Add(Vector3.zero);
		rayCount = 1;
		float step = Mathf.Clamp(resolution, radius / 8f, radius);
		for (float num = 0f - radius; num <= radius; num += step)
		{
			for (float num2 = 0f - radius; num2 <= radius; num2 += step)
			{
				Vector3 item = new Vector3(num, 0f, num2);
				if (item.magnitude <= radius && item.sqrMagnitude > 0.000001f)
				{
					RayStartPoints.Add(item);
					rayCount++;
				}
			}
		}
	}
}
