using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class VisitorNoteManager : Singleton<VisitorNoteManager>
{
	public VisitorNote Prefab;

	public float minInterval = 3f;

	public float maxInterval = 5f;

	public float nextTime;

	public TextAsset TextAsset;

	public List<string> Lines = new List<string>();

	public RectTransform Parent;

	public float chance = 0.1f;

	private void Start()
	{
		Parent.Clear();
		Lines = TextAsset.text.Split("\n").ToList();
		int num = 0;
		while (num < Lines.Count)
		{
			string text = Lines[num];
			text = text.Trim();
			if (string.IsNullOrEmpty(text))
			{
				Lines.RemoveAt(num);
				continue;
			}
			Lines[num] = text;
			num++;
		}
		VisitorManager current = Singleton<VisitorManager>.Current;
		current.OnVisitorStopped = (UnityAction<Visitor, float>)Delegate.Combine(current.OnVisitorStopped, new UnityAction<Visitor, float>(OnVisitorStopped));
	}

	public void Spawn(Visitor visitor, float duration)
	{
		VisitorNote visitorNote = UnityEngine.Object.Instantiate(Prefab, Parent);
		visitorNote.transform.Reset(scale: true);
		visitorNote.transform.position = visitor.transform.position;
		visitorNote.lifeTime = duration;
		visitorNote.Setup(visitor);
	}

	private void OnVisitorStopped(Visitor visitor, float duration)
	{
		if (!(nextTime > Time.time) && !(UnityEngine.Random.value > chance))
		{
			Spawn(visitor, duration);
			nextTime = Time.time + UnityEngine.Random.Range(minInterval, maxInterval);
		}
	}
}
