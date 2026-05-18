using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

[CreateAssetMenu]
public class TextStuff : ScriptableObject
{
	public TextAsset originalText;

	public List<string> rawLines = new List<string>();

	public List<string> ResultLines = new List<string>();

	public string resultPath = "D://result.txt";

	private void DoIt()
	{
		ResultLines.Clear();
		string text = originalText.text;
		rawLines = text.Split("\n").ToList();
		rawLines.RemoveAll((string a) => string.IsNullOrWhiteSpace(a));
		for (int num = 0; num < rawLines.Count; num++)
		{
			rawLines[num] = rawLines[num].Trim();
		}
		rawLines.RemoveAll((string a) => !a.EndsWith("prereleased") && !a.StartsWith("Change"));
		int num2 = 0;
		while (num2 < rawLines.Count - 1)
		{
			if (rawLines[num2].StartsWith("Change") && rawLines[num2 + 1].StartsWith("Change"))
			{
				rawLines.RemoveAt(num2);
			}
			else
			{
				num2++;
			}
		}
		for (int num3 = 0; num3 < rawLines.Count; num3++)
		{
			if (rawLines[num3].StartsWith("Change"))
			{
				rawLines[num3] = ExtractChangeSetAndDate(rawLines[num3]);
			}
			else
			{
				rawLines[num3] = TryExtractLeadingNumber(rawLines[num3]);
			}
		}
		for (int num4 = 0; num4 < rawLines.Count - 1; num4 += 2)
		{
			ResultLines.Add(rawLines[num4] ?? "");
		}
		File.WriteAllLines(resultPath, ResultLines);
	}

	private static string TryExtractLeadingNumber(string line)
	{
		long result = 0L;
		int num = line.IndexOf(' ');
		if (num <= 0)
		{
			return result.ToString();
		}
		long.TryParse(line.Substring(0, num), out result);
		return result.ToString();
	}

	private static string ExtractChangeSetAndDate(string input)
	{
		int num = input.IndexOf('#');
		int num2 = input.IndexOf(' ', num);
		long.Parse(input.Substring(num + 1, num2 - num - 1));
		int num3 = input.IndexOf('·');
		string[] array = input.Substring(num3 + 1).Trim().Split('–');
		string text = array[0].Trim();
		string text2 = array[1].Replace("UTC", "").Trim();
		return DateTime.ParseExact(text + " " + text2, "d MMMM yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal).ToString("HH:mm:ss") ?? "";
	}
}
