using System.Collections.Generic;
using UnityEngine;

public class HammerTierModel : MonoBehaviour
{
	public List<GameObject> Models = new List<GameObject>();

	public List<PrestigeSkill> HammerTierSkills = new List<PrestigeSkill>();

	private void Update()
	{
		if (HammerTierSkills.Count + 1 != Models.Count)
		{
			Debug.LogError("Hammer count missmatch");
		}
		int num = 0;
		for (int i = 0; i < HammerTierSkills.Count; i++)
		{
			if (HammerTierSkills[i].IsUnlocked)
			{
				num = i + 1;
			}
		}
		for (int j = 0; j < Models.Count; j++)
		{
			Models[j].SetActiveSmart(j == num);
		}
	}
}
