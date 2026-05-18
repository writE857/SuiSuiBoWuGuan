using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ArtifactOverlayDisplay : Singleton<ArtifactOverlayDisplay>
{
	public ArtifactDisplay ArtifactDisplay;

	public TMP_Text NameText;

	public TMP_Text GroupText;

	public TMP_Text DescriptionText;

	public TMP_Text LevelText;

	public TMP_Text CountFoundText;

	public TMP_Text XPText;

	public TMP_Text IncomeText;

	public Image XPFill;

	public GameObject Target;

	public Transform Parent;

	public Transform RotateTarget;

	public float lookSensitivity = 1f;

	public float moveSensitivity = 1f;

	private Vector3 currentRotation;

	public bool IsOpen;

	public InputActionReference LookAction;

	public InputActionReference MoveAction;

	private List<ArtifactDisplay> artifacts = new List<ArtifactDisplay>();

	private void Start()
	{
		Target.SetActiveSmart(newState: false);
		IsOpen = false;
		Parent.GetComponentsInChildren(includeInactive: true, artifacts);
	}

	public void Show(ArtifactDisplay currentHover)
	{
		if (currentHover == null || currentHover.Artifact == null)
		{
			IsOpen = false;
			Target.SetActiveSmart(newState: false);
			return;
		}
		Parent.GetComponentsInChildren(includeInactive: true, artifacts);
		artifacts.ForEach(delegate(ArtifactDisplay a)
		{
			a.gameObject.SetActiveSmart(newState: false);
		});
		ArtifactDisplay artifactDisplay = artifacts.FirstOrDefault((ArtifactDisplay a) => a.Artifact == currentHover.Artifact);
		if (artifactDisplay == null)
		{
			IsOpen = false;
			Target.SetActiveSmart(newState: false);
			return;
		}
		artifactDisplay.gameObject.SetActiveSmart(newState: true);
		RotateTarget.localRotation = Quaternion.identity;
		ArtifactDisplay = currentHover;
		NameText.text = ArtifactDisplay.Artifact.DisplayName;
		GroupText.text = ArtifactDisplay.Artifact.ArtifactGroup.DisplayName;
		DescriptionText.text = ArtifactDisplay.Artifact.Description;
		if (ArtifactDisplay.Artifact.IsMaxLevel)
		{
			LevelText.text = "等级 满级（10）";
			XPText.text = "满级";
		}
		else
		{
			LevelText.text = $"等级 {ArtifactDisplay.Artifact.Level}";
			XPText.text = $"{ArtifactDisplay.Artifact.CurrentLevelXP}/{ArtifactDisplay.Artifact.CurrentXPRequired} 碎片";
		}
		CountFoundText.text = $"累计发现 {ArtifactDisplay.Artifact.ItemFoundCount}";
		XPFill.fillAmount = ArtifactDisplay.Artifact.XPRatio;
		IncomeText.text = $"价值：${ArtifactDisplay.Artifact.CurrentValue}";
		IsOpen = true;
		Target.SetActiveSmart(newState: true);
	}

	public void Hide()
	{
		IsOpen = false;
		Target.SetActiveSmart(newState: false);
		artifacts.ForEach(delegate(ArtifactDisplay a)
		{
			a.gameObject.SetActiveSmart(newState: false);
		});
	}

	private void Update()
	{
		Vector2 vector = LookAction.action.ReadValue<Vector2>() * lookSensitivity;
		Vector2 vector2 = MoveAction.action.ReadValue<Vector2>() * moveSensitivity;
		vector += vector2;
		if (Mouse.current != null && Mouse.current.leftButton.isPressed)
		{
			Vector2 vector3 = Mouse.current.delta.ReadValue() * lookSensitivity;
			vector += vector3;
		}
		if (vector2.magnitude > 0f || (Mouse.current != null && Mouse.current.leftButton.isPressed))
		{
			currentRotation += new Vector3(vector.y, vector.x);
			RotateTarget.Rotate(Camera.main.transform.up, 0f - vector.x, Space.World);
			RotateTarget.Rotate(Camera.main.transform.right, vector.y, Space.World);
		}
	}
}
