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

	public void Hit(float damage)
	{
		if (!isDead)
		{
			hp -= damage;
			if (hp <= 0f && body == null)
			{
				body = base.gameObject.AddComponent<Rigidbody>();
				body.detectCollisions = true;
				body.isKinematic = false;
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
		PieceDeath.StartDoing();
		if (body != null)
		{
			Object.Destroy(body);
		}
		Object.Destroy(this);
	}

	internal void SetIndex()
	{
		index = base.transform.GetSiblingIndex();
	}
}
