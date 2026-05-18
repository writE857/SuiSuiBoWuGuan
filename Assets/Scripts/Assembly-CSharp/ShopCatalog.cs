using EPOOutline;
using UnityEngine;
using UnityEngine.EventSystems;

public class ShopCatalog : MonoBehaviour
{
	public Outlinable Outlinable;

	public Animator Animator;

	public GameObject UIObject;

	private void OnEnable()
	{
		Outlinable.AddAllChildRenderersToRenderingList();
		UIObject.SetActive(value: true);
		Animator.SetBool("IsOpen", value: false);
	}

	public void OnHoverStart()
	{
		Outlinable.enabled = true;
	}

	public void OnHoverEnd()
	{
		Outlinable.enabled = false;
	}

	public void Open(BaseEventData data)
	{
		if (((PointerEventData)data).button == PointerEventData.InputButton.Left)
		{
			Animator.SetBool("IsOpen", value: true);
		}
	}

	public void Close()
	{
		Animator.SetBool("IsOpen", value: false);
	}
}
