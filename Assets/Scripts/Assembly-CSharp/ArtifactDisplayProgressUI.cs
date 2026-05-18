using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ArtifactDisplayProgressUI : MonoBehaviour
{
	public Artifact Artifact;

	public TMP_Text Text;

	public Image fillImage;

	public void Setup(Artifact artifact)
	{
		Artifact = artifact;
		if (!(Text == null) && !(Artifact == null) && !(Artifact.ArtifactGroup == null))
		{
			Text.text = $"需要\n等级 {Artifact.ArtifactGroup.UnlockedAt(Artifact)}";
		}
	}

	private void Update()
	{
		if (!(Artifact == null) && !(Artifact.ArtifactGroup == null) && !(fillImage == null))
		{
			float num = Artifact.ArtifactGroup.ItemFoundCount;
			int artifactsRequired = Artifact.ArtifactGroup.GetArtifactsRequired(Artifact);
			fillImage.fillAmount = artifactsRequired > 0 ? num / (float)artifactsRequired : 0f;
		}
	}
}
