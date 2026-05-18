using System;
using System.Collections.Generic;

[Serializable]
public class BrickSaveData
{
	public string GroupID = "";

	public List<int> IndicesAlive = new List<int>();

	public int seed;
}
