using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class TicketHolder : MonoBehaviour
{
	public GameObject ticketPrefab;

	public Transform Parent;

	public float yDistance;

	public Vector3 offsetRange = new Vector3(0.01f, 0f, 0.01f);

	public Vector2 yRotRange = new Vector2(-30f, 30f);

	public List<GameObject> tickets = new List<GameObject>();

	public Transform CountCanvas;

	public TMP_Text CountText;

	private void Start()
	{
		Parent.Clear();
		tickets.Clear();
	}

	private void Update()
	{
		while (tickets.Count > SaveManager.Current.SaveData.TicketCount)
		{
			GameObject gameObject = tickets.Last();
			Object.Destroy(gameObject.gameObject);
			tickets.Remove(gameObject);
		}
		while (tickets.Count < SaveManager.Current.SaveData.TicketCount)
		{
			GameObject gameObject2 = Object.Instantiate(ticketPrefab, Parent);
			Vector3 localPosition = new Vector3
			{
				x = Random.Range(0f - offsetRange.x, offsetRange.x),
				y = yDistance * (float)tickets.Count,
				z = Random.Range(0f - offsetRange.z, offsetRange.z)
			};
			Vector3 localEulerAngles = new Vector3(0f, yRotRange.GetRandomBetweenXY(), 0f);
			gameObject2.transform.localEulerAngles = localEulerAngles;
			gameObject2.transform.localPosition = localPosition;
			tickets.Add(gameObject2);
		}
		if (tickets.Count > 0)
		{
			Transform transform = tickets.Last().transform;
			CountCanvas.position = transform.transform.position;
			CountCanvas.rotation = transform.transform.rotation;
			CountText.text = tickets.Count.ToString();
			CountText.enabled = true;
		}
		else
		{
			CountCanvas.position = Parent.transform.position;
			CountCanvas.rotation = Parent.transform.rotation;
			CountText.enabled = false;
		}
	}
}
