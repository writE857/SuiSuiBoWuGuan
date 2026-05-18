using UnityEngine;

public static class GameObjectExtensions
{
	public static bool IsInLayerMask(this GameObject obj, LayerMask layerMask)
	{
		return (layerMask.value & (1 << obj.layer)) != 0;
	}

	public static void SetActiveSmart(this GameObject target, bool newState)
	{
		if (!(target == null) && target.activeSelf != newState)
		{
			target.SetActive(newState);
		}
	}

	public static T[] FindObjectsByType<T>(this MonoBehaviour go) where T : MonoBehaviour
	{
		return Object.FindObjectsByType<T>(FindObjectsSortMode.None);
	}
}
