using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class BrickShop : Singleton<BrickShop>
{
	public RectTransform Parent;

	public RectTransform HideTransform;

	public float moveDuration = 0.2f;

	public float hiddenY = -50f;

	public float shownY;

	public BrickBuyButton BrickButtonPrefab;

	private List<GameObject> Buttons = new List<GameObject>();

	public bool IsHidden;

	public bool IsDown;

	public PrestigeSkill PerfectSkill;

	public PerfectBuyButton PerfectBuyButtonPref;

	public PerfectBuyButton PerfectInstance;

	public NuclearStone NuclearStone;

	public float nextBuy;

	public float buyInterval = 0.3f;

	public bool canBuy => nextBuy < Time.time;

	private void Start()
	{
		RefreshUI();
		GoDown();
		IsHidden = true;
		StartCoroutine(Do_Nuclear());
	}

	private IEnumerator Do_Nuclear()
	{
		NuclearStone.gameObject.SetActive(value: true);
		yield return null;
		NuclearStone.gameObject.SetActive(value: false);
	}

	private void Update()
	{
		RefreshUI();
		IsHidden = Singleton<GameSession>.Current.IsInMenu || Singleton<ArtifactOverlayDisplay>.Current.IsOpen || NuclearStone.IsBought;
		if (IsHidden != IsDown)
		{
			if (IsHidden)
			{
				GoDown();
			}
			else
			{
				GoUp();
			}
			IsDown = IsHidden;
		}
	}

	private void RefreshUI()
	{
		int num = brickCount();
		num += (PerfectSkill.IsUnlocked ? 1 : 0);
		if (Buttons.Count == num)
		{
			return;
		}
		Parent.Clear();
		Buttons.Clear();
		for (int i = 0; i < Singleton<GameResources>.Current.Artifacts.Groups.Count; i++)
		{
			ArtifactGroup artifactGroup = Singleton<GameResources>.Current.Artifacts.Groups[i];
			BrickBuyButton brickBuyButton = Object.Instantiate(BrickButtonPrefab, Parent);
			brickBuyButton.transform.Reset();
			brickBuyButton.ArtifactGroup = artifactGroup;
			Buttons.Add(brickBuyButton.gameObject);
			if (!artifactGroup.SaveData.IsUnlocked)
			{
				break;
			}
		}
		if (PerfectSkill.IsUnlocked)
		{
			PerfectBuyButton perfectBuyButton = Object.Instantiate(PerfectBuyButtonPref, Parent);
			perfectBuyButton.transform.Reset();
			Buttons.Add(perfectBuyButton.gameObject);
		}
	}

	private int brickCount()
	{
		int num = 0;
		for (int i = 0; i < Singleton<GameResources>.Current.Artifacts.Groups.Count; i++)
		{
			ArtifactGroup artifactGroup = Singleton<GameResources>.Current.Artifacts.Groups[i];
			num++;
			if (!artifactGroup.SaveData.IsUnlocked)
			{
				break;
			}
		}
		return num;
	}

	private void GoUp()
	{
		HideTransform.DOAnchorPosY(shownY, moveDuration).SetEase(Ease.OutQuad);
	}

	private void GoDown()
	{
		HideTransform.DOAnchorPosY(hiddenY, moveDuration).SetEase(Ease.OutQuad);
	}
}
