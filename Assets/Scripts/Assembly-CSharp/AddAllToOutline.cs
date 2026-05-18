using EPOOutline;
using UnityEngine;

public class AddAllToOutline : MonoBehaviour
{
	private Outlinable Outlinable;

	public void DoIt()
	{
		Outlinable = GetComponent<Outlinable>();
		Outlinable.AddAllChildRenderersToRenderingList();
	}
}
