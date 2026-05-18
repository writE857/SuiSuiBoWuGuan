using UnityEngine;

public class FitBoxColliderToSprite : MonoBehaviour
{
	private SpriteRenderer SpriteRenderer;

	private BoxCollider BoxCollider;

	private Bounds lastBounds;

	private void Start()
	{
		SpriteRenderer = GetComponent<SpriteRenderer>();
		BoxCollider = GetComponent<BoxCollider>();
	}

	private void Update()
	{
		if (!(SpriteRenderer == null) && !(BoxCollider == null))
		{
			Bounds localBounds = SpriteRenderer.localBounds;
			if (localBounds != lastBounds)
			{
				lastBounds = localBounds;
				BoxCollider.center = localBounds.center;
				BoxCollider.size = localBounds.size;
			}
		}
	}
}
