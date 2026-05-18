using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu]
public class Artifacts : ScriptableObject
{
	public List<Artifact> Entries = new List<Artifact>();

	public List<ArtifactGroup> Groups = new List<ArtifactGroup>();

	public ArtifactGroup GetGroup(string iD)
	{
		if (iD == null)
		{
			return null;
		}
		if (iD == ArtifactGroup.SellGroup.GROUPID)
		{
			return ArtifactGroup.SellGroup;
		}
		if (iD == ArtifactGroup.CoinGroup.GROUPID)
		{
			return ArtifactGroup.CoinGroup;
		}
		if (iD == ArtifactGroup.HammerGroup.GROUPID)
		{
			return ArtifactGroup.HammerGroup;
		}
		return Groups.FirstOrDefault((ArtifactGroup a) => a.GROUPID == iD);
	}

	private void CheckAll()
	{
		foreach (Artifact artifact in Entries)
		{
			if (!Groups.Any((ArtifactGroup a) => a.Artifacts.Contains(artifact)))
			{
				Debug.LogError("Not in groups: " + artifact, artifact);
			}
		}
		foreach (ArtifactGroup group in Groups)
		{
			foreach (Artifact artifact2 in group.Artifacts)
			{
				if (!Entries.Contains(artifact2))
				{
					Debug.LogError("Not in entries: " + group.name + "-" + artifact2.name, artifact2);
				}
			}
		}
	}

	private void AutoPopulateArtifacts()
	{
		Entries.Clear();
		foreach (ArtifactGroup group in Groups)
		{
			Entries.AddRange(group.Artifacts);
		}
	}

	private void OnValidate()
	{
		CheckAll();
	}

	public int GetGroupIndex(string iD)
	{
		ArtifactGroup artifactGroup = GetGroup(iD);
		if (artifactGroup == null)
		{
			return -1;
		}
		return artifactGroup.PriorityIndex;
	}
}
