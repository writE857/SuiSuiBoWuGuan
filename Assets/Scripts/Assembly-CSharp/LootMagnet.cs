using UnityEngine;

public class LootMagnet : MonoBehaviour
{
	public bool IsPulling;

	private void OnTriggerStay(Collider other)
	{
		if (!IsPulling)
		{
			return;
		}
		Loot componentInParent = other.GetComponentInParent<Loot>();
		if (componentInParent != null && componentInParent.IsFree && componentInParent.CanBeTaken)
		{
			if (componentInParent.Artifact != null)
			{
				Singleton<LootManager>.Current.InstantSellArtifact(componentInParent.Artifact);
			}
			else
			{
				Singleton<LootManager>.Current.InstantSell(componentInParent);
			}
		}
	}
}
