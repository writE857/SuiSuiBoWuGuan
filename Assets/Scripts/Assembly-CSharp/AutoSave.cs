using System.Collections;
using UnityEngine;

public class AutoSave : MonoBehaviour
{
	public int intervalSeconds = 15;

	private void Start()
	{
		StartCoroutine(Do_AutoSave());
	}

	private IEnumerator Do_AutoSave()
	{
		while (true)
		{
			yield return new WaitForSeconds(intervalSeconds);
			if (Singleton<GameSession>.Current.IsGameStarted)
			{
				SaveManager.Current.Save();
				Debug.Log("Autosave");
			}
		}
	}
}
