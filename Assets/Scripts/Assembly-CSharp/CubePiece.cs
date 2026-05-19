using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class CubePiece : MonoBehaviour
{
	public float hp = 1f;

	public float maxHp = 1f;

	public Rigidbody body;

	public int BrokenLayer;

	public PieceDeath PieceDeath;

	private int brokenLayerIndex;

	private bool isDead;

	public UnityAction OnBroken;

	public int index = -1;

	public bool HitAnyway;

	public bool IsAlive => hp > 0f;

	public bool IsPreparedForBreak => body != null && PieceDeath != null;

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

	public void Hit(float damage)
	{
		if (!isDead)
		{
			hp -= damage;
			if (hp <= 0f)
			{
				if (!IsPreparedForBreak)
				{
					PrepareBreakComponents(Singleton<Hammer>.Current != null ? Singleton<Hammer>.Current.PieceDeath : null);
				}
				body.detectCollisions = true;
				body.isKinematic = false;
				body.useGravity = true;
				body.drag = 1f;
				body.angularDrag = 25f;
				body.angularVelocity = Vector3.zero;
				base.transform.parent = null;
				isDead = true;
				StartCoroutine(Do_Death());
				OnBroken?.Invoke();
			}
		}
	}

	private IEnumerator Do_Death()
	{
		do
		{
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
		Object.Destroy(this);
	}

	internal void SetIndex()
	{
		index = base.transform.GetSiblingIndex();
	}
}
