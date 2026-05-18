using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ListExtensions
{
	public static T GetRandom<T>(this List<T> list)
	{
		T result = default(T);
		if (list == null || list.Count == 0)
		{
			return result;
		}
		return list[Random.Range(0, list.Count)];
	}

	public static T GetRandom<T>(this IEnumerable<T> list)
	{
		return list.ToList().GetRandom();
	}

	public static bool AddIfNew<T>(this List<T> list, T item)
	{
		if (list.Contains(item))
		{
			return false;
		}
		list.Add(item);
		return true;
	}

	public static int RemoveNulls<T>(this List<T> list)
	{
		return list.RemoveAll((T a) => a == null);
	}

	public static int IndexUntilSum(this List<int> list, int number)
	{
		int num = 0;
		int num2 = 0;
		foreach (int item in list)
		{
			num2 += item;
			num++;
			if (num2 >= number)
			{
				break;
			}
		}
		return num;
	}

	public static int LeftUntilSum(this List<int> list, int number)
	{
		int num = 0;
		int num2 = 0;
		foreach (int item in list)
		{
			num2 += item;
			num++;
			if (num2 >= number)
			{
				break;
			}
		}
		return number - num2 + list[num];
	}
}
