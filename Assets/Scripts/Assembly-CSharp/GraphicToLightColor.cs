using UnityEngine;
using UnityEngine.UI;

public class GraphicToLightColor : MonoBehaviour
{
	public Graphic Graphic;

	public Light Light;

	public float ValueMultiplier = 0.05f;

	private void OnEnable()
	{
		if (Light == null)
		{
			Light = GetComponent<Light>();
		}
	}

	private void Update()
	{
		if (!(Light == null) && !(Graphic == null))
		{
			Light.color = Graphic.color * ValueMultiplier;
		}
	}
}
