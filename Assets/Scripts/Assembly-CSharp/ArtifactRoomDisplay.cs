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

	private const float MaxDisplayWidth = 0.28f;

	private const float MaxDisplayHeight = 0.28f;

	private Vector3 initialImageScale = Vector3.one;

	private bool cachedInitialImageScale;

	private void Start()
	{
		CacheInitialImageScale();
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
		if (Artifact.IsUnlocked)
		{
			Show();
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
		}
		if (Light != null)
		{
			Light.SetActiveSmart(newState: false);
		}
	}

	private void Show()
	{
		if (ImageDisplay == null)
		{
			return;
		}
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
		if (Light != null)
		{
			Light.SetActiveSmart(newState: true);
		}
		FitImageToScreenSlot();
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

	private void FitImageToScreenSlot()
	{
		CacheInitialImageScale();
		if (ImageDisplay.sprite == null)
		{
			return;
		}
		ImageDisplay.transform.localScale = initialImageScale;
		Vector3 size = ImageDisplay.sprite.bounds.size;
		if (size.x <= 0f || size.y <= 0f)
		{
			Rect rect = ImageDisplay.sprite.rect;
			float pixelsPerUnit = ImageDisplay.sprite.pixelsPerUnit;
			if (pixelsPerUnit > 0f)
			{
				size = new Vector3(rect.width / pixelsPerUnit, rect.height / pixelsPerUnit, 0f);
			}
		}
		if (size.x <= 0f || size.y <= 0f)
		{
			return;
		}
		float scale = Mathf.Min(MaxDisplayWidth / size.x, MaxDisplayHeight / size.y);
		if (float.IsNaN(scale) || float.IsInfinity(scale) || scale >= 1f)
		{
			return;
		}
		ImageDisplay.transform.localScale = initialImageScale * scale;
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
