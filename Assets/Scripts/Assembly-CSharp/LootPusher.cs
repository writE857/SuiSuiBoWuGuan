using UnityEngine;

public class LootPusher : MonoBehaviour
{
	public Transform Target;

	public Rigidbody Rigidbody;

	public float force = 1f;

	private void Start()
	{
		Rigidbody = GetComponent<Rigidbody>();
		Target = base.transform.parent;
		base.transform.parent = null;
	}

	private void FixedUpdate()
	{
		Rigidbody.MovePosition(Target.position);
	}
}
