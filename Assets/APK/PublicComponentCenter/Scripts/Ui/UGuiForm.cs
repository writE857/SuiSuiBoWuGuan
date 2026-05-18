//------------------------------------------------------------
// Copyright © 2020-2021 Lefend. All rights reserved.
//------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;

namespace PublicComponentCenter
{
    public class UGuiForm : MonoBehaviour
    {
        protected virtual void Awake()
        {
            UpdateAllText();
        }

        protected virtual void Start()
        {
        }

        /// <summary>
        /// 更新语言
        /// </summary>
        protected virtual void UpdateAllText()
        {
            string tempText = string.Empty;
            //获取该界面下的所有文本，并修改内容
            Text[] texts = GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (!string.IsNullOrEmpty(texts[i].text))
                {
                    tempText = GameEntry.Localization.GetString(texts[i].text);
                    tempText = tempText.Replace("|", "\r\n");
                    texts[i].text = tempText;
                }
            }
        }
    }
}