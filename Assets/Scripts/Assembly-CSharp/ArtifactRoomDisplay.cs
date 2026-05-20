using System.Collections.Generic;
using UnityEngine;

public class ArtifactRoomDisplay : MonoBehaviour
{
	public SpriteRenderer ImageDisplay;

	public GameObject Light;

	public Artifact Artifact;

	private bool hovered;

	public LayerMask LayerMask;

	public ArtifactDisplayProgressUI ArtifactDisplayProgressUI;

	public List<BoxCollider> Colliders = new List<BoxCollider>();

	private Vector3 initialImageScale = Vector3.one;

	private bool cachedInitialImageScale;

	private Color initialImageColor = Color.white;

	private bool cachedInitialImageColor;

	private void Start()
	{
		CacheInitialImageScale();
		CacheInitialImageColor();
		GetComponentsInChildren(includeInactive: true, Colliders);
		if (Artifact == null)
		{
			Debug.LogError("Artifact not assigned!", this);
			base.gameObject.SetActiveSmart(newState: false);
		}
		else
		{
			Evalute();
		}
	}

	private void Evalute()
	{
		if (Artifact == null)
		{
			return;
		}
		bool isAvailable = Artifact.ArtifactGroup != null && Artifact.ArtifactGroup.IsAvailable(Artifact);
		if (Artifact.IsUnlocked || isAvailable)
		{
			Show(Artifact.IsUnlocked);
		}
		else
		{
			Hide();
		}
		if (ArtifactDisplayProgressUI == null || Artifact.ArtifactGroup == null)
		{
			return;
		}
		if (Artifact.IsUnlocked || Artifact.ArtifactGroup.IsAvailable(Artifact))
		{
			ArtifactDisplayProgressUI.gameObject.SetActiveSmart(newState: false);
			return;
		}
		ArtifactDisplayProgressUI.gameObject.SetActiveSmart(newState: true);
		ArtifactDisplayProgressUI.Setup(Artifact);
	}

	private void Hide()
	{
		if (ImageDisplay != null)
		{
			ImageDisplay.enabled = false;
			RestoreImageColor();
		}
		if (Light != null)
		{
			Light.SetActiveSmart(newState: false);
		}
	}

	private void Show(bool isUnlocked)
	{
		if (ImageDisplay == null)
		{
			return;
		}
		CacheInitialImageColor();
		ImageDisplay.sprite = Artifact.MuseumImage;
		if (ImageDisplay.sprite == null)
		{
			ImageDisplay.enabled = false;
			if (Light != null)
			{
				Light.SetActiveSmart(newState: false);
			}
			return;
		}
		ImageDisplay.enabled = true;
		ImageDisplay.color = isUnlocked ? initialImageColor : new Color(initialImageColor.r, initialImageColor.g, initialImageColor.b, initialImageColor.a * 0.45f);
		if (Light != null)
		{
			Light.SetActiveSmart(isUnlocked);
		}
		RestoreImageScale();
	}

	private void CacheInitialImageScale()
	{
		if (ImageDisplay == null || cachedInitialImageScale)
		{
			return;
		}
		initialImageScale = ImageDisplay.transform.localScale;
		cachedInitialImageScale = true;
	}

	private void CacheInitialImageColor()
	{
		if (ImageDisplay == null || cachedInitialImageColor)
		{
			return;
		}
		initialImageColor = ImageDisplay.color;
		cachedInitialImageColor = true;
	}

	private void RestoreImageColor()
	{
		if (ImageDisplay == null || !cachedInitialImageColor)
		{
			return;
		}
		ImageDisplay.color = initialImageColor;
	}

	private void RestoreImageScale()
	{
		CacheInitialImageScale();
		ImageDisplay.transform.localScale = initialImageScale;
	}

	public void OnPointerEnter()
	{
		if (!CanHoverOwnArtifact())
		{
			return;
		}
		Singleton<HoverInfo>.Current.CurrentHoveredArtifact = Artifact;
	}

	public void OnPointerExit()
	{
		if (Singleton<HoverInfo>.Current == null)
		{
			return;
		}
		if (!(Singleton<HoverInfo>.Current.CurrentHoveredArtifact != Artifact))
		{
			Singleton<HoverInfo>.Current.CurrentHoveredArtifact = null;
		}
	}

	private void Update()
	{
		if (Artifact == null)
		{
			return;
		}
		bool flag = Artifact.ArtifactGroup != null && Singleton<RoomContent>.Current != null && RoomContent.SameGroup(Singleton<RoomContent>.Current.CurrentlyShownGroup, Artifact.ArtifactGroup);
		foreach (BoxCollider collider in Colliders)
		{
			if (collider.enabled != flag)
			{
				collider.enabled = flag;
			}
		}
		bool flag2 = Singleton<HoverInfo>.Current.CurrentHoveredArtifact == Artifact && CanHoverOwnArtifact();
		if (hovered != flag2)
		{
			hovered = flag2;
			if (!hovered)
			{
				OnPointerExit();
			}
		}
		Evalute();
	}

	public bool CanHover(int layer)
	{
		if (!CanHoverOwnArtifact())
		{
			return false;
		}
		return (LayerMask.value & (1 << layer)) != 0;
	}

	private bool CanHoverOwnArtifact()
	{
		return Artifact != null && Artifact.IsUnlocked && Artifact.ArtifactGroup != null && Singleton<RoomContent>.Current != null && RoomContent.SameGroup(Singleton<RoomContent>.Current.CurrentlyShownGroup, Artifact.ArtifactGroup);
	}
}
