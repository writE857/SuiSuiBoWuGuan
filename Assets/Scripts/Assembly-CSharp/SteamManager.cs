using System;
using AOT;
using UnityEngine;

[DisallowMultipleComponent]
public class SteamManager : MonoBehaviour
{
	protected static bool s_EverInitialized;

	protected static SteamManager s_instance;

	protected bool m_bInitialized;

	protected static SteamManager Instance
	{
		get
		{
			if (s_instance == null)
			{
				return new GameObject("SteamManager").AddComponent<SteamManager>();
			}
			return s_instance;
		}
	}

	public static bool Initialized => Instance.m_bInitialized;

	[MonoPInvokeCallback(typeof(Action))]
	protected static void SteamAPIDebugTextHook()
	{
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void InitOnPlayMode()
	{
		s_EverInitialized = false;
		s_instance = null;
	}

	protected virtual void Awake()
	{
		if (s_instance != null)
		{
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		s_instance = this;
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		m_bInitialized = false;
		s_EverInitialized = true;
	}

	protected virtual void OnEnable()
	{
		if (s_instance == null)
		{
			s_instance = this;
		}
	}

	protected virtual void OnDestroy()
	{
		if (!(s_instance != this))
		{
			s_instance = null;
		}
	}

	protected virtual void Update()
	{
	}
}
