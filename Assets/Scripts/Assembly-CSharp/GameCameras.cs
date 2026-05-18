using UnityEngine;

public class GameCameras : Singleton<GameCameras>
{
	public GameObject MenuCamera;

	public GameObject GameCamera;

	private void Start()
	{
		MenuCamera.SetActive(value: true);
		GameCamera.SetActive(value: false);
	}

	private void Update()
	{
		MenuCamera.SetActiveSmart(Singleton<GameSession>.Current.IsInMenu);
		GameCamera.SetActiveSmart(!Singleton<GameSession>.Current.IsInMenu);
	}
}
