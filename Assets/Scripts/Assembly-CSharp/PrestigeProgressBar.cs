using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PrestigeProgressBar : MonoBehaviour
{
	public Image fillImage;

	public TMP_Text Text;

	public Transform NodeParent;

	public List<PrestigeNode> nodes = new List<PrestigeNode>();

	public int NodeMaxLevelSum;

	public int NodeCurrentLevelSum;

	private void OnEnable()
	{
		NodeParent.GetComponentsInChildren(nodes);
		NodeMaxLevelSum = nodes.Sum((PrestigeNode a) => a.PrestigeSkill.MaxLevel);
		StartCoroutine(Do_Update());
	}

	private IEnumerator Do_Update()
	{
		while (true)
		{
			yield return new WaitForSeconds(0.1f);
			NodeCurrentLevelSum = nodes.Sum((PrestigeNode a) => a.PrestigeSkill.CurrentLevel);
			fillImage.fillAmount = (float)NodeCurrentLevelSum / (float)NodeMaxLevelSum;
			if (NodeCurrentLevelSum == NodeMaxLevelSum)
			{
				Text.text = "满级";
			}
			else
			{
				Text.text = $"{NodeCurrentLevelSum}/{NodeMaxLevelSum}";
			}
		}
	}
}
