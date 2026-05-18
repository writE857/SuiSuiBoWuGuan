using System.Collections.Generic;
using UnityEngine;

public class ImageTester : MonoBehaviour
{
	public int Count = 1000;

	public RectTransform prefab;

	public List<RectTransform> images = new List<RectTransform>();

	public RectTransform Rect;

	private void Start()
	{
		for (int i = 0; i < Count; i++)
		{
			RectTransform item = Object.Instantiate(prefab, Rect);
			item.Reset(scale: true);
			images.Add(item);
		}
		images.Add(prefab);
	}

	private void Update()
	{
		Vector2 size = Rect.rect.size;
		for (int i = 0; i < images.Count; i++)
		{
			Vector2 insideUnitCircle = Random.insideUnitCircle;
			insideUnitCircle.x *= size.x;
			insideUnitCircle.y *= size.y;
			images[i].localPosition = insideUnitCircle;
		}
	}
}
