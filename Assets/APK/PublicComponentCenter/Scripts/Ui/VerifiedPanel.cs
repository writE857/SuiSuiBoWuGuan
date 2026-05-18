//------------------------------------------------------------
// Copyright © 2020-2021 Lefend. All rights reserved.
//------------------------------------------------------------

#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PublicComponentCenter
{
    public class VerifiedPanel : MonoBehaviour
    {
        #region Properties

        public InputField IptFieldUserName;
        public InputField IptFieldIdentityNum;
        public Button BtnConfirm;
        public Text TextError;

        public Text Text_Verified;

        public GameObject Panel_Verified;

        public GameObject Panel_Curfew;
        public Text TextCurfew;
        public GameObject Panel_Tips;

        public Text TextTipsLeftTime;
        public Text TextTipsContent;

        #endregion

        #region Mono

        void Start()
        {
            TextError.gameObject.SetActive(false);
            BtnConfirm.onClick.AddListener(OnBtnConfirmClick);
            RegisterGameEvent();
            Text_Verified.text =
                "尊敬的游戏用户：\r\n\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0您好，根据《国家新闻出版署关于防止未成年人沉迷网络游戏的通知》要求，游戏用户需要登记如下个人信息：";
        }

        void OnDestroy()
        {
            UnRegisterGameEvent();
        }

        #endregion

        #region BtnEvent

        private void OnBtnConfirmClick()
        {
            GameEntry.Antiaddiction.AddText("开始认证");
            ConfirmInteractable(false);
            string userName = IptFieldUserName.text;
            string identityNum = IptFieldIdentityNum.text;
            if (GameEntry.Antiaddiction.TestMode)
            {
                userName = Constant.Setting.TestUserName;
                identityNum = Constant.Setting.TestUserIdentityNum;
            }
            else
            {
                userName = IptFieldUserName.text;
                identityNum = IptFieldIdentityNum.text;
            }


            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(identityNum) || identityNum.Length < 18)
            {
                ShowErrorText();
                return;
            }

            GameEntry.Antiaddiction.VerifiedIdentity(userName, identityNum);
        }

        public void OnBtnQuitClick()
        {
            QuitGame();
        }

        #endregion

        #region GameEvent

        /// <summary>
        /// 实名验证失败事件处理方法
        /// </summary>
        /// <param name="sender"> 事件发送者 </param>
        /// <param name="e"> 事件 </param>
        private void OnEventVerifiedFailed(object sender, GlobalEventArgs e)
        {
            VerifiedFailedEventArgs args = e as VerifiedFailedEventArgs;
            ConfirmInteractable(true);
            if (args != null)
            {
                Debug.Log("实名认证失败");
                ShowErrorText();
            }
            else
            {
                Debug.Log("VerifiedFailedEventArgs is null");
            }
        }

        /// <summary>
        /// 未成年
        /// </summary>
        /// <param name="sender"> 事件发送者 </param>
        /// <param name="e"> 事件 </param>
        private void OnEventVerifiedUnderage(object sender, GlobalEventArgs e)
        {
            VerifiedUnderageEventArgs args = e as VerifiedUnderageEventArgs;
            ConfirmInteractable(true);
            Panel_Verified.SetActive(false);
            if (args != null)
            {
                //TODO 填充：认证成功未成年处理

                GameEntry.Antiaddiction.LoadScene();
                Debug.Log("未成年");
            }
            else
            {
                Debug.Log("VerifiedUnderageEventArgs is null");
            }
        }

        /// <summary>
        /// 成年
        /// </summary>
        /// <param name="sender"> 事件发送者 </param>
        /// <param name="e"> 事件 </param>
        private void OnEventVerifiedAdult(object sender, GlobalEventArgs e)
        {
            VerifiedAdultEventArgs args = e as VerifiedAdultEventArgs;
            ConfirmInteractable(true);
            Panel_Verified.SetActive(false);
            if (args != null)
            {
                //TODO 填充：认证成功 成年处理
                GameEntry.Antiaddiction.LoadScene();
                Debug.Log("成年");
            }
            else
            {
                Debug.Log("VerifiedAdultEventArgs is null");
            }
        }


        private void OnEventVerifiedQuitGame(object sender, GlobalEventArgs e)
        {
            VerifiedQuitGameArgs args = e as VerifiedQuitGameArgs;
            ConfirmInteractable(true);
            if (args != null)
            {
                Panel_Curfew.SetActive(true);
                if (args.IsOnlineTimeup)
                {
                    TextCurfew.text = string.Format("由于您的账号已被纳入防沉迷系统，当天在线时间已满。({0}秒后自动退出)", 5);
                    Invoke("QuitGame", 5);
                }
                else
                {
                    TextCurfew.text = "由于您的账号已被纳入防沉迷系统，每日22：00至次日8：00，无法为您提供游戏服务。";
                }

                Debug.Log("退出 游戏");
            }
            else
            {
                Debug.Log("VerifiedAdultEventArgs is null");
            }
        }

        private void OnEventVerifiedTips(object sender, GlobalEventArgs e)
        {
            VerifiedTipsArgs args = e as VerifiedTipsArgs;
            ConfirmInteractable(true);
            if (args != null)
            {
                if (args.IsHoliday)
                {
                    TextTipsContent.text = "您是未成年人，按照有关规定，您法定节假日只能在线180分钟游戏。";
                }
                else
                {
                    TextTipsContent.text = "您是未成年人，按照有关规定，您非法定节假日（含周六日）只能在线90分钟游戏。";
                }

                TextTipsLeftTime.text = string.Format("剩余时间：{0}分钟", args.LeftTime);
                Panel_Tips.SetActive(true);
            }
            else
            {
                Debug.Log("VerifiedAdultEventArgs is null");
            }
        }

        #endregion

        #region Helper

        private void RegisterGameEvent()
        {
            GameEntry.Event.Subscribe(VerifiedFailedEventArgs.EventId, OnEventVerifiedFailed);
            GameEntry.Event.Subscribe(VerifiedUnderageEventArgs.EventId, OnEventVerifiedUnderage);
            GameEntry.Event.Subscribe(VerifiedAdultEventArgs.EventId, OnEventVerifiedAdult);
            GameEntry.Event.Subscribe(VerifiedQuitGameArgs.EventId, OnEventVerifiedQuitGame);
            GameEntry.Event.Subscribe(VerifiedTipsArgs.EventId, OnEventVerifiedTips);
        }

        private void UnRegisterGameEvent()
        {
            GameEntry.Event.Unsubscribe(VerifiedFailedEventArgs.EventId, OnEventVerifiedFailed);
            GameEntry.Event.Unsubscribe(VerifiedUnderageEventArgs.EventId, OnEventVerifiedUnderage);
            GameEntry.Event.Unsubscribe(VerifiedAdultEventArgs.EventId, OnEventVerifiedAdult);
            GameEntry.Event.Unsubscribe(VerifiedQuitGameArgs.EventId, OnEventVerifiedQuitGame);
            GameEntry.Event.Unsubscribe(VerifiedTipsArgs.EventId, OnEventVerifiedTips);
        }

        private void ConfirmInteractable(bool interactable)
        {
            BtnConfirm.interactable = interactable;
        }

        private void ShowErrorText(string tips = "")
        {
            TextError.gameObject.SetActive(true);
            TextError.text = string.IsNullOrEmpty(tips) ? "请输入正确的身份信息" : tips;
            ConfirmInteractable(true);
        }


        private void QuitGame()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion

        #region 备用代码

        //private bool isNeedLoadScen = false;

        public void OnBtnTipsCloseClick()
        {
            //if (!isNeedLoadScen) return;
            GameEntry.Antiaddiction.LoadScene();
        }

        #endregion
    }
}