using DG.Tweening;
using TMPro;
using UnityEngine;

public class VisitorNote : MonoBehaviour
{
	public TMP_Text Text;

	public float lifeTime = 3f;

	public float animDuration = 0.25f;

	public Visitor Visitor;

	public void Setup(Visitor visitor)
	{
		Visitor = visitor;
		Text = GetComponentInChildren<TMP_Text>();
		Text.text = "";
		string random = Singleton<VisitorNoteManager>.Current.Lines.GetRandom();
		Text.DOText(random, animDuration).SetEase(Ease.Linear);
		Object.Destroy(base.gameObject, lifeTime);
	}

	private void Update()
	{
		if (Visitor != null)
		{
			base.transform.position = Visitor.transform.position;
		}
	}
}
