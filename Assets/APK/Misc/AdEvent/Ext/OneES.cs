using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Fix
{
    public class OneES : MonoBehaviour
    {
        [SerializeField] private EventSystem es;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            es = GetComponent<EventSystem>();
            SceneManager.sceneLoaded += (s, m) =>
            {
                es.gameObject.SetActive(FindObjectsOfType<EventSystem>().Length == 0);
            };
        }
    }
}