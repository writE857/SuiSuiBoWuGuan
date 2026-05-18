using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ArtifactGroupDetailPanel : MonoBehaviour
{
	public TMP_Text TitleText;

	public TMP_Text GroupLevelText;

	public TMP_Text GroupXPText;

	public TMP_Text StoneContentText;

	public TMP_Text StoneCompleteText;

	public TMP_Text ArtifactsText;

	public TMP_Text DescriptionText;

	public float TitleDuration = 0.2f;

	public Image LootFoundFill;

	public Image XPFill;

	private Color lootFillColor;

	private Color xpFillColor;

	public GameObject xpBar;

	public Color moneyColor;

	private float lastLootFill = -1f;

	private float lastXPFill = -1f;

	private Dictionary<TMP_Text, string> textValues = new Dictionary<TMP_Text, string>();

	private void OnEnable()
	{
		GameEvents current = Singleton<GameEvents>.Current;
		current.OnRestart += OnRestart;
		current.OnPrestigeChange += OnRestart;
		current.OnArtifactLevelUp += OnArtifactChanged;
		current.OnArtifactUnlocked += OnArtifactChanged;
		current.OnArtifactGroupLevelUp += OnArtifactGroupChanged;
		current.OnLootPickedUp += OnLootChanged;
		current.OnLootSold += OnLootChanged;
		current.OnLootExtracted += OnLootChanged;
		current.OnUpgradeBought += OnUpgradeBought;
		Singleton<HoverInfo>.Current.HoverStateChanged += RefreshDisplayedGroup;
		OnRestart();
		RefreshDisplayedGroup();
	}

	private void OnDisable()
	{
		GameEvents gameEvents = UnityEngine.Object.FindFirstObjectByType<GameEvents>(FindObjectsInactive.Include);
		if (gameEvents != null)
		{
			gameEvents.OnRestart -= OnRestart;
			gameEvents.OnPrestigeChange -= OnRestart;
			gameEvents.OnArtifactLevelUp -= OnArtifactChanged;
			gameEvents.OnArtifactUnlocked -= OnArtifactChanged;
			gameEvents.OnArtifactGroupLevelUp -= OnArtifactGroupChanged;
			gameEvents.OnLootPickedUp -= OnLootChanged;
			gameEvents.OnLootSold -= OnLootChanged;
			gameEvents.OnLootExtracted -= OnLootChanged;
			gameEvents.OnUpgradeBought -= OnUpgradeBought;
		}
		HoverInfo hoverInfo = UnityEngine.Object.FindFirstObjectByType<HoverInfo>(FindObjectsInactive.Include);
		if (hoverInfo != null)
		{
			hoverInfo.HoverStateChanged -= RefreshDisplayedGroup;
		}
	}

	private void OnRestart()
	{
		lootFillColor = LootFoundFill.color;
		xpFillColor = XPFill.color;
		StoneCompleteText.gameObject.SetActiveSmart(newState: false);
		StoneContentText.gameObject.SetActiveSmart(newState: true);
		TitleText.text = "欢迎来到博物馆！";
		StoneContentText.text = "1. 先购买石块";
		ArtifactsText.text = "\n2. 开始砸碎";
		ArtifactsText.text += "\n3. 收集掉落物";
		ArtifactsText.text += "\n4. ???";
		ArtifactsText.text += "\n5. 获利";
		DescriptionText.text = "6. 看数字上涨...";
		LootFoundFill.fillAmount = 0f;
		XPFill.fillAmount = 0f;
		xpBar.gameObject.SetActiveSmart(newState: false);
		GroupLevelText.gameObject.SetActiveSmart(newState: false);
		textValues.Clear();
		lastLootFill = -1f;
		lastXPFill = -1f;
	}

	private void RefreshDisplayedGroup()
	{
		if (!(Singleton<HoverInfo>.Current.CurrentBrickArtifactGroup != null))
		{
			return;
		}
		ArtifactGroup artifactGroup = Singleton<HoverInfo>.Current.CurrentBrickArtifactGroup;
		if (Singleton<HoverInfo>.Current.CurrentHoveredArtifactGroup != null)
		{
			artifactGroup = Singleton<HoverInfo>.Current.CurrentHoveredArtifactGroup;
		}
		xpBar.gameObject.SetActiveSmart(newState: true);
		GroupLevelText.gameObject.SetActiveSmart(newState: true);
		SetText(TitleText, artifactGroup.DisplayName);
		if (!(Singleton<BrickTable>.Current.CurrentBrick != null))
		{
			return;
		}
		Brick currentBrick = Singleton<BrickTable>.Current.CurrentBrick;
		int startAllLootCount = currentBrick.LootGenerator.StartAllLootCount;
		int num = startAllLootCount - currentBrick.LootGenerator.AllLootCount;
		SetText(StoneContentText, $"已发现掉落 {num}/{startAllLootCount}");
		float num2 = (float)num / (float)startAllLootCount;
		if (lastLootFill != num2)
		{
			lastLootFill = num2;
			LootFoundFill.DOFillAmount(num2, TitleDuration);
			LootFoundFill.color = Color.white;
			LootFoundFill.DOColor(lootFillColor, TitleDuration);
		}
		if (startAllLootCount == num)
		{
			if (!StoneCompleteText.isActiveAndEnabled)
			{
				StoneCompleteText.gameObject.SetActiveSmart(newState: true);
				StoneContentText.gameObject.SetActiveSmart(newState: false);
				StoneCompleteText.text = "";
				textValues[StoneCompleteText] = "";
				SetText(StoneCompleteText, "石块完成！");
			}
		}
		else
		{
			StoneCompleteText.gameObject.SetActiveSmart(newState: false);
			StoneContentText.gameObject.SetActiveSmart(newState: true);
		}
		if (artifactGroup.IsMaxLevel)
		{
			SetText(GroupLevelText, "收藏等级 <color=#" + ColorUtility.ToHtmlStringRGBA(moneyColor) + ">满级</color>");
			SetText(GroupXPText, "满级");
		}
		else
		{
			SetText(GroupLevelText, $"收藏等级 {artifactGroup.Level}");
			SetText(GroupXPText, $"{artifactGroup.CurrentLevelXP}/{artifactGroup.CurrentXPRequired} 碎片");
		}
		float xPRatio = artifactGroup.XPRatio;
		if (lastXPFill != xPRatio)
		{
			lastXPFill = xPRatio;
			XPFill.DOFillAmount(xPRatio, TitleDuration);
			XPFill.color = Color.white;
			XPFill.DOColor(xpFillColor, TitleDuration);
		}
		string text = "";
		foreach (Artifact artifact in artifactGroup.Artifacts)
		{
			text = ((!artifact.IsUnlocked) ? ((!artifactGroup.IsAvailable(artifact)) ? (text + $"需要等级 {artifactGroup.UnlockedAt(artifact)}\n") : (text + "尚未发现！\n")) : ((!artifact.IsMaxLevel) ? (text + string.Format("<color=#99D2FF>{0}级</color> {1} <color=#{2}>${3}</color>\n", artifact.Level, artifact.DisplayName, ColorUtility.ToHtmlStringRGBA(moneyColor), artifact.CurrentValue.ToString("N0"))) : (text + "<color=#" + ColorUtility.ToHtmlStringRGBA(moneyColor) + ">满级</color> " + artifact.DisplayName + " <color=#" + ColorUtility.ToHtmlStringRGBA(moneyColor) + ">$" + artifact.CurrentValue.ToString("N0") + "</color>\n")));
		}
		text = text.Trim();
		SetText(ArtifactsText, text);
		SetText(DescriptionText, artifactGroup.Description);
	}

	private void OnArtifactChanged(Artifact artifact)
	{
		ArtifactGroup artifactGroup = GetCurrentArtifactGroup();
		if (artifactGroup != null && artifact.ArtifactGroup == artifactGroup)
		{
			RefreshDisplayedGroup();
		}
	}

	private void OnArtifactGroupChanged(ArtifactGroup artifactGroup)
	{
		if (artifactGroup == GetCurrentArtifactGroup())
		{
			RefreshDisplayedGroup();
		}
	}

	private void OnLootChanged(Loot loot)
	{
		if (Singleton<BrickTable>.Current.CurrentBrick != null)
		{
			RefreshDisplayedGroup();
		}
	}

	private void OnUpgradeBought(string upgradeName, int newLevel)
	{
		if (GetCurrentArtifactGroup() != null)
		{
			RefreshDisplayedGroup();
		}
	}

	private ArtifactGroup GetCurrentArtifactGroup()
	{
		ArtifactGroup currentBrickArtifactGroup = Singleton<HoverInfo>.Current.CurrentBrickArtifactGroup;
		if (currentBrickArtifactGroup == null)
		{
			return null;
		}
		if (Singleton<HoverInfo>.Current.CurrentHoveredArtifactGroup != null)
		{
			return Singleton<HoverInfo>.Current.CurrentHoveredArtifactGroup;
		}
		return currentBrickArtifactGroup;
	}

	private void SetText(TMP_Text text, string newText)
	{
		string text2 = "";
		if (textValues.ContainsKey(text))
		{
			text2 = textValues[text];
		}
		else
		{
			textValues.Add(text, "");
		}
		if (!(text2 == newText))
		{
			text2 = newText;
			textValues[text] = text2;
			text.DOText(newText, TitleDuration).SetEase(Ease.Linear);
		}
	}
}
