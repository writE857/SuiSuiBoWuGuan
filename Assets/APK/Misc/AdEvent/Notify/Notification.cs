using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace AdEvent
{
    public class Notification : MonoBehaviour
    {
        private static readonly object SyncRoot = new object();
        private static volatile Notification instance;
        
        public static Notification Instance
        {
            get
            {
                if (instance != null) return instance;
                lock (SyncRoot)
                {
                    var prefab = Resources.Load<GameObject>($"{nameof(Notification)}");
                    var o = Instantiate(prefab);
                    DontDestroyOnLoad(o);
                    instance = o.TryGetComponent<Notification>(out var n) ? n : o.AddComponent<Notification>();
                }
                return instance;
            }
        }

        public const float DisplaySeconds = 2f;

        private GameObject obj;
        private Image image;
        private Text text;
        private Color primaryColor;

        private void Awake()
        {
            transform.SetAsLastSibling();
            obj = transform.GetChild(0).gameObject;
            image = GetComponentInChildren<Image>();
            text = GetComponentInChildren<Text>();
            primaryColor = image.color;
        }

        public void Notify(string s,float seconds=DisplaySeconds)
        {
            text.text = s;
            image.color = primaryColor;
            StartCoroutine(AsyncNotify(seconds));
        }

        public void Notify(string s, Color bgColor,float seconds=DisplaySeconds)
        {
            text.text = s;
            image.color = bgColor;
            StartCoroutine(AsyncNotify(seconds));
        }

        private IEnumerator AsyncNotify(float seconds)
        {
            obj.SetActive(true);
            yield return new WaitForSeconds(seconds);
            obj.SetActive(false);
        }
    }
}
