using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class HoverInfo : Singleton<HoverInfo>
{
	private const int MaxRaycastHits = 64;

	private static readonly RaycastHit[] RaycastHits = new RaycastHit[MaxRaycastHits];

	private ArtifactGroup currentBrickArtifactGroup;

	private ArtifactGroup currentHoveredArtifactGroup;

	private Artifact currentHoveredArtifact;

	private IncomeGraphColumnCell hoveredCell;

	public event UnityAction HoverStateChanged;

	public ArtifactGroup CurrentBrickArtifactGroup
	{
		get
		{
			return currentBrickArtifactGroup;
		}
		set
		{
			SetHoverValue(ref currentBrickArtifactGroup, value);
		}
	}

	public ArtifactGroup CurrentHoveredArtifactGroup
	{
		get
		{
			return currentHoveredArtifactGroup;
		}
		set
		{
			SetHoverValue(ref currentHoveredArtifactGroup, value);
		}
	}

	public Artifact CurrentHoveredArtifact
	{
		get
		{
			return currentHoveredArtifact;
		}
		set
		{
			SetHoverValue(ref currentHoveredArtifact, value);
		}
	}

	public IncomeGraphColumnCell HoveredCell
	{
		get
		{
			return hoveredCell;
		}
		set
		{
			SetHoverValue(ref hoveredCell, value);
		}
	}

	public ArtifactGroup ArtifactGroup
	{
		get
		{
			if (HoveredCell != null)
			{
				return HoveredCell.RoomIncomeData.TryGetArtifactGroup();
			}
			if (CurrentHoveredArtifact != null)
			{
				return CurrentHoveredArtifact.ArtifactGroup;
			}
			if (CurrentHoveredArtifactGroup != null)
			{
				return CurrentHoveredArtifactGroup;
			}
			return null;
		}
	}

	private void OnEnable()
	{
		GameEvents current = Singleton<GameEvents>.Current;
		current.OnRestart += OnRestart;
		GameEvents current2 = Singleton<GameEvents>.Current;
		current2.OnPrestigeChange += OnRestart;
	}

	private void OnDisable()
	{
		if (_current == null)
		{
			return;
		}
		GameEvents current = Singleton<GameEvents>.Current;
		current.OnRestart -= OnRestart;
		GameEvents current2 = Singleton<GameEvents>.Current;
		current2.OnPrestigeChange -= OnRestart;
	}

	private void OnRestart()
	{
		bool changed = currentBrickArtifactGroup != null || currentHoveredArtifactGroup != null || currentHoveredArtifact != null || hoveredCell != null;
		currentBrickArtifactGroup = null;
		currentHoveredArtifactGroup = null;
		currentHoveredArtifact = null;
		hoveredCell = null;
		if (changed)
		{
			HoverStateChanged?.Invoke();
		}
	}

	private void Update()
	{
		CurrentBrickArtifactGroup = ((Singleton<BrickTable>.Current.CurrentBrick == null) ? null : Singleton<BrickTable>.Current.CurrentBrick.LootGenerator.ArtifactGroup);
		UpdateHoveredArtifact();
	}

	private void SetHoverValue<T>(ref T field, T value)
	{
		if (EqualityComparer<T>.Default.Equals(field, value))
		{
			return;
		}
		field = value;
		HoverStateChanged?.Invoke();
	}

	private void UpdateHoveredArtifact()
	{
		Artifact hoveredArtifact = ResolveHoveredArtifact();
		CurrentHoveredArtifact = hoveredArtifact;
	}

	private Artifact ResolveHoveredArtifact()
	{
		if (Camera.main == null || !MouseScreenPosition.TryGetInputSystem(out var mousePosition))
		{
			return null;
		}
		Ray ray = Camera.main.ScreenPointToRay(mousePosition);
		int num = Physics.RaycastNonAlloc(ray, RaycastHits, float.MaxValue);
		if (num <= 0)
		{
			return null;
		}
		float num2 = float.MaxValue;
		Artifact result = null;
		for (int i = 0; i < num; i++)
		{
			RaycastHit raycastHit = RaycastHits[i];
			Collider collider = raycastHit.collider;
			if (collider == null)
			{
				continue;
			}
			ArtifactDisplay componentInParent = collider.GetComponentInParent<ArtifactDisplay>();
			if (componentInParent != null && componentInParent.CanHover(collider.gameObject.layer) && raycastHit.distance < num2)
			{
				num2 = raycastHit.distance;
				result = componentInParent.Artifact;
				continue;
			}
			ArtifactRoomDisplay componentInParent2 = collider.GetComponentInParent<ArtifactRoomDisplay>();
			if (componentInParent2 != null && componentInParent2.CanHover(collider.gameObject.layer) && raycastHit.distance < num2)
			{
				num2 = raycastHit.distance;
				result = componentInParent2.Artifact;
			}
		}
		return result;
	}
}
