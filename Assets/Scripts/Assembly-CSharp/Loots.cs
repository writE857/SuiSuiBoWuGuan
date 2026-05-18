using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class Loots : ScriptableObject
{
	public List<Loot> Entries = new List<Loot>();

	public Loot CoinLoot;

	public Loot Ticket;
}
