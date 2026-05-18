using System;
using UnityEngine;

[Serializable]
public class RoomIncomeData
{
	public string ID;

	public int Income;

	private int _groupIndex = -1;

	public int GroupIndex
	{
		get
		{
			if (_groupIndex != -1)
			{
				return _groupIndex;
			}
			return _groupIndex = ((!Application.isPlaying) ? (-1) : Singleton<GameResources>.Current.Artifacts.GetGroupIndex(ID));
		}
	}

	public ArtifactGroup TryGetArtifactGroup()
	{
		return Singleton<GameResources>.Current.Artifacts.GetGroup(ID);
	}
}
