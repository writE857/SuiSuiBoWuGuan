using DG.Tweening;
using TMPro;

public static class TMPTextTweenExtensions
{
	public static Tweener DOText(this TMP_Text target, string endValue, float duration, bool richTextEnabled = true, ScrambleMode scrambleMode = ScrambleMode.None, string scrambleChars = null)
	{
		return DOTween.To(() => target.text, x => target.text = x, endValue, duration).SetOptions(richTextEnabled, scrambleMode, scrambleChars).SetTarget(target);
	}
}
