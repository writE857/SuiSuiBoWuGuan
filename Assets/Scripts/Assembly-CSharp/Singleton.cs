using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
	protected static T _current;

	public static T Current
	{
		get
		{
			if (!(_current != null))
			{
				return _current = Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
			}
			return _current;
		}
	}
}
