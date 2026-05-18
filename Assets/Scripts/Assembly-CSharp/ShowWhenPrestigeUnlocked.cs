using UnityEngine;

public class ShowWhenPrestigeUnlocked : MonoBehaviour
{
	public PrestigeSkill PrestigeSkill;

	public GameObject Target;

	private void Update()
	{
		if (!(Target == null) && !(PrestigeSkill == null))
		{
			Target.SetActiveSmart(PrestigeSkill.IsUnlocked);
		}
	}
}
