using System;
using System.Collections.Generic;
using AdEvent;
using APK;
using PublicComponentCenter;
using UnityEngine;

[DefaultExecutionOrder(int.MinValue)]
public class AutoAdSettings : MonoBehaviour
{
    public static event Action OnRefresh;
    public static AutoAdSettings Instance { get; private set; }

    [Header("预制了部分响应")] [SerializeField] private bool 模拟激励 = true;
    private Ori orientation;

    [SerializeField] private GamePlatform platform;
#if UNITY_EDITOR
    [TextArea] public string testCmd;
#endif
    public GamePlatform Platform
    {
        get => platform;
        set => platform = value;
    }

    public bool EyePanelActive
    {
        get => EyeManager.Instance.Enabled;
        set => EyeManager.Instance.Enabled = value;
    }

    public int Orientation
    {
        get => (int) orientation;
        set { orientation = (Ori) value; }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            DontDestroyOnLoad(gameObject);
            Instance = this;
            gameObject.name = nameof(AutoAdSettings);
            orientation = InitOrientation();
            Debug.Log(orientation);
            _ = typeof(CmdManager);
        }
        else
        {
            DestroyImmediate(this);
        }
    }

    private void OnEnable() => OnRefresh += RefreshOther;

    private void OnDisable() => OnRefresh -= RefreshOther;

    private void Start()
    {
        var initCmd = new List<string>()
        {
            $"SetPlatform {platform.ToString()}",
        };
#if UNITY_EDITOR
        if(模拟激励)
            initCmd.Add("Reward True");
#endif
        Receiver(string.Join("\n", initCmd));
    }

    private void Init()
    {
        GameEntry.Ad.CurrentGamePlatformType = Platform;
    }

    private void RefreshOther()
    {
        try
        {
            Init();
        }
        catch
        {
        }

        try
        {
            GameEntry.Ad.Init();
        }
        catch
        {
        }
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Receiver(testCmd);
        }
#endif
    }

    public void Receiver(string msg)
    {
        try
        {
            var tokens = msg.Split(new string[] {"\r", "\n"}, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens) CmdManager.Execute(token);
        }
        finally
        {
            OnRefresh?.Invoke();
        }
    }

    private static Ori InitOrientation()
    {
        return Screen.width > Screen.height ? Ori.Landscape : Ori.Portrait;
    }
}