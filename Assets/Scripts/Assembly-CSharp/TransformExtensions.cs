using System.Collections.Generic;
using UnityEngine;

public static class TransformExtensions
{
	public static void SetX(this Vector3 v, float value)
	{
		v[0] = value;
	}

	public static void SetLocalPositionY(this Transform transform, float y)
	{
		Vector3 localPosition = transform.localPosition;
		localPosition.y = y;
		transform.localPosition = localPosition;
	}

	public static void SetLocalRotationXZ(this Transform transform, float x, float z)
	{
		Vector3 localEulerAngles = transform.localEulerAngles;
		localEulerAngles.x = x;
		localEulerAngles.z = z;
		transform.localEulerAngles = localEulerAngles;
	}

	public static Vector3 RoundTo(this Vector3 vector, float rounding)
	{
		vector /= rounding;
		vector = Vector3Int.RoundToInt(vector);
		vector *= rounding;
		return vector;
	}

	public static Vector3 X0Y(this Vector2 vector)
	{
		return new Vector3(vector.x, 0f, vector.y);
	}

	public static Vector3 X0Z(this Vector3 vector)
	{
		return new Vector3(vector.x, 0f, vector.z);
	}

	public static List<Transform> Children(this Transform transform)
	{
		List<Transform> list = new List<Transform>();
		for (int i = 0; i < transform.childCount; i++)
		{
			list.Add(transform.GetChild(i));
		}
		return list;
	}

	public static void CopyPositionAndRotation(this Transform transform, Transform from)
	{
		if (!(transform == null) && !(from == null))
		{
			transform.SetPositionAndRotation(from.position, from.rotation);
		}
	}

	public static void DisableAllChildren(this Transform transform)
	{
		int childCount = transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			Transform child = transform.GetChild(i);
			if (child.gameObject.activeSelf)
			{
				child.gameObject.SetActive(value: false);
			}
		}
	}

	public static void Reset(this Transform transform, bool scale = false)
	{
		if (scale)
		{
			transform.localScale = Vector3.one;
		}
		transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
	}

	public static int Clear<T>(this Transform transform, bool includeInactive = true) where T : MonoBehaviour
	{
		T[] componentsInChildren = transform.GetComponentsInChildren<T>(includeInactive);
		int num = 0;
		T[] array = componentsInChildren;
		foreach (T val in array)
		{
			num++;
			Object.Destroy(val.gameObject);
		}
		return num;
	}

	public static int Clear(this Transform transform)
	{
		bool flag = !Application.isPlaying;
		List<Transform> list = new List<Transform>();
		int childCount = transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			list.Add(transform.GetChild(i));
		}
		if (flag)
		{
			list.ForEach(delegate(Transform a)
			{
				Object.DestroyImmediate(a.gameObject);
			});
		}
		else
		{
			list.ForEach(delegate(Transform a)
			{
				Object.Destroy(a.gameObject);
			});
		}
		return childCount;
	}

	public static void GetCapsulePoints(this CapsuleCollider col, out Vector3 p1, out Vector3 p2)
	{
		Vector3 vector = col.transform.TransformPoint(col.center);
		Vector3 vector2 = col.direction switch
		{
			0 => col.transform.right, 
			1 => col.transform.up, 
			2 => col.transform.forward, 
			_ => col.transform.up, 
		};
		float num = Mathf.Max(0f, col.height * 0.5f - col.radius);
		p1 = vector + vector2 * num;
		p2 = vector - vector2 * num;
	}

	public static bool IsInside(this Bounds bounds, Bounds other)
	{
		if (other.min.x >= bounds.min.x && other.max.x <= bounds.max.x && other.min.y >= bounds.min.y && other.max.y <= bounds.max.y && other.min.z >= bounds.min.z)
		{
			return other.max.z <= bounds.max.z;
		}
		return false;
	}

	public static float WorldRadius(this SphereCollider sphere)
	{
		float num = Mathf.Max(sphere.transform.lossyScale.x, sphere.transform.lossyScale.y, sphere.transform.lossyScale.z);
		return sphere.radius * num;
	}

	public static bool IsPointInside(this BoxCollider box, Vector3 point)
	{
		Vector3 vector = box.transform.InverseTransformPoint(point);
		vector -= box.center;
		Vector3 vector2 = box.size * 0.5f;
		if (Mathf.Abs(vector.x) <= vector2.x && Mathf.Abs(vector.y) <= vector2.y)
		{
			return Mathf.Abs(vector.z) <= vector2.z;
		}
		return false;
	}

	public static Vector3 WorldSize(this BoxCollider boxCollider)
	{
		return Vector3.Scale(boxCollider.size, boxCollider.transform.lossyScale);
	}
}
