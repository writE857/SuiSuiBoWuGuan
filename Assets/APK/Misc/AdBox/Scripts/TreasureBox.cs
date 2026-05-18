using AdEvent;
using PublicComponentCenter;
using UnityEngine;

public class TreasureBox : MonoBehaviour
{
    public bool boxEnabled;
    public int boxFirstTime = 180;
    public int boxRepeatTime = 120;
    private GameObject boxPanel;
    public bool adEnabled;
    public int adFirstTime = 45;
    public int adRepeatTime = 45;
    public static TreasureBox Instance { get; private set; }

    public void ShowBoxHanler()
    {
        boxPanel.SetActive(true);
    }

    protected void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        AutoAdSettings.OnRefresh += SetOrientation;
        SetOrientation();
    }

    private void SetOrientation()
    {
        for (int i = 0; i < transform.childCount; i++) transform.GetChild(i).gameObject.SetActive(false);
        boxPanel = transform.GetChild(AutoAdSettings.Instance.Orientation).gameObject;
    }

    private void OnDestroy()
    {
        AutoAdSettings.OnRefresh -= SetOrientation;
    }
    private void Start()
    {
        boxPanel.SetActive(false);
        if (boxEnabled) InvokeRepeating(nameof(RepeatBox), boxFirstTime, boxRepeatTime);
        if (adEnabled) InvokeRepeating(nameof(RepeatAd), adFirstTime, adRepeatTime);
    }

    private void RepeatBox() => AdUtils.ShowBox();
    private void RepeatAd() => AdUtils.ShowBlackAd();
}