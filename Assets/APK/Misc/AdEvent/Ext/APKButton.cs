using System;
using System.Linq;
using PublicComponentCenter;
using UnityEngine;

public class APKButton : MonoBehaviour
{
    [SerializeField] private BtnType type;

    private void Awake()
    {
        SetUGUI();
        SetNGUI();
        AutoAdSettings.OnRefresh += SetPlatform;
        SetPlatform();
    }

    private void OnDestroy()
    {
        AutoAdSettings.OnRefresh -= SetPlatform;
    }

    private void SetPlatform()
    {
        int bitCode = 0;
        switch (AutoAdSettings.Instance.Platform)
        {
            case GamePlatform.Hw:
                bitCode = 0b1001;
                break;
            case GamePlatform.Oppo:
                bitCode = 0b1110;
                break;
            case GamePlatform.Vivo:
                bitCode = 0b1100;
                break;
            case GamePlatform.Xm:
                bitCode = 0b1000;
                break;
            case GamePlatform.Harmony:
                bitCode = 0b1000;
                break;
            default:
                $"不支持自动设置的平台 :{AutoAdSettings.Instance.Platform}".Log();
                break;
        }

        gameObject.SetActive((bitCode & (int) type) != 0);
    }

    private void SetUGUI()
    {
        if (TryGetComponent<UnityEngine.UI.Button>(out var btn))
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnClick);
        }

        var text = GetComponentInChildren<UnityEngine.UI.Text>();
        if (text != null) text.text = type.ToString();
    }

    private void SetNGUI()
    {
        if (!HasNGUI) return;
        if (TryGetType("UILabel", out var uiLabelType))
        {
            var uiLabel = GetComponentInChildren(uiLabelType);
            if (uiLabel != null)
            {
                uiLabelType.GetProperty("text")?.SetValue(uiLabel, type.ToString());
            }
        }
    }

    private static bool? hasNGUI;

    private static bool HasNGUI => hasNGUI ?? (hasNGUI = TryGetType("NGUITools", out _)).Value;

    private static bool TryGetType(string typeName, out Type type)
    {
        type = AppDomain.CurrentDomain.GetAssemblies().Select(e => e.GetType(typeName))
            .FirstOrDefault(e => e != null);
        return type != null;
    }

    public void OnClick()
    {
        switch (type)
        {
            case BtnType.隐私政策:
                PublicComponentCenter.GameEntry.Ad.PrivacyPolicy();
                break;
            case BtnType.联系客服:
                PublicComponentCenter.GameEntry.Ad.ContactCustomerService();
                break;
            case BtnType.更多精彩:
                PublicComponentCenter.GameEntry.Ad.MoreWonderful();
                break;
            case BtnType.注销账号:
                PublicComponentCenter.GameEntry.Ad.CleanPackageData();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}

public enum BtnType
{
    隐私政策 = 1 << 3,
    联系客服 = 1 << 2,
    更多精彩 = 1 << 1,
    注销账号 = 1
}