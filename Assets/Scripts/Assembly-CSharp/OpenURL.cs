using UnityEngine;

public class OpenURL : MonoBehaviour
{
	public string url;

	public void DoOpenURL()
	{
		Application.OpenURL(url);
	}
}
