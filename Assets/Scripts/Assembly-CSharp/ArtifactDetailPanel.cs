using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ArtifactDetailPanel : MonoBehaviour
{
	public TMP_Text TitleText;

	public TMP_Text GroupLevelText;

	public TMP_Text LevelText;

	public TMP_Text XPText;

	public TMP_Text ValueText;

	public TMP_Text DescriptionText;

	public Image Icon;

	public Image XPFill;

	public float TitleDuration = 0.2f;

	public Color moneyColor;

	private Dictionary<TMP_Text, string> textValues = new Dictionary<TMP_Text, string>();

	private float lastXPFill = -1f;

	private Sprite lastIcon;

	private void OnEnable()
	{
		HoverInfo current = Singleton<HoverInfo>.Current;
		current.HoverStateChanged += RefreshDisplayedArtifact;
		GameEvents current2 = Singleton<GameEvents>.Current;
		current2.OnArtifactLevelUp += OnArtifactChanged;
		current2.OnArtifactUnlocked += OnArtifactChanged;
		current2.OnArtifactGroupLevelUp += OnArtifactGroupChanged;
		current2.OnUpgradeBought += OnUpgradeBought;
		current2.OnRestart += ClearDisplay;
		current2.OnPrestigeChange += ClearDisplay;
		ClearDisplay();
		RefreshDisplayedArtifact();
	}

	private void OnDisable()
	{
		HoverInfo hoverInfo = UnityEngine.Object.FindFirstObjectByType<HoverInfo>(FindObjectsInactive.Include);
		if (hoverInfo != null)
		{
			hoverInfo.HoverStateChanged -= RefreshDisplayedArtifact;
		}
		GameEvents gameEvents = UnityEngine.Object.FindFirstObjectByType<GameEvents>(FindObjectsInactive.Include);
		if (gameEvents != null)
		{
			gameEvents.OnArtifactLevelUp -= OnArtifactChanged;
			gameEvents.OnArtifactUnlocked -= OnArtifactChanged;
			gameEvents.OnArtifactGroupLevelUp -= OnArtifactGroupChanged;
			gameEvents.OnUpgradeBought -= OnUpgradeBought;
			gameEvents.OnRestart -= ClearDisplay;
			gameEvents.OnPrestigeChange -= ClearDisplay;
		}
	}

	private void ClearDisplay()
	{
		TitleText.text = "";
		GroupLevelText.text = "";
		LevelText.text = "";
		XPText.text = "";
		ValueText.text = "";
		DescriptionText.text = "";
		Icon.enabled = false;
		Icon.sprite = null;
		lastXPFill = -1f;
		lastIcon = null;
		textValues.Clear();
	}

	private void RefreshDisplayedArtifact()
	{
		Artifact currentHoveredArtifact = Singleton<HoverInfo>.Current.CurrentHoveredArtifact;
		if (currentHoveredArtifact == null)
		{
			return;
		}
		SetText(TitleText, currentHoveredArtifact.DisplayName);
		SetText(GroupLevelText, currentHoveredArtifact.ArtifactGroup.DisplayName);
		SetText(DescriptionText, currentHoveredArtifact.Description);
		if (currentHoveredArtifact.IsMaxLevel)
		{
			SetText(LevelText, "文物等级 <color=#" + ColorUtility.ToHtmlStringRGBA(moneyColor) + ">满级</color>");
			SetText(XPText, "满级");
		}
		else
		{
			SetText(XPText, $"{currentHoveredArtifact.CurrentLevelXP}/{currentHoveredArtifact.CurrentXPRequired} 碎片");
			SetText(LevelText, $"文物等级 {currentHoveredArtifact.Level}");
		}
		SetText(ValueText, $"价值：${currentHoveredArtifact.CurrentValue}");
		float xPRatio = currentHoveredArtifact.XPRatio;
		if (!Mathf.Approximately(lastXPFill, xPRatio))
		{
			lastXPFill = xPRatio;
			XPFill.fillAmount = xPRatio;
		}
		if (currentHoveredArtifact.MuseumImage != lastIcon)
		{
			lastIcon = currentHoveredArtifact.MuseumImage;
			Icon.enabled = lastIcon != null;
			Icon.sprite = lastIcon;
		}
	}

	private void OnArtifactChanged(Artifact artifact)
	{
		if (Singleton<HoverInfo>.Current.CurrentHoveredArtifact == artifact)
		{
			RefreshDisplayedArtifact();
		}
	}

	private void OnArtifactGroupChanged(ArtifactGroup artifactGroup)
	{
		Artifact currentHoveredArtifact = Singleton<HoverInfo>.Current.CurrentHoveredArtifact;
		if (currentHoveredArtifact != null && currentHoveredArtifact.ArtifactGroup == artifactGroup)
		{
			RefreshDisplayedArtifact();
		}
	}

	private void OnUpgradeBought(string upgradeName, int newLevel)
	{
		if (Singleton<HoverInfo>.Current.CurrentHoveredArtifact != null)
		{
			RefreshDisplayedArtifact();
		}
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
