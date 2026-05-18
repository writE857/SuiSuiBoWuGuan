using UnityEngine;

public class SpriteFlipbook : MonoBehaviour
{
	public Sprite[] frames;

	public float fps = 12f;

	public bool loop = true;

	public SpriteRenderer sr;

	private int index;

	private float timer;

	private void Start()
	{
		index = Random.Range(0, frames.Length);
	}

	private void Update()
	{
		if (frames == null || frames.Length == 0)
		{
			return;
		}
		timer += Time.deltaTime;
		float num = 1f / fps;
		if (!(timer >= num))
		{
			return;
		}
		timer -= num;
		index++;
		if (index >= frames.Length)
		{
			if (loop)
			{
				index = 0;
			}
			else
			{
				index = frames.Length - 1;
			}
		}
		sr.sprite = frames[index];
	}

	public void Play(Sprite[] newFrames)
	{
		frames = newFrames;
		index = 0;
		timer = 0f;
		sr.sprite = frames[0];
	}
}
