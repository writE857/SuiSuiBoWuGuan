using UnityEngine;

public class OnlyWebGL : MonoBehaviour
{
	private void OnEnable()
	{
		base.gameObject.SetActive(value: false);
	}
}
