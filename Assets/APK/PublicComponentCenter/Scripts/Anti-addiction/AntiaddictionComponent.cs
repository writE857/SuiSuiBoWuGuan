//------------------------------------------------------------
// Copyright © 2020-2021 Lefend. All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections;
using System.Text;
using LitJson;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PublicComponentCenter
{
    /// <summary>
    /// 实名制模块
    /// </summary>
    public class AntiaddictionComponent : ComponentBase
    {
        private string m_IdentityNum = "";

        /// <summary> 是否是第一次获取剩余时间 </summary>
        private bool isFirstGet = false;

        /// <summary> 今日时间上限(分) </summary>
        private int canPlayTime = 0;

        /// <summary> 验证结果:0-未认证，1-认证未成年，2认证成年 </summary>
        private int verifiededNum = 0;

        /// <summary> 调试用面板 </summary>
        public Text text;

        [SerializeField, HideInInspector] private bool m_TestMode = false;
        [SerializeField, HideInInspector] private bool m_ClearDataRuntime = false;
        [SerializeField, HideInInspector] private bool m_Enable = true;
        [SerializeField] private GameObject Panel_Loading;

        [SerializeField] private Text m_TextLoading;

        private const string m_LoadingString = "加载中......";
        private int m_LoadingStrIndex = 3;
        [Tooltip("Splash时间"), Range(0, 10)] public float SplashTime = 6;
        public GameObject CanvasSplash;

        /// <summary>
        /// 是否开启实名制
        /// </summary>
        public bool Enable
        {
            get { return m_Enable; }
            set { m_Enable = value; }
        }


        /// <summary>
        /// 测试模式
        /// </summary>
        public bool TestMode
        {
            get { return m_TestMode; }
            set { m_TestMode = value; }
        }

        /// <summary>
        /// 运行时清空数据
        /// </summary>
        public bool ClearDataRuntime
        {
            get { return m_ClearDataRuntime; }
            set { m_ClearDataRuntime = value; }
        }

        public void AddText(string value)
        {
            if (text.text.Length < 2)
            {
                text.text = value;
            }
            else
            {
                text.text += "\r\n" + value;
            }
        }

        void Start()
        {
            if (Enable)
            {
                if (ClearDataRuntime)
                {
                    DeleteKey();
                }

                if (TestMode)
                {
                    SplashTime = 1;
                    text.transform.parent.parent.gameObject.SetActive(true);
                }

                CanvasSplash.SetActive(true);
                CheckVerifiedState();
                text.gameObject.SetActive(TestMode);
                Invoke("HideSplash", SplashTime);
                InvokeRepeating("UpdateText", 0, 0.6f);
            }
            else
            {
                SplashTime = 0;
                CanvasSplash.SetActive(false);
                LoadScene();
                text.transform.parent.parent.gameObject.SetActive(false);
            }
        }

        private void DeleteKey()
        {
            PlayerPrefs.DeleteKey(Constant.Data.IsVerifiedKey);
            PlayerPrefs.DeleteKey(Constant.Data.IdentityNum);
            PlayerPrefs.DeleteKey(Constant.Data.UserName);
            Debug.Log("清空实名认证数据成功");
            AddText("清空实名认证数据成功");
        }

        /// <summary>
        /// 实名验证
        /// </summary>
        /// <param name="userName"> 用户名 </param>
        /// <param name="identityNum"> 身份证号 </param>
        public void VerifiedIdentity(string userName, string identityNum)
        {
            try
            {
                StopAllCoroutines();
                StartCoroutine(IeVerifiedIdentity(userName, identityNum));
            }
            catch (Exception e)
            {
                AddText("StartCoroutine Error：" + e.Message);
            }
        }

        IEnumerator IeVerifiedIdentity(string userName, string identityNum)
        {
            AddText("开始向阿里验证信息");
#if UNITY_EDITOR

#else
            string url = string.Format("{0}?cardno={1}&name={2}", Constant.Url.VerifySysUrl, identityNum,
                UnityWebRequest.EscapeURL(userName));

            UnityWebRequest www = UnityWebRequest.Get(url);
            www.SetRequestHeader(Constant.Url.HeaderName, Constant.Url.AppCode);

            AddText("开始请求......");

            yield return www.Send();

            if (www.isNetworkError || www.isHttpError)
            {
                GameEntry.Event.Fire(this, ReferencePool.Acquire<VerifiedFailedEventArgs>());
                AddText("网络错误");
                Debug.LogFormat("网络错误.");
            }
            else

#endif

            {
#if UNITY_EDITOR
                string jsonStr = Constant.Setting.AliRespInTestMode;
                //编译器模拟HTTP请求延时
                yield return new WaitForSeconds(1);
#else
                string jsonStr = www.downloadHandler.text;
#endif

                AddText(string.Format("下载信息是：{0}", jsonStr));
                AddText("开始解析数据");
                ALiRes aLiRes = JsonMapper.ToObject<ALiRes>(jsonStr);
                if (aLiRes == null)
                {
                    AddText("aLiRes is null." + "--->>>" + JsonMapper.ToJson(aLiRes));
                    yield break;
                }

                string res = aLiRes.resp.desc;
                string birthdayStr = aLiRes.data.birthday;

                if (string.IsNullOrEmpty(res) || res != "匹配")
                {
                    AddText("身份验证失败,进入游客模式");
                    Debug.LogFormat("身份验证失败,进入游客模式");
                    GameEntry.Event.Fire(this, ReferencePool.Acquire<VerifiedFailedEventArgs>());
                }
                else
                {
                    //验证成功第一步：储存信息到远端
                    AddText("验证成功第一步：储存信息到远端");
                    yield return StartCoroutine(IeSaveDataToServer(userName, identityNum));

                    //验证成功第二步：本地处理
                    m_IdentityNum = identityNum;
                    PlayerPrefs.SetString(Constant.Data.IdentityNum, m_IdentityNum);
                    DateTime date = Convert.ToDateTime(birthdayStr);
                    AddText("验证成功第二步：本地处理");

                    TimeSpan span = DateTime.Now.Subtract(date);
                    if (span.Days / 365 >= 18)
                    {
                        AddText("验证成功,成年.");
                        GameEntry.Event.Fire(this, ReferencePool.Acquire<VerifiedAdultEventArgs>());
                        PlayerPrefs.SetInt(Constant.Data.IsVerifiedKey, (int) VerifiedState.Adult);
                    }
                    else
                    {
                        AddText("验证成功,未成年.");
                        GameEntry.Event.Fire(this, ReferencePool.Acquire<VerifiedUnderageEventArgs>());
                        PlayerPrefs.SetInt(Constant.Data.IsVerifiedKey, (int) VerifiedState.Underage);
                        StartUpdateOnlineTime();
                    }
                }
            }
        }

        IEnumerator IeSaveDataToServer(string userName, string identityNum)
        {
            PlayerInfo info = new PlayerInfo(Constant.Setting.GameId, identityNum, userName);
            string infoJson = JsonMapper.ToJson(info);
            byte[] body = Encoding.UTF8.GetBytes(infoJson);
            UnityWebRequest unityWeb = new UnityWebRequest(Utility.Url.GetAuthenticationStatusModifyUrl(),
                Constant.Url.RequestMethodPost);
            unityWeb.uploadHandler = new UploadHandlerRaw(body);
            unityWeb.SetRequestHeader(Constant.Url.RequestHeaderName, Constant.Url.RequestHeaderValue);
            unityWeb.downloadHandler = new DownloadHandlerBuffer();
            AddText("发送储存请求");
            yield return unityWeb.Send();
            if (unityWeb.isNetworkError)
            {
                Debug.Log(unityWeb.error);
                AddText("网络错误....");
            }
            else
            {
                Debug.LogFormat("Form upload complete! data Is:{0}", unityWeb.downloadHandler.text);
                AddText("远端存储数据成功.");
            }
        }

        private void UpdateOnlineTime()
        {
            StartCoroutine(IeUpdateOnlineTime());
        }

        IEnumerator IeUpdateOnlineTime()
        {
            using (UnityWebRequest webRequest =
                UnityWebRequest.Get(Utility.Url.GetOnlineTimeTodayUrl(m_IdentityNum)))
            {
                yield return webRequest.Send();
                if (webRequest.isNetworkError)
                {
                    //Debug.Log("网络出错");
                    Debug.LogFormat("Error:{0}", webRequest.error);
                }
                else
                {
                    //获取状态成功
                    AddText(string.Format("请求的数据是：{0}", webRequest.downloadHandler.text));

                    try
                    {
                        UserOnlineResult userRes =
                            JsonMapper.ToObject<UserOnlineResult>(webRequest.downloadHandler
                                .text);
                        //晚22——早8点不可进入游戏
                        bool isCanPlay = userRes.can_play == 1;
                        //模拟宵禁，正式包关掉
                        //isCanPlay = false;
                        VerifiedQuitGameArgs vqga = ReferencePool.Acquire<VerifiedQuitGameArgs>();
                        if (!isCanPlay)
                        {
                            //弹出宵禁提示
                            AddText("宵禁");
                            vqga.Fill(false);
                            GameEntry.Event.Fire(this, vqga);
                            yield break;
                        }

                        bool isHoliday = userRes.is_holiday == 1;
                        //isHoliday = true;
                        canPlayTime = isHoliday ? 180 : 90;
                        int durationTodayTime = Utility.Timer.MillisecondToMinute(userRes.content.duration_today);

                        if (!isFirstGet)
                        {
                            AddText("第一次运行");
                            VerifiedTipsArgs tipsArgs = ReferencePool.Acquire<VerifiedTipsArgs>();
                            tipsArgs.Fill(canPlayTime - durationTodayTime, isHoliday);
                            GameEntry.Event.Fire(this, tipsArgs);
                            isFirstGet = true;
                        }

                        //到时间上限,弹出退出弹窗(正式包关掉)
                        //durationTodayTime = 10000;
                        if (durationTodayTime >= canPlayTime)
                        {
                            AddText("到时间上限,弹出退出弹窗");
                            vqga.Fill(true);
                            GameEntry.Event.Fire(this, vqga);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("error:" + e.Message);
                    }
                }
            }
        }


        private void CheckVerifiedState()
        {
            Debug.LogFormat("检查认证状态.");
            verifiededNum = PlayerPrefs.GetInt(Constant.Data.IsVerifiedKey);

            string verifiedStr = "";
            switch (verifiededNum)
            {
                case (int) VerifiedState.NotVerified:
                    //  开始认证流程
                    verifiedStr = "未认证";
                    StartVerifide(true);
                    break;
                case (int) VerifiedState.Underage:
                    verifiedStr = "未成年";
                    m_IdentityNum = PlayerPrefs.GetString(Constant.Data.IdentityNum, "");
                    //  未成年处理
                    StartVerifide(false);
                    StartUpdateOnlineTime();
                    GameEntry.Antiaddiction.ShowLoadingPanel(true);
                    break;
                case (int) VerifiedState.Adult:
                    verifiedStr = "成年";
                    //直接进入游戏,不处理
                    StartVerifide(false);
                    break;
            }

            Debug.LogFormat("verifieded is:{0}", verifiedStr);
            AddText(string.Format("verifieded is:{0}", verifiedStr));
        }

        private void StartVerifide(bool showVerifiedPanel)
        {
            GameObject uiRoot = GameObject.Find(Constant.Res.UiRoot);

            if (!uiRoot)
            {
                GameObject uiRootRes = Resources.Load<GameObject>(Constant.ResPath.UiPath + Constant.Res.UiRoot);
                uiRoot = Instantiate(uiRootRes, Vector3.zero, Quaternion.identity);
                uiRoot.name = Constant.Res.UiRoot;
                uiRoot.transform.SetParent(transform);
                uiRoot.transform.SetAsFirstSibling();
                uiRoot.transform.localPosition = Vector3.zero;
                uiRoot.transform.localEulerAngles = Vector3.zero;
                uiRoot.transform.localScale = Vector3.zero;
            }

            uiRoot.transform.GetChild(0).gameObject.SetActive(showVerifiedPanel);
            EventSystem eventSys = FindObjectOfType<EventSystem>();
            if (!eventSys)
            {
                GameObject eventSystemRes =
                    Resources.Load<GameObject>(Constant.ResPath.UiPath + Constant.Res.EventSystem);
                eventSys = Instantiate(eventSystemRes, Vector3.zero, Quaternion.identity).GetComponent<EventSystem>();
                eventSys.name = Constant.Res.EventSystem;
            }

            eventSys.transform.SetParent(transform);
        }

        private void StartUpdateOnlineTime()
        {
            InvokeRepeating("UpdateOnlineTime", 0, 20);
        }


        private void UpdateText()
        {
            if (!Panel_Loading.activeInHierarchy) return;
            m_LoadingStrIndex++;
            if (m_LoadingStrIndex > m_LoadingString.Length - 1)
            {
                m_LoadingStrIndex = 3;
            }

            m_TextLoading.text = m_LoadingString.Substring(0, m_LoadingStrIndex);
        }


        private void HideSplash()
        {
            Debug.Log("隐藏Splash");
            CanvasSplash.SetActive(false);
        }

        public void ShowLoadingPanel(bool show)
        {
            Panel_Loading.SetActive(show);
        }

        public void CanvasLogEnable(bool enable)
        {
            text.transform.parent.parent.gameObject.SetActive(enable);
        }

        public void LoadScene()
        {
            try
            {
                GameEntry.Antiaddiction.ShowLoadingPanel(Enable);
                SceneManager.LoadScene(1);
            }
            catch (Exception e)
            {
                Debug.LogWarningFormat("请确保游戏正式场景在BuildSetting中的索引为1.\r\n错误信息：{0}", e.Message);
            }
        }
    }
}