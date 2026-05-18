using System.Collections.Generic;
using UnityEngine;

public class PrestigeLineManager : Singleton<PrestigeLineManager>
{
	public PrestigeLine LinePrefab;

	public RectTransform parent;

	public List<PrestigeLine> Lines = new List<PrestigeLine>();

	private void Start()
	{
		parent.Clear();
	}

	private void Update()
	{
		Lines.RemoveNulls();
		Lines.RemoveAll((PrestigeLine a) => a.To == null);
		Lines.RemoveAll((PrestigeLine a) => a.From == null);
		foreach (PrestigeNode entry in PrestigeNode.Entries)
		{
			TryAddLine(entry.From, entry);
		}
	}

	private void TryAddLine(PrestigeNode from, PrestigeNode to)
	{
		if (!(from == null) && !(to == null) && !Lines.Exists((PrestigeLine a) => to == a.To && from == a.From))
		{
			PrestigeLine prestigeLine = Object.Instantiate(LinePrefab, parent);
			prestigeLine.To = to;
			prestigeLine.From = from;
			Lines.Add(prestigeLine);
			prestigeLine.Init();
		}
	}
}
