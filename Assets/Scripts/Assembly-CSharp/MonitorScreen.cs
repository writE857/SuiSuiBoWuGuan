using System.Collections.Generic;
using UnityEngine;

public class MonitorScreen : MonoBehaviour
{
	public GameObject Target;

	public List<GameObject> Targets = new List<GameObject>();

	public CanvasGroup CanvasGroup;

	public void Activate()
	{
		if (!base.gameObject.activeSelf)
		{
			base.gameObject.SetActive(value: true);
		}
		if (Target != null)
		{
			Target.SetActiveSmart(newState: true);
		}
		Targets.ForEach(delegate(GameObject a)
		{
			if (a != null)
			{
				a.SetActiveSmart(newState: true);
			}
		});
		if (CanvasGroup != null)
		{
			CanvasGroup.alpha = 1f;
			CanvasGroup.blocksRaycasts = true;
			CanvasGroup.interactable = true;
		}
	}

	public void Hide()
	{
		if (Target != null)
		{
			Target.SetActiveSmart(newState: false);
		}
		Targets.ForEach(delegate(GameObject a)
		{
			if (a != null)
			{
				a.SetActiveSmart(newState: false);
			}
		});
		if (CanvasGroup != null)
		{
			CanvasGroup.alpha = 0f;
			CanvasGroup.blocksRaycasts = false;
			CanvasGroup.interactable = false;
		}
	}
}
