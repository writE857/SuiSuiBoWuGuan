using System.Collections.Generic;
using UnityEngine;

public class SpriteTester : MonoBehaviour
{
	public Transform prefab;

	public int Count = 10000;

	public List<Transform> sprites = new List<Transform>();

	public Vector2 size;

	private void Start()
	{
		for (int i = 0; i < Count; i++)
		{
			Transform item = Object.Instantiate(prefab, base.transform);
			item.Reset(scale: true);
			sprites.Add(item);
		}
		sprites.Add(prefab);
	}

	private void Update()
	{
		for (int i = 0; i < sprites.Count; i++)
		{
			Vector2 insideUnitCircle = Random.insideUnitCircle;
			insideUnitCircle.x *= size.x;
			insideUnitCircle.y *= size.y;
			sprites[i].localPosition = insideUnitCircle;
		}
	}
}
