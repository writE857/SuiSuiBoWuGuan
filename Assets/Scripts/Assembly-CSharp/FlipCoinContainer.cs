using UnityEngine;

public class FlipCoinContainer : MonoBehaviour
{
	public PrestigeSkill FlipCoinSkill;

	public GameObject Content;

	private void Update()
	{
		Content.SetActiveSmart(FlipCoinSkill.IsUnlocked);
	}
}
