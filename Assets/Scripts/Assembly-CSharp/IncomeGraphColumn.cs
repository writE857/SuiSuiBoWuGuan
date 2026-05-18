using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class IncomeGraphColumn : MonoBehaviour
{
	public IncomeGraph IncomeGraph;

	public IncomeGraphUI IncomeGraphUI;

	public IncomeGraphColumnCell prefab;

	public IncomeBarData BarData;

	public List<IncomeGraphColumnCell> Cells = new List<IncomeGraphColumnCell>();

	public RectTransform Parent;

	public VerticalLayoutGroup VerticalLayoutGroup;

	private float lastPadding;

	public float lerpSpeed = 1f;

	public int CurrentIndex;

	private void Start()
	{
		Parent.Clear();
	}

	public void Init()
	{
		lastPadding = IncomeGraphUI.ySize;
		VerticalLayoutGroup.padding.top = (int)lastPadding;
	}

	private void Update()
	{
		if (BarData != null && !IncomeGraph.BarDatas.Contains(BarData))
		{
			BarData = null;
		}
		if (BarData == null)
		{
			Parent.Clear();
			return;
		}
		int count = BarData.RoomIncomeDatas.Count;
		for (int i = 0; i < count; i++)
		{
			IncomeGraphColumnCell incomeGraphColumnCell = Cells.ElementAtOrDefault(i);
			bool flag = false;
			if (incomeGraphColumnCell == null)
			{
				incomeGraphColumnCell = Object.Instantiate(prefab, Parent);
				incomeGraphColumnCell.IncomeGraphUI = IncomeGraphUI;
				Cells.Add(incomeGraphColumnCell);
				flag = true;
			}
			incomeGraphColumnCell.BarData = BarData;
			RoomIncomeData roomIncomeData = BarData.RoomIncomeDatas.ElementAtOrDefault(i);
			incomeGraphColumnCell.id = roomIncomeData?.ID;
			incomeGraphColumnCell.RoomIncomeData = roomIncomeData;
			if (flag)
			{
				Reorder();
			}
		}
		if (IncomeGraph.currentMax > 0)
		{
			float num = (float)BarData.SumAmount / (float)IncomeGraph.currentMax;
			num = 1f - num;
			float b = num * IncomeGraphUI.ySize;
			lastPadding = Mathf.Lerp(lastPadding, b, Time.deltaTime * lerpSpeed);
			VerticalLayoutGroup.padding.top = (int)lastPadding;
		}
		Color color = ((CurrentIndex == 0) ? IncomeGraphUI.currentBarColor : IncomeGraphUI.defaultColor);
		foreach (IncomeGraphColumnCell cell in Cells)
		{
			if (cell == null)
			{
				continue;
			}
			if (Singleton<HoverInfo>.Current.ArtifactGroup != null && cell.RoomIncomeData.TryGetArtifactGroup() == Singleton<HoverInfo>.Current.ArtifactGroup)
			{
				if (cell == Singleton<HoverInfo>.Current.HoveredCell)
				{
					cell.SetColor(IncomeGraphUI.hoveredCellColor);
				}
				else
				{
					cell.SetColor(IncomeGraphUI.passiveHoveredCellColor);
				}
			}
			else
			{
				cell.SetColor(color);
			}
		}
	}

	private void Reorder()
	{
		List<IncomeGraphColumnCell> list = Cells.OrderBy((IncomeGraphColumnCell a) => a.RoomIncomeData.GroupIndex).ToList();
		for (int num = 0; num < list.Count; num++)
		{
			list[num].transform.SetSiblingIndex(num);
		}
	}
}
