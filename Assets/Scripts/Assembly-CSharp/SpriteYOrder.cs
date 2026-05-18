using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteYOrder : MonoBehaviour
{
	public SpriteRenderer Renderer;

	public List<SpriteRenderer> SyncRenderers = new List<SpriteRenderer>();

	private const float orderMultiplier = 30000f;

	private void OnEnable()
	{
		StartCoroutine(Do_Order());
	}

	private IEnumerator Do_Order()
	{
		yield return null;
		yield return null;
		if (Renderer == null)
		{
			Renderer = GetComponent<SpriteRenderer>();
		}
		float num = Mathf.InverseLerp(Singleton<VisitorManager>.Current.TopZ, Singleton<VisitorManager>.Current.BotZ, base.transform.position.z);
		int order = (int)(num * 30000f);
		Renderer.sortingOrder = order;
		SyncRenderers.ForEach(delegate(SpriteRenderer a)
		{
			a.sortingOrder = order;
		});
	}
}
