using UnityEngine;
using UnityEngine.Rendering;

public class NuclearStone : Singleton<NuclearStone>
{
	public CubePiece CubePiece;

	public float materialEmissive = 10f;

	public float lightIntensity = 1f;

	public MeshRenderer Renderer;

	public Light Light;

	public Volume Volume;

	public GameObject Target;

	public PrestigeSkill PerfectSkill;

	public AnimationCurve valueCurve;

	private MaterialPropertyBlock Block;

	public float Treshold = 10000f;

	public float deathTreshold = 1000f;

	public float chainSpeed = 1f;

	private bool isDead;

	public bool IsBought;

	private void Start()
	{
		Block = new MaterialPropertyBlock();
		Block.SetFloat("EmissiveMultiplier", 0f);
		Renderer.SetPropertyBlock(Block);
		base.gameObject.SetActive(value: false);
	}

	private void Update()
	{
		if (!isDead)
		{
			float time = 1f - CubePiece.hp / CubePiece.maxHp;
			time = valueCurve.Evaluate(time);
			Light.intensity = Mathf.Lerp(0f, lightIntensity, time);
			Volume.weight = Mathf.Lerp(0f, 1f, time);
			Block.SetFloat("EmissiveMultiplier", Mathf.Lerp(0f, materialEmissive, time));
			if (CubePiece.maxHp - CubePiece.hp > Treshold)
			{
				CubePiece.hp = Mathf.Lerp(CubePiece.hp, 0f, Time.deltaTime * chainSpeed);
			}
			if (CubePiece.hp < deathTreshold)
			{
				isDead = true;
				Singleton<GameSession>.Current.PlayEnd();
			}
		}
	}
}
