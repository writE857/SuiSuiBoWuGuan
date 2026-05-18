using UnityEngine;

public class DisableWhenInMenu : MonoBehaviour
{
	public GameObject Content;

	private void Update()
	{
		Content.SetActiveSmart(!Singleton<GameSession>.Current.IsInMenu);
	}
}
