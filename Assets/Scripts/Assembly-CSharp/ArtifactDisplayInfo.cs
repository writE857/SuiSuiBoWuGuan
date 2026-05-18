using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ArtifactDisplayInfo : Singleton<ArtifactDisplayInfo>
{
	public LayerMask LayerMask;

	public RectTransform Display;

	public Image Icon;

	public TMP_Text NameText;

	public TMP_Text GroupText;

	public TMP_Text DescriptionText;

	public TMP_Text LevelText;

	public TMP_Text IncomeText;

	public float ChangeDuration = 0.2f;

	public InputActionReference ClickAction;

	public Light Light;

	public bool lightOn;

	private void Start()
	{
		Light.enabled = false;
		lightOn = false;
	}

	private void Update()
	{
		Artifact currentHoveredArtifact = Singleton<HoverInfo>.Current.CurrentHoveredArtifact;
		ArtifactDisplay artifactDisplay = null;
		if (currentHoveredArtifact != null)
		{
			artifactDisplay = ArtifactDisplay.Entries.FirstOrDefault((ArtifactDisplay a) => a.Artifact == currentHoveredArtifact);
		}
		if (artifactDisplay != null && ClickAction.action.WasPerformedThisFrame())
		{
			Singleton<ArtifactOverlayDisplay>.Current.Show(artifactDisplay);
		}
		if (artifactDisplay != null)
		{
			Light.enabled = true;
			Light.transform.LookAt(artifactDisplay.transform);
		}
		else
		{
			Light.enabled = false;
		}
	}
}
