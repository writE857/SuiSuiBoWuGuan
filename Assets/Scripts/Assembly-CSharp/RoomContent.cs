using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class RoomContent : Singleton<RoomContent>
{
	private const string RoomRootName = "Rooms";

	private const string RoomBackgroundName = "Background";

	private const string RoomSpriteSortingLayer = "Sprites";

	private const string RoomBackgroundSortingLayer = "Sprite BG";

	private const int RoomBackgroundSortingOrder = -32768;

	private const int RoomMaskFrontSortingOrder = 32767;

	private const int DefaultSortingLayerID = 0;

	public TMP_Text Text;

	public float TitleDuration = 0.2f;

	private string lastText;

	public float RoomLength = 0.3f;

	public List<RoomContainer> Rooms = new List<RoomContainer>();

	public Transform RoomContainerParent;

	private ArtifactGroup currentGroup;

	public float durationOver = 0.1f;

	public float durationFinal = 0.1f;

	public float overshoot = 0.05f;

	public TMP_Text CameraText;

	public ArtifactGroup CurrentlyShownGroup;

	private Dictionary<TMP_Text, string> textValues = new Dictionary<TMP_Text, string>();

	private int lastIndex = -1;

	private void Start()
	{
		Rooms.Clear();
		if (RoomContainerParent != null)
		{
			RoomContainerParent.GetComponentsInChildren(includeInactive: true, Rooms);
			EnsureRoomRenderOrder();
		}
	}

	private void EnsureRoomRenderOrder()
	{
		Transform roomRoot = FindRoomRoot();
		if (roomRoot == null)
		{
			return;
		}
		EnsureRoomMaskRange(roomRoot);
		foreach (SpriteRenderer spriteRenderer in roomRoot.GetComponentsInChildren<SpriteRenderer>(includeInactive: true))
		{
			if (spriteRenderer == null)
			{
				continue;
			}
			if (spriteRenderer.gameObject.name == RoomBackgroundName)
			{
				TrySetSortingLayer(spriteRenderer, RoomBackgroundSortingLayer);
				spriteRenderer.sortingOrder = RoomBackgroundSortingOrder;
			}
			else
			{
				TrySetSortingLayer(spriteRenderer, RoomSpriteSortingLayer);
			}
			spriteRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
		}
	}

	private void EnsureRoomMaskRange(Transform roomRoot)
	{
		foreach (SpriteMask spriteMask in roomRoot.GetComponentsInChildren<SpriteMask>(includeInactive: true))
		{
			if (spriteMask == null)
			{
				continue;
			}
			spriteMask.isCustomRangeActive = true;
			spriteMask.backSortingLayerID = GetSortingLayerIDOrDefault(RoomBackgroundSortingLayer);
			spriteMask.frontSortingLayerID = GetSortingLayerIDOrDefault(RoomSpriteSortingLayer);
			spriteMask.backSortingOrder = RoomBackgroundSortingOrder;
			spriteMask.frontSortingOrder = RoomMaskFrontSortingOrder;
		}
	}

	private Transform FindRoomRoot()
	{
		Transform current = RoomContainerParent;
		while (current != null)
		{
			if (current.name == RoomRootName)
			{
				return current;
			}
			current = current.parent;
		}
		return RoomContainerParent;
	}

	private static void TrySetSortingLayer(SpriteRenderer spriteRenderer, string sortingLayerName)
	{
		spriteRenderer.sortingLayerID = GetSortingLayerIDOrDefault(sortingLayerName);
	}

	private static int GetSortingLayerIDOrDefault(string sortingLayerName)
	{
		foreach (SortingLayer sortingLayer in SortingLayer.layers)
		{
			if (sortingLayer.name == sortingLayerName)
			{
				return sortingLayer.id;
			}
		}
		return DefaultSortingLayerID;
	}

	private void Update()
	{
		CurrentlyShownGroup = ResolveShownGroup();
		if (CurrentlyShownGroup != null)
		{
			SetText(Text, CurrentlyShownGroup.DisplayName + " 展厅");
			int num = GetGameResourceGroupIndex(CurrentlyShownGroup);
			SetText(CameraText, $"摄像机 {num + 1}");
		}
		else
		{
			SetText(Text, "粉碎博物馆");
		}
		if (!SameGroup(currentGroup, CurrentlyShownGroup))
		{
			currentGroup = CurrentlyShownGroup;
			GoToRoom(currentGroup);
		}
	}

	private ArtifactGroup ResolveShownGroup()
	{
		HoverInfo hoverInfo = Singleton<HoverInfo>.Current;
		if (hoverInfo != null)
		{
			ArtifactGroup hoverGroup = hoverInfo.ArtifactGroup;
			if (hoverGroup != null)
			{
				return hoverGroup;
			}
			if (hoverInfo.CurrentBrickArtifactGroup != null)
			{
				return hoverInfo.CurrentBrickArtifactGroup;
			}
		}
		BrickTable brickTable = Singleton<BrickTable>.Current;
		if (brickTable != null && brickTable.CurrentBrick != null)
		{
			Brick currentBrick = brickTable.CurrentBrick;
			if (currentBrick.LootGenerator != null && currentBrick.LootGenerator.ArtifactGroup != null)
			{
				return currentBrick.LootGenerator.ArtifactGroup;
			}
			if (currentBrick.Instance != null)
			{
				return currentBrick.Instance.ArtifactGroup;
			}
		}
		for (int i = 0; i < Rooms.Count; i++)
		{
			if (Rooms[i] != null && Rooms[i].ArtifactGroup != null)
			{
				return Rooms[i].ArtifactGroup;
			}
		}
		return null;
	}

	public static bool SameGroup(ArtifactGroup a, ArtifactGroup b)
	{
		if (a == b)
		{
			return true;
		}
		if (a == null || b == null)
		{
			return false;
		}
		return !string.IsNullOrEmpty(a.GROUPID) && a.GROUPID == b.GROUPID;
	}

	private int GetGameResourceGroupIndex(ArtifactGroup group)
	{
		int num = -1;
		if (Singleton<GameResources>.Current != null && Singleton<GameResources>.Current.Artifacts != null)
		{
			num = Singleton<GameResources>.Current.Artifacts.Groups.FindIndex((ArtifactGroup a) => SameGroup(a, group));
		}
		if (num != -1)
		{
			return num;
		}
		num = Rooms.FindIndex((RoomContainer a) => a != null && SameGroup(a.ArtifactGroup, group));
		return Mathf.Max(0, num);
	}

	private void SetText(TMP_Text text, string newText)
	{
		if (text == null)
		{
			return;
		}
		string text2 = "";
		if (textValues.ContainsKey(text))
		{
			text2 = textValues[text];
		}
		if (!(newText == text2))
		{
			textValues[text] = newText;
			text.DOKill();
			text.text = newText;
		}
	}

	private void GoToRoom(ArtifactGroup group)
	{
		if (RoomContainerParent == null)
		{
			return;
		}
		int num = Rooms.FindIndex((RoomContainer a) => a != null && SameGroup(a.ArtifactGroup, group));
		if (num != -1)
		{
			bool flag = num > lastIndex;
			lastIndex = num;
			float num2 = RoomLength * (float)num;
			RoomContainerParent.DOKill();
			Sequence s = DOTween.Sequence();
			s.Append(ShortcutExtensions.DOLocalMoveX(endValue: flag ? (num2 - overshoot) : (num2 + overshoot), target: RoomContainerParent, duration: durationOver).SetEase(Ease.OutQuad));
			s.Append(RoomContainerParent.DOLocalMoveX(num2, durationFinal).SetEase(Ease.InOutQuad));
		}
	}
}
