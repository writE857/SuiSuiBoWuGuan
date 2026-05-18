using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class SettingsEntryLogic : MonoBehaviour
{
	public SettingsEntry SettingsEntry;

	public string SaveID = "Settings ID??";

	public string SaveStringValue;

	public int SaveIndex;

	public int DefaultIndex;

	public List<string> options = new List<string>();

	private void Start()
	{
		SettingsEntry settingsEntry = SettingsEntry;
		settingsEntry.OnSelectedIndexChanged = (UnityAction<int>)Delegate.Combine(settingsEntry.OnSelectedIndexChanged, new UnityAction<int>(IndexChanged));
	}

	protected virtual void InitOptions()
	{
	}

	private void IndexChanged(int index)
	{
		if (options == null || options.Count == 0)
		{
			SaveIndex = -1;
			SaveStringValue = string.Empty;
			return;
		}
		SaveIndex = Mathf.Clamp(index, 0, options.Count - 1);
		SaveStringValue = options[SaveIndex];
		Apply();
	}

	public void Load()
	{
		InitOptions();
		if (options == null)
		{
			options = new List<string>();
		}
		SaveStringValue = PlayerPrefs.GetString(SaveID + "_String", "");
		SaveIndex = PlayerPrefs.GetInt(SaveID + "_Int", -1);
		int num = options.FindIndex((string a) => a == SaveStringValue);
		num = ((num == -1) ? SaveIndex : num);
		num = (SaveIndex = ((num == -1) ? DefaultIndex : num));
		if (options.Count == 0)
		{
			SaveIndex = -1;
			SaveStringValue = string.Empty;
			SettingsEntry.SetData(options, SaveIndex);
			Debug.LogWarning("Settings entry has no options: " + SaveID, this);
			return;
		}
		SaveIndex = Mathf.Clamp(SaveIndex, 0, options.Count - 1);
		SaveStringValue = options[SaveIndex];
		SettingsEntry.SetData(options, SaveIndex);
		Apply();
	}

	private void Apply()
	{
		Save();
		_Apply();
	}

	protected virtual void _Apply()
	{
	}

	protected virtual void _Load()
	{
	}

	public void Save()
	{
		PlayerPrefs.SetString(SaveID + "_String", SaveStringValue);
		PlayerPrefs.SetInt(SaveID + "_Int", SaveIndex);
		Debug.Log("Save - " + SaveID + ": " + SaveStringValue + ", " + SaveIndex);
		_Save();
	}

	protected virtual void _Save()
	{
	}
}
