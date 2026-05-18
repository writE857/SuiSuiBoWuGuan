using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;

public class NotificationManager : MonoBehaviour
{
	public List<Notification> Notifications = new List<Notification>();

	public RectTransform Parent;

	public Notification Prefab;

	public int destroyAfter = 10;

	public int alphaMaxCount = 5;

	public AnimationCurve AlphaCurve;

	public AudioResource AudioResource;

	private void Start()
	{
		GameEvents current = Singleton<GameEvents>.Current;
		current.OnArtifactLevelUp = (UnityAction<Artifact>)Delegate.Combine(current.OnArtifactLevelUp, new UnityAction<Artifact>(OnArtifactLevelUp));
		GameEvents current2 = Singleton<GameEvents>.Current;
		current2.OnArtifactGroupLevelUp = (UnityAction<ArtifactGroup>)Delegate.Combine(current2.OnArtifactGroupLevelUp, new UnityAction<ArtifactGroup>(OnArtifactGroupLevelUp));
		GameEvents current3 = Singleton<GameEvents>.Current;
		current3.OnLootExtracted = (UnityAction<Loot>)Delegate.Combine(current3.OnLootExtracted, new UnityAction<Loot>(OnLootExtracted));
		GameEvents current4 = Singleton<GameEvents>.Current;
		current4.OnLootSold = (UnityAction<Loot>)Delegate.Combine(current4.OnLootSold, new UnityAction<Loot>(OnLootSold));
		GameEvents current5 = Singleton<GameEvents>.Current;
		current5.OnUpgradeBought = (UnityAction<string, int>)Delegate.Combine(current5.OnUpgradeBought, new UnityAction<string, int>(OnUpgradeBought));
		Parent.Clear();
	}

	private void OnArtifactLevelUp(Artifact artifact)
	{
		string text = $"{artifact.DisplayName} 升到 {artifact.Level} 级。";
		AddNotification(text);
	}

	private void OnArtifactGroupLevelUp(ArtifactGroup artifactGroup)
	{
		string text = $"{artifactGroup.DisplayName} 升到 {artifactGroup.Level} 级。";
		AddNotification(text);
	}

	private void OnLootSold(Loot loot)
	{
		string text = $"{loot.DisplayName} 已售出，获得 ${loot.FinalValue}。";
		AddNotification(text);
	}

	private void OnLootExtracted(Loot loot)
	{
		string text = "发现 " + loot.DisplayName + "。";
		AddNotification(text);
	}

	private void OnUpgradeBought(string upgradeName, int newLevel)
	{
		string text = $"{upgradeName} 升到 {newLevel} 级。";
		AddNotification(text);
	}

	private void AddNotification(string text)
	{
		Notification notification = UnityEngine.Object.Instantiate(Prefab, Parent);
		notification.DisplayText = text;
		notification.Init();
		notification.transform.SetAsFirstSibling();
		Notifications.Insert(0, notification);
		Singleton<AudioPool>.Current.Play(AudioResource, notification.transform.position);
	}

	private void Update()
	{
		while (Notifications.Count > destroyAfter)
		{
			Notification notification = Notifications.Last();
			UnityEngine.Object.Destroy(notification.gameObject);
			Notifications.Remove(notification);
		}
		for (int i = 0; i < Notifications.Count; i++)
		{
			float num = (float)i / (float)alphaMaxCount;
			num = 1f - num;
			float alpha = AlphaCurve.Evaluate(num);
			Notifications[i].SetAlpha(alpha);
		}
	}
}
