using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using EPOOutline;
using UnityEngine;
using UnityEngine.Events;

public class ArtifactDisplay : MonoBehaviour
{
	public Artifact Artifact;

	private Vector3 originalPosition;

	private Vector3 originalScale;

	private MeshRenderer MeshRenderer;

	[Header("Drop Settings")]
	public float dropHeight = 2f;

	public float dropDuration = 0.25f;

	public float impactScale = 0.85f;

	public float settleDuration = 0.15f;

	[ColorUsage(true, true)]
	public Color outlineColor;

	public Outlinable Outlinable;

	public MeshCollider MeshCollider;

	public bool IsHighlighted;

	public LayerMask LayerMask;

	private bool hovered;

	internal static List<ArtifactDisplay> Entries = new List<ArtifactDisplay>();

	private void Start()
	{
		Entries.Add(this);
		MeshRenderer = GetComponent<MeshRenderer>();
		Outlinable = GetComponent<Outlinable>();
		MeshCollider = GetComponent<MeshCollider>();
		originalPosition = base.transform.position;
		originalScale = base.transform.localScale;
		if (Artifact != null && Artifact.IsUnlocked)
		{
			SilentSpawn();
			return;
		}
		Hide();
	}

	private void OnEnable()
	{
		GameEvents current = Singleton<GameEvents>.Current;
		current.OnArtifactUnlocked += SpawnUnlocked;
		current.OnRestart += RefreshVisualState;
		current.OnPrestigeChange += RefreshVisualState;
		Singleton<HoverInfo>.Current.HoverStateChanged += RefreshVisualState;
	}

	private void OnDisable()
	{
		GameEvents gameEvents = UnityEngine.Object.FindFirstObjectByType<GameEvents>(FindObjectsInactive.Include);
		if (gameEvents != null)
		{
			gameEvents.OnArtifactUnlocked -= SpawnUnlocked;
			gameEvents.OnRestart -= RefreshVisualState;
			gameEvents.OnPrestigeChange -= RefreshVisualState;
		}
		HoverInfo hoverInfo = UnityEngine.Object.FindFirstObjectByType<HoverInfo>(FindObjectsInactive.Include);
		if (hoverInfo != null)
		{
			hoverInfo.HoverStateChanged -= RefreshVisualState;
		}
	}

	private void OnDestroy()
	{
		Entries.Remove(this);
	}

	private void Hide()
	{
		MeshRenderer.enabled = false;
		Outlinable.enabled = false;
		MeshCollider.enabled = false;
	}

	private void SilentSpawn()
	{
		MeshRenderer.enabled = true;
		Outlinable.enabled = false;
		MeshCollider.enabled = true;
	}

	private void SpawnUnlocked(Artifact artifact)
	{
		if (!(Artifact == null) && !(artifact.ID != Artifact.ID))
		{
			Sequence sequence = DOTween.Sequence();
			base.transform.position = originalPosition + Vector3.up * dropHeight;
			sequence.Append(base.transform.DOMoveY(originalPosition.y, dropDuration).SetEase(Ease.InQuad)).Append(base.transform.DOScale(originalScale * impactScale, 0.08f).SetEase(Ease.OutQuad)).Append(base.transform.DOScale(originalScale, settleDuration).SetEase(Ease.OutBack, 3f))
				.Join(base.transform.DOMoveY(originalPosition.y + 0.02f, 0.06f).SetEase(Ease.OutQuad))
				.OnComplete(delegate
				{
					base.transform.position = originalPosition;
				});
			sequence.Play();
			MeshRenderer.enabled = true;
			MeshCollider.enabled = true;
			RefreshOutlineState();
		}
	}

	private void RefreshVisualState()
	{
		if (Artifact.IsUnlocked != MeshRenderer.enabled)
		{
			if (Artifact.IsUnlocked)
			{
				SpawnUnlocked(Artifact);
			}
			else
			{
				Hide();
			}
		}
		RefreshOutlineState();
	}

	private void RefreshOutlineState()
	{
		bool flag = Singleton<HoverInfo>.Current.CurrentHoveredArtifact == Artifact && Artifact != null && Artifact.IsUnlocked;
		if (Outlinable.enabled != flag)
		{
			Outlinable.enabled = flag;
		}
		hovered = flag;
	}

	public void AutoAssignArtifact()
	{
		Outlinable = GetComponent<Outlinable>();
		Outlinable.OutlineParameters.Color = outlineColor;
		List<Artifact> entries = Singleton<GameResources>.Current.Artifacts.Entries;
		Mesh mesh = GetComponentInChildren<MeshFilter>(includeInactive: true).sharedMesh;
		Artifact artifact = entries.FirstOrDefault((Artifact a) => a.GetComponentInChildren<MeshFilter>(includeInactive: true).sharedMesh == mesh);
		if (artifact != null)
		{
			Artifact = artifact;
		}
		else
		{
			Debug.LogError("Could not find matching artifact", this);
		}
		MeshCollider = GetComponent<MeshCollider>();
		if (MeshCollider == null)
		{
			MeshCollider = base.gameObject.AddComponent<MeshCollider>();
		}
	}

	public bool CanHover(int layer)
	{
		if (!enabled || Artifact == null || !Artifact.IsUnlocked || MeshCollider == null || !MeshCollider.enabled)
		{
			return false;
		}
		return (LayerMask.value & (1 << layer)) != 0;
	}
}
