using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LootGeneratorTester : MonoBehaviour
{
	public LootGenerator LootGenerator;

	[Range(10f, 1000f)]
	public int Samples = 10;

	[Header("Count")]
	public int CountAverage;

	public int CountMean;

	public Vector2Int CountMinMax;

	[Header("Value")]
	public int ValueAverage;

	public int ValueMean;

	public Vector2Int ValueMinMax;

	private void Test()
	{
		List<int> list = new List<int>();
		CountAverage = 0;
		CountMean = 0;
		CountMinMax = default(Vector2Int);
		List<int> list2 = new List<int>();
		ValueAverage = 0;
		ValueMean = 0;
		ValueMinMax = default(Vector2Int);
		for (int i = 0; i < Samples; i++)
		{
			LootGenerator.GenerateLoot();
			list.Add(LootGenerator.Loots.Count);
			list2.Add(LootGenerator.Loots.Sum((Loot a) => a.FinalValue));
		}
		CountAverage = (int)list.Average();
		CountMean = list.OrderBy((int a) => a).ElementAt(list.Count / 2);
		CountMinMax.x = list.Min();
		CountMinMax.y = list.Max();
		ValueAverage = (int)list2.Average();
		ValueMean = list2.OrderBy((int a) => a).ElementAt(list2.Count / 2);
		ValueMinMax.x = list2.Min();
		ValueMinMax.y = list2.Max();
	}
}
