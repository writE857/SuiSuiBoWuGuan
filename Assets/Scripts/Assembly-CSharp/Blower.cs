using UnityEngine;

public class Blower : MonoBehaviour
{
	public SphereCollider SphereCollider;

	public Vector2 forceRange = new Vector2(3f, 1f);

	private void OnTriggerStay(Collider other)
	{
		Rigidbody attachedRigidbody = other.attachedRigidbody;
		if (attachedRigidbody != null)
		{
			Vector3 vector = attachedRigidbody.position - base.transform.position;
			float value = vector.magnitude / SphereCollider.radius;
			value = Mathf.Clamp01(value);
			value = 1f - value;
			float num = Mathf.Lerp(forceRange.x, forceRange.y, value);
			attachedRigidbody.AddForce(vector * num, ForceMode.Force);
		}
	}
}
