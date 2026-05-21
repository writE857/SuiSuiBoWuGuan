using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class CubePiece : MonoBehaviour
{
	private const int MaxSimulatedDebris = 32;

	private static int simulatedDebrisCount;

	private const float MaxBreakHorizontalDistance = 1.75f;

	private const float MaxBreakVerticalRise = 1.25f;

	private const float BreakBoundaryDamping = 0.35f;

	private const float MaxBreakSpeed = 1.5f;

	public float hp = 1f;

	public float maxHp = 1f;

	public Rigidbody body;

	public int BrokenLayer;

	public PieceDeath PieceDeath;

	private BoxCollider boxCollider;

	private int brokenLayerIndex;

	private bool isDead;

	private bool countedAsSimulatedDebris;

	private Vector3 breakOrigin;

	private bool hasBreakOrigin;

	private CubeModel owner;

	private int ownerIndex = -1;

	private int ownerActiveIndex = -1;

	public UnityAction OnBroken;

	public int index = -1;

	public bool HitAnyway;

	public bool IsAlive => hp > 0f;

	public bool IsPreparedForBreak => body != null && PieceDeath != null;

	private static bool CanSimulateDebris => simulatedDebrisCount < MaxSimulatedDebris;

	public BoxCollider BoxCollider
	{
		get
		{
			if (boxCollider == null)
			{
				boxCollider = GetComponent<BoxCollider>();
			}
			return boxCollider;
		}
	}

	public Vector3 HitPointFor(Vector3 origin)
	{
		if (boxCollider == null)
		{
			return base.transform.position;
		}
		return base.transform.TransformPoint(boxCollider.center);
	}

	public void PrepareBreakComponents(PieceDeath template)
	{
		if (body == null)
		{
			body = base.gameObject.AddComponent<Rigidbody>();
			body.detectCollisions = false;
			body.isKinematic = true;
			body.useGravity = false;
			body.drag = 1f;
			body.angularDrag = 25f;
			body.angularVelocity = Vector3.zero;
		}
		if (PieceDeath == null)
		{
			PieceDeath = base.gameObject.AddComponent<PieceDeath>();
		}
		if (template != null)
		{
			template.CopyTo(PieceDeath);
		}
		PieceDeath.enabled = false;
	}

	public void SetOwner(CubeModel cubeModel, int arrayIndex, int activeIndex)
	{
		owner = cubeModel;
		ownerIndex = arrayIndex;
		ownerActiveIndex = activeIndex;
		if (boxCollider == null)
		{
			boxCollider = GetComponent<BoxCollider>();
		}
	}

	public void SetOwnerActiveIndex(int activeIndex)
	{
		ownerActiveIndex = activeIndex;
	}

	public void Hit(float damage)
	{
		if (!isDead)
		{
			hp -= damage;
			if (hp <= 0f)
			{
				bool simulateDebris = CanSimulateDebris;
				if (simulateDebris && !IsPreparedForBreak)
				{
					PrepareBreakComponents(Singleton<Hammer>.Current != null ? Singleton<Hammer>.Current.PieceDeath : null);
				}
				BoxCollider collider = simulateDebris ? BoxCollider : boxCollider;
				if (collider != null)
				{
					collider.enabled = simulateDebris;
				}
				base.transform.parent = null;
				isDead = true;
				breakOrigin = base.transform.position;
				hasBreakOrigin = simulateDebris;
				NotifyOwnerBroken();
				if (simulateDebris && body != null)
				{
					RegisterSimulatedDebris();
					int hammerLayer = LayerMask.NameToLayer("Hammer");
					int brokenLayer = LayerMask.NameToLayer("Broken");
					if (hammerLayer >= 0 && brokenLayer >= 0)
					{
						Physics.IgnoreLayerCollision(hammerLayer, brokenLayer, true);
					}
					body.detectCollisions = true;
					body.isKinematic = false;
					body.useGravity = true;
					body.drag = 1f;
					body.angularDrag = 25f;
					body.angularVelocity = Vector3.zero;
					StartCoroutine(Do_PhysicsDeath());
				}
				else
				{
					Do_LightweightDeath();
				}
				OnBroken?.Invoke();
			}
		}
	}

	private void NotifyOwnerBroken()
	{
		if (owner == null)
		{
			return;
		}
		owner.NotifyPieceBroken(this, ownerIndex, ownerActiveIndex);
		owner = null;
		ownerIndex = -1;
		ownerActiveIndex = -1;
	}

	private void RegisterSimulatedDebris()
	{
		if (countedAsSimulatedDebris)
		{
			return;
		}
		countedAsSimulatedDebris = true;
		simulatedDebrisCount++;
	}

	private void ReleaseSimulatedDebris()
	{
		if (!countedAsSimulatedDebris)
		{
			return;
		}
		countedAsSimulatedDebris = false;
		simulatedDebrisCount = Mathf.Max(0, simulatedDebrisCount - 1);
	}

	private IEnumerator Do_PhysicsDeath()
	{
		do
		{
			ClampBreakMotion();
			yield return null;
		}
		while (!isDead || (!(body == null) && !body.IsSleeping()));
		if (PieceDeath != null)
		{
			PieceDeath.StartDoing();
		}
		if (body != null)
		{
			Object.Destroy(body);
			body = null;
		}
		ReleaseSimulatedDebris();
		Object.Destroy(this);
	}

	private void ClampBreakMotion()
	{
		if (body == null || !hasBreakOrigin)
		{
			return;
		}
		Vector3 position = body.position;
		Vector3 offset = position - breakOrigin;
		Vector3 horizontal = new Vector3(offset.x, 0f, offset.z);
		bool clamped = false;
		float horizontalMagnitude = horizontal.magnitude;
		if (horizontalMagnitude > MaxBreakHorizontalDistance)
		{
			horizontal = horizontal.normalized * MaxBreakHorizontalDistance;
			position.x = breakOrigin.x + horizontal.x;
			position.z = breakOrigin.z + horizontal.z;
			clamped = true;
		}
		float maxHeight = breakOrigin.y + MaxBreakVerticalRise;
		if (position.y > maxHeight)
		{
			position.y = maxHeight;
			if (body.velocity.y > 0f)
			{
				body.velocity = new Vector3(body.velocity.x, 0f, body.velocity.z);
			}
			clamped = true;
		}
		if (clamped)
		{
			body.position = position;
			body.velocity *= BreakBoundaryDamping;
		}
		if (body.velocity.sqrMagnitude > MaxBreakSpeed * MaxBreakSpeed)
		{
			body.velocity = Vector3.ClampMagnitude(body.velocity, MaxBreakSpeed);
		}
	}

	private void Do_LightweightDeath()
	{
		gameObject.SetActive(false);
	}

	private void OnDestroy()
	{
		ReleaseSimulatedDebris();
	}

	internal void SetIndex()
	{
		index = base.transform.GetSiblingIndex();
	}
}
