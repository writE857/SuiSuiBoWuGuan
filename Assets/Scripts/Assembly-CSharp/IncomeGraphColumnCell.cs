using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class IncomeGraphColumnCell : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	public IncomeBarData BarData;

	public RoomIncomeData RoomIncomeData;

	public LayoutElement LayoutElement;

	public string id;

	public float lerpSpeed = 20f;

	public Image Image;

	private float flexValue;

	public AudioResource hoverSFX;

	private Color lastColor = Color.black;

	public IncomeGraphUI IncomeGraphUI;

	public void SetColor(Color currentColor)
	{
		if (!(lastColor == currentColor))
		{
			lastColor = currentColor;
			Image.color = currentColor;
		}
	}

	private void Update()
	{
		if (RoomIncomeData != null)
		{
			flexValue = Mathf.Lerp(flexValue, RoomIncomeData.Income, Time.deltaTime * lerpSpeed);
			LayoutElement.flexibleHeight = flexValue;
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		Singleton<HoverInfo>.Current.CurrentHoveredArtifactGroup = RoomIncomeData.TryGetArtifactGroup();
		Singleton<HoverInfo>.Current.HoveredCell = this;
		Singleton<AudioPool>.Current.Play(hoverSFX, base.transform.position);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		Singleton<HoverInfo>.Current.CurrentHoveredArtifactGroup = null;
		Singleton<HoverInfo>.Current.HoveredCell = null;
	}
}
