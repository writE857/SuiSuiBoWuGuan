using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class IncomeGraphUI : MonoBehaviour
{
	public IncomeGraph IncomeGraph;

	public IncomeGraphColumn prefab;

	public int maxColumns = 8;

	public List<IncomeGraphColumn> Columns = new List<IncomeGraphColumn>();

	public RectTransform Parent;

	public float ySize = 100f;

	public Color defaultColor;

	public Color currentBarColor;

	public Color hoveredCellColor;

	public Color passiveHoveredCellColor;

	public TMP_Text TitleText;

	public float TitleDuration = 0.2f;

	private string lastText = "";

	public RoomIncomeData HoveredRoomIncomeData
	{
		get
		{
			if (!(Singleton<HoverInfo>.Current.HoveredCell == null))
			{
				return Singleton<HoverInfo>.Current.HoveredCell.RoomIncomeData;
			}
			return null;
		}
	}

	private void Start()
	{
		OnRestart();
		GameEvents current = Singleton<GameEvents>.Current;
		current.OnRestart = (UnityAction)Delegate.Combine(current.OnRestart, new UnityAction(OnRestart));
	}

	private void OnRestart()
	{
		Parent.Clear();
		Columns.Clear();
		RectTransform component = GetComponent<RectTransform>();
		ySize = component.rect.size.y;
		for (int i = 0; i < maxColumns; i++)
		{
			IncomeGraphColumn incomeGraphColumn = UnityEngine.Object.Instantiate(prefab, Parent);
			incomeGraphColumn.IncomeGraph = IncomeGraph;
			incomeGraphColumn.IncomeGraphUI = this;
			incomeGraphColumn.Init();
			Columns.Insert(0, incomeGraphColumn);
		}
	}

	private void Update()
	{
		for (int i = 0; i < maxColumns; i++)
		{
			IncomeBarData data = IncomeGraph.BarDatas.ElementAtOrDefault(i);
			if (!Columns.Exists((IncomeGraphColumn a) => a.BarData == data))
			{
				IncomeGraphColumn incomeGraphColumn = UnityEngine.Object.Instantiate(prefab, Parent);
				incomeGraphColumn.BarData = data;
				incomeGraphColumn.IncomeGraph = IncomeGraph;
				incomeGraphColumn.IncomeGraphUI = this;
				incomeGraphColumn.Init();
				Columns.Insert(0, incomeGraphColumn);
			}
		}
		while (Columns.Count > maxColumns)
		{
			UnityEngine.Object.Destroy(Columns.Last().gameObject);
			Columns.Remove(Columns.Last());
		}
		Columns = Columns.OrderByDescending((IncomeGraphColumn a) => (a.BarData != null) ? a.BarData.startTime : (-100)).ToList();
		for (int num = 0; num < Columns.Count; num++)
		{
			Columns[num].transform.SetSiblingIndex(num);
			Columns[num].CurrentIndex = num;
		}
		if (HoveredRoomIncomeData == null)
		{
			SetTitleText("每小时明细");
			return;
		}
		ArtifactGroup artifactGroup = HoveredRoomIncomeData.TryGetArtifactGroup();
		if (artifactGroup != null)
		{
			SetTitleText(artifactGroup.DisplayName + "\n$" + NumFormat.ToM1Decimal(HoveredRoomIncomeData.Income));
		}
		else if (HoveredRoomIncomeData.ID == ArtifactGroup.CoinGroup.GROUPID)
		{
			SetTitleText($"幸运币\n${HoveredRoomIncomeData.Income}");
		}
		else if (HoveredRoomIncomeData.ID == ArtifactGroup.SellGroup.GROUPID)
		{
			SetTitleText($"已售矿物\n${HoveredRoomIncomeData.Income}");
		}
		else if (HoveredRoomIncomeData.ID == ArtifactGroup.HammerGroup.GROUPID)
		{
			SetTitleText($"锤子\n${HoveredRoomIncomeData.Income}");
		}
	}

	private void SetTitleText(string newText)
	{
		if (!(lastText == newText))
		{
			lastText = newText;
			TitleText.DOText(newText, TitleDuration).SetEase(Ease.Linear);
		}
	}
}
