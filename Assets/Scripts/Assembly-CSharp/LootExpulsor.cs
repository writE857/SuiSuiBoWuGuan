using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LootExpulsor : Singleton<LootExpulsor>
{
	public float force = 1f;

	public Vector2 ShootSpeed = new Vector2(0.3f, 1f);

	private List<Loot> loots = new List<Loot>();

	private List<Loot> lootsToPush = new List<Loot>();

	private HashSet<Loot> lootSet = new HashSet<Loot>();

	private HashSet<Loot> lootToPushSet = new HashSet<Loot>();

	public Transform center;

	public float canBeTakenTime = 1f;

	public void ShootOut(Loot loot)
	{
		StartCoroutine(Do_SetTakeable(loot));
	}

	private IEnumerator Do_SetTakeable(Loot loot)
	{
		yield return new WaitForFixedUpdate();
		Vector3 vector = loot.transform.position - center.position;
		vector.Normalize();
		Vector3 b = vector;
		b.y = 0f;
		if (b.magnitude < 0.001f)
		{
			b = new Vector3(Random.Range(-1, 1), 0f, Random.Range(-1, 1));
		}
		b.Normalize();
		vector = Vector3.Lerp(vector, b, 0.2f);
		loot.transform.position += Vector3.up * 0.05f;
		Vector3 vector2 = vector * ShootSpeed.GetRandomBetweenXY();
		loot.Rigidbody.velocity = vector2;
		Debug.DrawRay(loot.transform.position, vector2, Color.green, 10f);
		yield return new WaitForSeconds(canBeTakenTime);
		loot.CanBeTaken = true;
	}

	private void OnTriggerEnter(Collider other)
	{
		Loot componentInParent = other.GetComponentInParent<Loot>();
		if (componentInParent != null && lootSet.Add(componentInParent))
		{
			loots.Add(componentInParent);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		Loot componentInParent = other.GetComponentInParent<Loot>();
		if (componentInParent != null && lootSet.Remove(componentInParent))
		{
			loots.Remove(componentInParent);
			if (lootToPushSet.Remove(componentInParent))
			{
				lootsToPush.Remove(componentInParent);
			}
		}
	}

	private void FixedUpdate()
	{
		for (int i = loots.Count - 1; i >= 0; i--)
		{
			Loot loot = loots[i];
			if (loot != null)
			{
				continue;
			}
			lootSet.Remove(loot);
			loots.RemoveAt(i);
		}
		for (int j = lootsToPush.Count - 1; j >= 0; j--)
		{
			Loot loot2 = lootsToPush[j];
			if (!(loot2 == null) && !(loot2.Rigidbody == null) && !(loot2.transform == null))
			{
				continue;
			}
			lootToPushSet.Remove(loot2);
			lootsToPush.RemoveAt(j);
		}
		for (int k = 0; k < loots.Count; k++)
		{
			Loot loot3 = loots[k];
			if (loot3.IsFree && loot3.Rigidbody != null && loot3.Rigidbody.IsSleeping() && lootToPushSet.Add(loot3))
			{
				lootsToPush.Add(loot3);
			}
		}
		for (int l = 0; l < lootsToPush.Count; l++)
		{
			Loot loot4 = lootsToPush[l];
			Vector3 vector = loot4.transform.position - center.position;
			vector.Normalize();
			loot4.Rigidbody.AddForce(vector * force, ForceMode.Force);
		}
	}
}
