using UnityEngine;
using UnityEngine.Events;

public class EventTriggerRelay : MonoBehaviour
{
	public UnityAction onPointerClick;

	public void OnPointerClick()
	{
		onPointerClick?.Invoke();
	}
}
