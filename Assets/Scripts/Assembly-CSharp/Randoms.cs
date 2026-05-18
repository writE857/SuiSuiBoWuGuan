using UnityEngine;

public static class Randoms
{
	public static int Sign
	{
		get
		{
			if (!(Random.value > 0.5f))
			{
				return -1;
			}
			return 1;
		}
	}

	public static Vector3 Range(Vector3 a, Vector3 b, bool separateAxis = true)
	{
		Vector3 result = a;
		if (separateAxis)
		{
			result.x = Random.Range(a.x, b.x);
			result.y = Random.Range(a.y, b.y);
			result.z = Random.Range(a.z, b.z);
		}
		else
		{
			result = Vector3.Lerp(a, b, Random.value);
		}
		return result;
	}

	public static float GetRandomBetweenXY(this Vector2 vector)
	{
		return Random.Range(vector.x, vector.y);
	}

	public static int GetRandomBetweenXY(this Vector2Int vector, bool inclusive = false)
	{
		if (inclusive)
		{
			return Random.Range(vector.x, vector.y + 1);
		}
		return Random.Range(vector.x, vector.y);
	}
}
