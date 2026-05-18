using TMPro;
using UnityEngine;

public class CollectionValueText : MonoBehaviour
{
	private TMP_Text Text;

	private int lastCollectionValue = int.MinValue;

	private void OnEnable()
	{
		Text = GetComponent<TMP_Text>();
		lastCollectionValue = int.MinValue;
		RefreshText();
	}

	private void Update()
	{
		RefreshText();
	}

	private void RefreshText()
	{
		int collectionValue = Singleton<VisitorManager>.Current.CollectionValue;
		if (collectionValue == lastCollectionValue)
		{
			return;
		}
		lastCollectionValue = collectionValue;
		Text.text = "藏品价值：$" + collectionValue.ToString("N0");
	}
}
