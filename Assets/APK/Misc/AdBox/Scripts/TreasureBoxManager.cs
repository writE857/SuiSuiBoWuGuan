using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TreasureBoxManager : MonoBehaviour
{
    public Slider BoxSlider;
    public float MaxValue;
    public float LostSpeed = 2;
    public float Increment = 1f;
    private bool IsFinish;

    private void Init()
    {
        IsFinish = false;
        BoxSlider.maxValue = MaxValue;
        BoxSlider.value = 0f;
        StartCoroutine(LoseProgress());
    }

    private void OnEnable()
    {
        Init();
    }

    private void Update()
    {
        CheckIsFinish();
    }

    private void CheckIsFinish()
    {
        var CurrentValue = GetCurrentSliderValue(BoxSlider);
        if ((MaxValue - CurrentValue) <= BoxSlider.maxValue / 2f && !IsFinish)
        {
            IsFinish = true;
            AdUtils.BaoXiangReward();
            gameObject.SetActive(false);
        }
    }

    private IEnumerator LoseProgress()
    {
        while (!IsFinish)
        {
            BoxSlider.value -= (Time.deltaTime * LostSpeed);
            yield return null;
        }
    }

    public void OnClickOpenBox()
    {
        IncreaseSliderValue(BoxSlider, Increment);
    }

    private float GetCurrentSliderValue(Slider slider)
    {
        return slider.value;
    }

    private void IncreaseSliderValue(Slider slider, float value)
    {
        slider.value += value;
    }

    private void OnDisable()
    {
        StopCoroutine(LoseProgress());
    }
}