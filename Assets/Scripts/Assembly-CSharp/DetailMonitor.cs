using System;
using UnityEngine.Events;

public class DetailMonitor : Singleton<DetailMonitor>
{
	public ArtifactGroupDetailPanel ArtifactGroupDetailPanel;

	public ArtifactDetailPanel ArtifactDetailPanel;

	private void Start()
	{
		GameEvents current = Singleton<GameEvents>.Current;
		current.OnRestart = (UnityAction)Delegate.Combine(current.OnRestart, new UnityAction(OnRestart));
		GameEvents current2 = Singleton<GameEvents>.Current;
		current2.OnPrestigeChange = (UnityAction)Delegate.Combine(current2.OnPrestigeChange, new UnityAction(OnRestart));
		OnRestart();
	}

	private void OnRestart()
	{
		ArtifactDetailPanel.gameObject.SetActiveSmart(newState: false);
		ArtifactGroupDetailPanel.gameObject.SetActiveSmart(newState: true);
	}

	private void Update()
	{
		if (Singleton<GameSession>.Current.IsInMenu)
		{
			ArtifactDetailPanel.gameObject.SetActiveSmart(newState: false);
			ArtifactGroupDetailPanel.gameObject.SetActiveSmart(newState: false);
		}
		else if (Singleton<HoverInfo>.Current.CurrentHoveredArtifact != null)
		{
			ArtifactDetailPanel.gameObject.SetActiveSmart(newState: true);
			ArtifactGroupDetailPanel.gameObject.SetActiveSmart(newState: false);
		}
		else
		{
			ArtifactDetailPanel.gameObject.SetActiveSmart(newState: false);
			ArtifactGroupDetailPanel.gameObject.SetActiveSmart(newState: true);
		}
	}
}
