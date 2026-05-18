using TMPro;
using UnityEngine;

public class BrickLootCountText : MonoBehaviour
{
	public TMP_Text Text;

	private Brick lastBrick;

	private int lastFoundCount = int.MinValue;

	private int lastStartCount = int.MinValue;

	private bool lastHadBrick;

	private void OnEnable()
	{
		lastBrick = null;
		lastFoundCount = int.MinValue;
		lastStartCount = int.MinValue;
		lastHadBrick = false;
		RefreshText();
	}

	private void Update()
	{
		RefreshText();
	}

	private void RefreshText()
	{
		Brick currentBrick = Singleton<BrickTable>.Current.CurrentBrick;
		bool hasBrick = currentBrick != null;
		if (hasBrick)
		{
			int foundCount = currentBrick.LootGenerator.StartCollectibleCount - currentBrick.LootGenerator.CollectibleLootCount;
			int startCount = currentBrick.LootGenerator.StartCollectibleCount;
			if (lastHadBrick && currentBrick == lastBrick && foundCount == lastFoundCount && startCount == lastStartCount)
			{
				return;
			}
			lastHadBrick = true;
			lastBrick = currentBrick;
			lastFoundCount = foundCount;
			lastStartCount = startCount;
			Text.text = foundCount + "/" + startCount + " 已发现";
		}
		else if (lastHadBrick || Text.text != "先购买石块！")
		{
			lastHadBrick = false;
			lastBrick = null;
			lastFoundCount = int.MinValue;
			lastStartCount = int.MinValue;
			Text.text = "先购买石块！";
		}
	}
}
