using TMPro;
using UnityEngine;

public class ArtifactIncomeText : MonoBehaviour
{
	public TMP_Text Text;

	public Artifact Artifact;

	private void Update()
	{
		if (!(Artifact == null))
		{
			if (Artifact.IsUnlocked)
			{
				Text.text = Artifact.DisplayName + " 等级 " + Artifact.Level + " $" + Artifact.CurrentValue;
			}
			else
			{
				Text.text = "未发现";
			}
		}
	}
}
