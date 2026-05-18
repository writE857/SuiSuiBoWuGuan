using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu]
public class TestPrestigeSkills : ScriptableObject
{
	public List<PrestigeSkill> Entries = new List<PrestigeSkill>();

	public int sumTickets => Entries.Sum((PrestigeSkill a) => a.DebugSumPrice);
}
