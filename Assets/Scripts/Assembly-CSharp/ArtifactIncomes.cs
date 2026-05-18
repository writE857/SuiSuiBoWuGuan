using System.Collections.Generic;
using UnityEngine;

public class ArtifactIncomes : MonoBehaviour
{
	public List<ArtifactIncomeText> Texts = new List<ArtifactIncomeText>();

	public RectTransform Parent;

	public ArtifactIncomeText Prefab;

	private void OnEnable()
	{
		Parent.Clear();
	}

	private void Update()
	{
		RefreshUI();
	}

	private void RefreshUI()
	{
		foreach (Artifact item in Singleton<GameResources>.Current.Artifacts.Entries)
		{
			if (!Texts.Exists((ArtifactIncomeText a) => a.Artifact == item) && item.IsUnlocked)
			{
				ArtifactIncomeText artifactIncomeText = Object.Instantiate(Prefab, Parent);
				artifactIncomeText.transform.Reset(scale: true);
				artifactIncomeText.Artifact = item;
				Texts.Add(artifactIncomeText);
			}
		}
	}
}
