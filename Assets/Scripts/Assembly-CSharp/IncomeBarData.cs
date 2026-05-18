using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class IncomeBarData
{
	public int startTime;

	public List<RoomIncomeData> RoomIncomeDatas = new List<RoomIncomeData>();

	public int SumAmount => RoomIncomeDatas.Sum((RoomIncomeData a) => a.Income);

	public void AddIncome(string id, int amount)
	{
		RoomIncomeData roomIncomeData = RoomIncomeDatas.FirstOrDefault((RoomIncomeData a) => a.ID == id);
		if (roomIncomeData == null)
		{
			roomIncomeData = new RoomIncomeData
			{
				ID = id
			};
			RoomIncomeDatas.Add(roomIncomeData);
		}
		roomIncomeData.Income += amount;
	}
}
