using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace SceneTools.Editor
{
	public class SceneTextTool : EditorWindow
	{
		// Unity UI Text script GUID
		private const string UITextGuid = "5f7201a12d95ffc409449d95f23cf332";
		// TextMeshProUGUI script GUID
		private const string TMPTextUGUIGuid = "f4688fdb7df04437aeb418b961361dc5";
		// TextMeshPro (3D) script GUID
		private const string TMPText3DGuid = "b9839c2d141f8ee45a585ba6c3e0571e";

		private static readonly Type TMPFontAssetType;
		private static readonly bool HasTMP;

		static SceneTextTool()
		{
			// 通过反射查找 TMP_FontAsset，兼容未安装 TMP 的项目
			TMPFontAssetType = AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
				.FirstOrDefault(t => t.FullName == "TMPro.TMP_FontAsset");
			HasTMP = TMPFontAssetType != null;
		}

		private string exportPath = "Assets/SceneTexts";
		private Font newUIFont;
		private UnityEngine.Object newTMPFontAsset;
		private Vector2 scrollPos;
		private string logMessage = "";

		// 扫描范围选项
		private enum ScanScope { All, ScenesOnly, PrefabsOnly, ScriptStringsOnly }
		private ScanScope scanScope = ScanScope.All;
		private static readonly string[] ScanScopeLabels = { "全部（场景+预制体）", "仅场景", "仅预制体", "仅脚本字符串" };
		private bool includeScriptStrings = true;
	private bool filterChinese = false; // 提纯时过滤掉含中文的文本

		// 脚本字符串数组扫描缓存: scriptGUID -> { fieldName -> true }
		private static Dictionary<string, Dictionary<string, bool>> _scriptStringFields;
		// scriptGUID -> script class name
		private static Dictionary<string, string> _scriptNames;

		// 嵌套 [Serializable] 结构体内的字符串字段信息
		private struct NestedStringFieldInfo
		{
			public string innerFieldName;  // 结构体内字段名，如 "line", "eventName"
			public bool isArray;           // true = string[]/List<string>, false = 单个 string
		}
		// scriptGUID -> { outerFieldName -> List<NestedStringFieldInfo> }
		private static Dictionary<string, Dictionary<string, List<NestedStringFieldInfo>>> _scriptNestedFields;

		[MenuItem("Tools/Scene Text Tool")]
		public static void ShowWindow()
		{
			GetWindow<SceneTextTool>("场景翻译工具");
		}

		private static GUIStyle _titleStyle;
		private static GUIStyle _sectionHeaderStyle;
		private static GUIStyle _sectionBox;

		private static void InitStyles()
		{
			if (_titleStyle != null) return;

			_titleStyle = new GUIStyle(EditorStyles.boldLabel)
			{
				fontSize = 16,
				alignment = TextAnchor.MiddleCenter,
				margin = new RectOffset(0, 0, 8, 4)
			};

			_sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
			{
				fontSize = 12
			};

			_sectionBox = new GUIStyle("HelpBox")
			{
				padding = new RectOffset(10, 10, 8, 8),
				margin = new RectOffset(4, 4, 2, 6)
			};
		}

		private void OnGUI()
		{
			InitStyles();
			scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

			// Title
			GUILayout.Label("场景翻译工具", _titleStyle);
			DrawSeparator();

			// Path
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel("导出路径");
			exportPath = EditorGUILayout.TextField(exportPath);
			if (GUILayout.Button("...", GUILayout.Width(30)))
			{
				string sel = EditorUtility.OpenFolderPanel("选择导出路径", exportPath, "");
				if (!string.IsNullOrEmpty(sel))
				{
					// 转为相对路径
					string dataPath = Application.dataPath.Replace("\\", "/");
					sel = sel.Replace("\\", "/");
					if (sel.StartsWith(dataPath))
						exportPath = "Assets" + sel.Substring(dataPath.Length);
					else
						exportPath = sel;
				}
			}
			EditorGUILayout.EndHorizontal();

			// 扫描范围
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel("扫描范围");
			scanScope = (ScanScope)EditorGUILayout.Popup((int)scanScope, ScanScopeLabels);
			EditorGUILayout.EndHorizontal();
			if (scanScope != ScanScope.ScriptStringsOnly)
				includeScriptStrings = EditorGUILayout.ToggleLeft("包含脚本字符串数组（string[] / List<string>）", includeScriptStrings);

			GUILayout.Space(6);

			bool isScriptOnly = scanScope == ScanScope.ScriptStringsOnly;

			// 根据扫描范围动态生成标签
			string scopeLabel;
			switch (scanScope)
			{
				case ScanScope.PrefabsOnly: scopeLabel = "预制体"; break;
				case ScanScope.ScenesOnly: scopeLabel = "场景"; break;
				case ScanScope.ScriptStringsOnly: scopeLabel = "脚本字符串"; break;
				default: scopeLabel = "场景+预制体"; break;
			}

			// 翻译工作流（步骤式）
			EditorGUILayout.BeginVertical(_sectionBox);
			GUILayout.Label("翻译工作流", _sectionHeaderStyle);
			GUILayout.Space(4);

			// Step 1
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("①", GUILayout.Width(18));
			if (GUILayout.Button($"导出{scopeLabel}文本", GUILayout.Height(26)))
			{
				if (isScriptOnly)
				{
					ExportScriptStringArrays();
				}
				else
				{
					ExportAllSceneTexts();
					if (includeScriptStrings)
					{
						string prevLog = logMessage;
						ExportScriptStringArrays();
						logMessage = prevLog + "\n" + logMessage;
					}
				}
			}
			EditorGUILayout.EndHorizontal();
			if (isScriptOnly)
				EditorGUILayout.HelpBox("扫描场景+预制体中脚本 string[]/List<string> 字段，导出到 txt", MessageType.None);
			else
				EditorGUILayout.HelpBox($"扫描{scopeLabel}中的 Text/TMP 组件，导出到 txt 文件" +
					(includeScriptStrings ? "\n同时导出脚本 string[]/List<string> 字段" : ""), MessageType.None);

			GUILayout.Space(2);

			// Step 2
			filterChinese = EditorGUILayout.ToggleLeft("过滤已翻译的中文文本", filterChinese);
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("②", GUILayout.Width(18));
			if (GUILayout.Button("提取纯文本（去重）", GUILayout.Height(26)))
				ExtractPureTexts();
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.HelpBox("从 txt 中提取不重复文本 → extracted_texts.txt\n同时生成 translated_texts.txt 模板供翻译填写", MessageType.None);

			GUILayout.Space(2);

			// Step 3
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("③", GUILayout.Width(18));
			if (GUILayout.Button("替换翻译文本", GUILayout.Height(26)))
				ReplaceWithTranslatedTexts();
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.HelpBox("将 translated_texts.txt 中的译文批量替换回各 txt 文件", MessageType.None);

			GUILayout.Space(2);

			// Step 4
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("④", GUILayout.Width(18));
			if (GUILayout.Button($"导入{scopeLabel}文本", GUILayout.Height(26)))
			{
				if (isScriptOnly)
				{
					ImportScriptStringArrays();
				}
				else
				{
					ImportAllSceneTexts();
					if (includeScriptStrings)
					{
						string prevLog = logMessage;
						ImportScriptStringArrays();
						logMessage = prevLog + "\n" + logMessage;
					}
				}
			}
			EditorGUILayout.EndHorizontal();
			if (isScriptOnly)
				EditorGUILayout.HelpBox("将翻译后的 txt 写回场景+预制体中的脚本字符串数组", MessageType.None);
			else
				EditorGUILayout.HelpBox($"将翻译后的 txt 写回{scopeLabel}文件" +
					(includeScriptStrings ? "\n同时写回脚本字符串数组" : ""), MessageType.None);

			EditorGUILayout.EndVertical();

			GUILayout.Space(2);

			// 字体替换（脚本字符串模式下不显示）
			if (!isScriptOnly)
			{
			EditorGUILayout.BeginVertical(_sectionBox);
			GUILayout.Label("字体替换", _sectionHeaderStyle);
			GUILayout.Space(2);
			newUIFont = (Font)EditorGUILayout.ObjectField("UI Text 字体", newUIFont, typeof(Font), false);
			if (HasTMP)
				newTMPFontAsset = EditorGUILayout.ObjectField("TMP 字体", newTMPFontAsset, TMPFontAssetType, false);
			GUILayout.Space(4);
			if (GUILayout.Button($"替换所有{scopeLabel}字体", GUILayout.Height(28)))
			{
				if (newUIFont == null && newTMPFontAsset == null)
					EditorUtility.DisplayDialog("提示", "请至少选择一个字体", "确定");
				else
					ReplaceAllFonts();
			}
			EditorGUILayout.EndVertical();
			} // end if (!isScriptOnly)

			GUILayout.Space(4);
	
			// Log area
			if (!string.IsNullOrEmpty(logMessage))
			{
				DrawSeparator();
				EditorGUILayout.HelpBox(logMessage, MessageType.Info);
			}
	
			EditorGUILayout.EndScrollView();
		}

		private static void DrawSeparator()
		{
			GUILayout.Space(2);
			Rect r = EditorGUILayout.GetControlRect(false, 1);
			EditorGUI.DrawRect(r, new Color(0.5f, 0.5f, 0.5f, 0.5f));
			GUILayout.Space(2);
		}

		// Determine component type from script GUID
		private enum TextCompType { None, UIText, TMP }

		private static TextCompType GetTextCompType(string line)
		{
			if (line.Contains("guid: " + UITextGuid)) return TextCompType.UIText;
			if (line.Contains("guid: " + TMPTextUGUIGuid)) return TextCompType.TMP;
			if (line.Contains("guid: " + TMPText3DGuid)) return TextCompType.TMP;
			return TextCompType.None;
		}

		private string GetSubDir(string filePath)
		{
			return filePath.EndsWith(".prefab") ? "Prefabs" : "Scenes";
		}

		private string[] GetExportDirs()
		{
			List<string> dirs = new List<string>();
			if (scanScope == ScanScope.ScriptStringsOnly)
			{
				dirs.Add(Path.Combine(exportPath, "ScriptStrings", "Scenes"));
				dirs.Add(Path.Combine(exportPath, "ScriptStrings", "Prefabs"));
				return dirs.ToArray();
			}
			if (scanScope != ScanScope.PrefabsOnly)
				dirs.Add(Path.Combine(exportPath, "Scenes"));
			if (scanScope != ScanScope.ScenesOnly)
				dirs.Add(Path.Combine(exportPath, "Prefabs"));
			if (includeScriptStrings)
			{
				if (scanScope != ScanScope.PrefabsOnly)
					dirs.Add(Path.Combine(exportPath, "ScriptStrings", "Scenes"));
				if (scanScope != ScanScope.ScenesOnly)
					dirs.Add(Path.Combine(exportPath, "ScriptStrings", "Prefabs"));
			}
			return dirs.ToArray();
		}

		#region Export

		private void ExportAllSceneTexts()
		{
			string[] files = GetFilesByScope();
			if (files.Length == 0)
			{
				logMessage = "未找到任何文件。";
				return;
			}

			int totalCount = 0;

			for (int i = 0; i < files.Length; i++)
			{
				string filePath = files[i];
				string fileName = Path.GetFileNameWithoutExtension(filePath);
				EditorUtility.DisplayProgressBar("导出文本", fileName, (float)i / files.Length);

				List<TextEntry> entries = ParseSceneTexts(filePath);
				if (entries.Count == 0) continue;

				string subDir = Path.Combine(exportPath, GetSubDir(filePath));
				if (!Directory.Exists(subDir))
					Directory.CreateDirectory(subDir);

				string txtPath = Path.Combine(subDir, fileName + ".txt");
				using (StreamWriter writer = new StreamWriter(txtPath, false, Encoding.UTF8))
				{
					foreach (TextEntry entry in entries)
					{
						string escapedText = entry.text.Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\r", "");
						string typeTag = entry.compType == TextCompType.TMP ? "TMP" : "Text";
						writer.WriteLine($"{entry.fileID}|{typeTag}|{entry.gameObjectName}|{escapedText}");
					}
				}

				totalCount += entries.Count;
			}

			EditorUtility.ClearProgressBar();
			AssetDatabase.Refresh();
			logMessage = $"导出完成！共扫描 {files.Length} 个文件，导出 {totalCount} 条文本到 {exportPath}";
			Debug.Log(logMessage);
		}

		#endregion

		#region Extract / Replace Pure Text

		private void ExtractPureTexts()
		{
			string[] dirs = GetExportDirs();
			List<string> allTxtFiles = new List<string>();
			foreach (string dir in dirs)
			{
				if (Directory.Exists(dir))
					allTxtFiles.AddRange(Directory.GetFiles(dir, "*.txt"));
			}

			if (allTxtFiles.Count == 0)
			{
				logMessage = "未找到任何 txt 文件，请先执行步骤①导出。";
				return;
			}

			// 用 LinkedHashSet 模拟有序去重
			List<string> uniqueTexts = new List<string>();
			HashSet<string> seen = new HashSet<string>();

			foreach (string txtPath in allTxtFiles)
			{
				string[] lines = File.ReadAllLines(txtPath, Encoding.UTF8);
				foreach (string line in lines)
				{
					if (string.IsNullOrWhiteSpace(line)) continue;
					int sep3 = -1;
					int pipeCount = 0;
					for (int c = 0; c < line.Length; c++)
					{
						if (line[c] == '|')
						{
							pipeCount++;
							if (pipeCount == 3) { sep3 = c; break; }
						}
					}
					if (sep3 < 0) continue;

					string text = line.Substring(sep3 + 1);
					if (string.IsNullOrWhiteSpace(text)) continue;
					// 跳过纯数字、时间格式
					if (IsSkippable(text)) continue;
					// 过滤含中文的文本（已翻译）
					if (filterChinese && ContainsChinese(text)) continue;

					if (seen.Add(text))
						uniqueTexts.Add(text);
				}
			}

			string transDir = Path.Combine(exportPath, "Translation");
			if (!Directory.Exists(transDir)) Directory.CreateDirectory(transDir);

			string outPath = Path.Combine(transDir, "extracted_texts.txt");
			File.WriteAllLines(outPath, uniqueTexts.ToArray(), Encoding.UTF8);

			// 同时生成翻译模板文件（空行，行号对应）
			string templatePath = Path.Combine(transDir, "translated_texts.txt");
			if (!File.Exists(templatePath))
			{
				string[] emptyLines = new string[uniqueTexts.Count];
				for (int i = 0; i < uniqueTexts.Count; i++)
					emptyLines[i] = "";
				File.WriteAllLines(templatePath, emptyLines, Encoding.UTF8);
			}

			AssetDatabase.Refresh();
			logMessage = $"提取完成！共 {uniqueTexts.Count} 条不重复文本\n" +
			             "extracted_texts.txt = 原文，translated_texts.txt = 译文（按行号对应，直接粘贴）";
			Debug.Log(logMessage);
		}

		private void ReplaceWithTranslatedTexts()
		{
			string transDir = Path.Combine(exportPath, "Translation");
			string extractedPath = Path.Combine(transDir, "extracted_texts.txt");
			string translatedPath = Path.Combine(transDir, "translated_texts.txt");

			if (!File.Exists(extractedPath) || !File.Exists(translatedPath))
			{
				logMessage = "找不到 Translation/extracted_texts.txt 或 translated_texts.txt\n请先点击「提取纯文本」";
				return;
			}

			string[] origLines = File.ReadAllLines(extractedPath, Encoding.UTF8);
			string[] transLines = File.ReadAllLines(translatedPath, Encoding.UTF8);

			// 按行号配对，构建映射
			Dictionary<string, string> transMap = new Dictionary<string, string>();
			int count = Math.Min(origLines.Length, transLines.Length);
			for (int i = 0; i < count; i++)
			{
				string orig = origLines[i];
				string trans = transLines[i];
				if (!string.IsNullOrEmpty(orig) && !string.IsNullOrEmpty(trans))
					transMap[orig] = trans;
			}

			if (transMap.Count == 0)
			{
				logMessage = "没有找到有效的翻译。请在 translated_texts.txt 对应行填入译文。";
				return;
			}

			string[] dirs = GetExportDirs();
			List<string> txtFileList = new List<string>();
			foreach (string dir in dirs)
			{
				if (Directory.Exists(dir))
					txtFileList.AddRange(Directory.GetFiles(dir, "*.txt"));
			}
			int totalReplaced = 0;

			foreach (string txtPath in txtFileList)
			{
				string fileName = Path.GetFileName(txtPath);
				if (fileName == "extracted_texts.txt" || fileName == "translated_texts.txt")
					continue;

				string[] lines = File.ReadAllLines(txtPath, Encoding.UTF8);
				bool changed = false;

				for (int i = 0; i < lines.Length; i++)
				{
					string line = lines[i];
					if (string.IsNullOrWhiteSpace(line)) continue;

					int sep3 = -1;
					int pipeCount = 0;
					for (int c = 0; c < line.Length; c++)
					{
						if (line[c] == '|')
						{
							pipeCount++;
							if (pipeCount == 3) { sep3 = c; break; }
						}
					}
					if (sep3 < 0) continue;

					string prefix = line.Substring(0, sep3 + 1);
					string text = line.Substring(sep3 + 1);

					if (transMap.TryGetValue(text, out string translated))
					{
						lines[i] = prefix + translated;
						changed = true;
						totalReplaced++;
					}
				}

				if (changed)
					File.WriteAllLines(txtPath, lines, Encoding.UTF8);
			}

			AssetDatabase.Refresh();
			logMessage = $"替换完成！共替换 {totalReplaced} 处文本（映射表 {transMap.Count} 条）。";
			Debug.Log(logMessage);
		}

		private static bool IsSkippable(string text)
		{
			string t = text.Trim();
			if (t.Length == 0) return true;
			// 纯数字/百分比
			if (Regex.IsMatch(t, @"^[\d.\-\s%]+$")) return true;
			// 时间格式
			if (Regex.IsMatch(t, @"^[\d:.]+$")) return true;
			// 版本号
			if (Regex.IsMatch(t, @"^\d+\.\d+\.\d+")) return true;
			return false;
		}

		private static bool ContainsChinese(string text)
		{
			foreach (char c in text)
			{
				if (c >= 0x4E00 && c <= 0x9FFF) return true;
			}
			return false;
		}

		#endregion

		#region Import

		private void ImportAllSceneTexts()
		{
			string[] dirs = GetExportDirs();
			List<string> allTxtFiles = new List<string>();
			foreach (string dir in dirs)
			{
				if (Directory.Exists(dir))
					allTxtFiles.AddRange(Directory.GetFiles(dir, "*.txt"));
			}

			if (allTxtFiles.Count == 0)
			{
				logMessage = "未找到任何 txt 文件，请先执行步骤①导出。";
				return;
			}

			int totalReplaced = 0;

			for (int i = 0; i < allTxtFiles.Count; i++)
			{
				string txtPath = allTxtFiles[i];
				string fileName = Path.GetFileNameWithoutExtension(txtPath);
				string filePath = FindFileByName(fileName);
				if (filePath == null)
				{
					Debug.LogWarning($"找不到对应文件: {fileName}");
					continue;
				}

				EditorUtility.DisplayProgressBar("导入文本", fileName, (float)i / allTxtFiles.Count);

				// Read txt entries: fileID|type|name|text
				Dictionary<string, ImportEntry> textMap = new Dictionary<string, ImportEntry>();
				string[] lines = File.ReadAllLines(txtPath, Encoding.UTF8);
				foreach (string line in lines)
				{
					if (string.IsNullOrWhiteSpace(line)) continue;
					int sep1 = line.IndexOf('|');
					if (sep1 < 0) continue;
					int sep2 = line.IndexOf('|', sep1 + 1);
					if (sep2 < 0) continue;
					int sep3 = line.IndexOf('|', sep2 + 1);
					if (sep3 < 0) continue;

					string fileID = line.Substring(0, sep1);
					string typeTag = line.Substring(sep1 + 1, sep2 - sep1 - 1);
					string text = line.Substring(sep3 + 1);
					text = UnescapeText(text);

					textMap[fileID] = new ImportEntry
					{
						text = text,
						isTMP = typeTag == "TMP"
					};
				}

				int replaced = ReplaceSceneTexts(filePath, textMap);
				totalReplaced += replaced;
			}

			EditorUtility.ClearProgressBar();
			AssetDatabase.Refresh();
			logMessage = $"导入完成！共替换 {totalReplaced} 条文本。";
			Debug.Log(logMessage);
		}

		private struct ImportEntry
		{
			public string text;
			public bool isTMP;
		}

		private static string UnescapeText(string text)
		{
			StringBuilder sb = new StringBuilder(text.Length);
			for (int i = 0; i < text.Length; i++)
			{
				if (text[i] == '\\' && i + 1 < text.Length)
				{
					char next = text[i + 1];
					if (next == 'n') { sb.Append('\n'); i++; }
					else if (next == '\\') { sb.Append('\\'); i++; }
					else sb.Append(text[i]);
				}
				else
				{
					sb.Append(text[i]);
				}
			}
			return sb.ToString();
		}

		private int ReplaceSceneTexts(string scenePath, Dictionary<string, ImportEntry> textMap)
		{
			string[] sceneLines = File.ReadAllLines(scenePath, Encoding.UTF8);
			int replaced = 0;
			string currentFileID = null;
			TextCompType compType = TextCompType.None;

			for (int i = 0; i < sceneLines.Length; i++)
			{
				string line = sceneLines[i];

				if (line.StartsWith("--- !u!114 &"))
				{
					currentFileID = line.Substring("--- !u!114 &".Length);
					compType = TextCompType.None;
				}
				else if (line.StartsWith("--- "))
				{
					currentFileID = null;
					compType = TextCompType.None;
				}

				if (currentFileID != null)
				{
					TextCompType detected = GetTextCompType(line);
					if (detected != TextCompType.None)
						compType = detected;
				}

				if (compType == TextCompType.None || currentFileID == null) continue;

				// Match the text field: m_Text for UI Text, m_text for TMP
				string textFieldPrefix = compType == TextCompType.UIText ? "m_Text:" : "m_text:";
				if (!line.TrimStart().StartsWith(textFieldPrefix)) continue;

				// Detect multi-line quoted value
				int mTextStart = i;
				string rawText = line.Substring(line.IndexOf(textFieldPrefix) + textFieldPrefix.Length).TrimStart();
				if (rawText.StartsWith("\"") && !rawText.EndsWith("\""))
				{
					while (i + 1 < sceneLines.Length)
					{
						i++;
						if (sceneLines[i].TrimEnd().EndsWith("\""))
							break;
					}
				}
				int mTextEnd = i;

				if (textMap.TryGetValue(currentFileID, out ImportEntry entry))
				{
					string indent = line.Substring(0, line.Length - line.TrimStart().Length);
					sceneLines[mTextStart] = indent + textFieldPrefix + " " + EscapeYamlValue(entry.text);
					for (int r = mTextStart + 1; r <= mTextEnd; r++)
						sceneLines[r] = null;
					replaced++;
				}
				compType = TextCompType.None;
			}

			if (replaced > 0)
			{
				List<string> output = new List<string>(sceneLines.Length);
				for (int i = 0; i < sceneLines.Length; i++)
				{
					if (sceneLines[i] != null)
						output.Add(sceneLines[i]);
				}
				File.WriteAllLines(scenePath, output.ToArray(), Encoding.UTF8);
			}

			return replaced;
		}

		#endregion

		#region Replace Font

		private void ReplaceAllFonts()
		{
			string uiFontGuid = null;
			string tmpFontGuid = null;

			if (newUIFont != null)
			{
				uiFontGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(newUIFont));
			}
			if (newTMPFontAsset != null)
			{
				tmpFontGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(newTMPFontAsset));
			}

			string[] files = GetFilesByScope();
			int totalReplaced = 0;

			for (int i = 0; i < files.Length; i++)
			{
				string filePath = files[i];
				string fileName = Path.GetFileNameWithoutExtension(filePath);
				EditorUtility.DisplayProgressBar("替换字体", fileName, (float)i / files.Length);

				totalReplaced += ReplaceSceneFonts(filePath, uiFontGuid, tmpFontGuid);
			}

			EditorUtility.ClearProgressBar();
			AssetDatabase.Refresh();
			logMessage = $"字体替换完成！共替换 {totalReplaced} 处字体引用。";
			Debug.Log(logMessage);
		}

		private int ReplaceSceneFonts(string scenePath, string uiFontGuid, string tmpFontGuid)
		{
			string[] sceneLines = File.ReadAllLines(scenePath, Encoding.UTF8);
			int replaced = 0;
			TextCompType compType = TextCompType.None;
			bool inFontData = false;

			Regex guidRegex = new Regex(@"(guid:\s*)[a-f0-9]{32}");

			for (int i = 0; i < sceneLines.Length; i++)
			{
				string line = sceneLines[i];

				if (line.StartsWith("--- !u!114 &"))
				{
					compType = TextCompType.None;
					inFontData = false;
				}
				else if (line.StartsWith("--- "))
				{
					compType = TextCompType.None;
					inFontData = false;
				}

				TextCompType detected = GetTextCompType(line);
				if (detected != TextCompType.None)
					compType = detected;

				// UI Text: m_FontData -> m_Font
				if (compType == TextCompType.UIText && uiFontGuid != null)
				{
					if (line.TrimStart().StartsWith("m_FontData:"))
						inFontData = true;

					if (inFontData && line.TrimStart().StartsWith("m_Font:"))
					{
						Match match = guidRegex.Match(line);
						if (match.Success)
						{
							sceneLines[i] = guidRegex.Replace(line, "${1}" + uiFontGuid);
							replaced++;
						}
						inFontData = false;
					}
				}

				// TMP: m_fontAsset directly on the component
				if (compType == TextCompType.TMP && tmpFontGuid != null)
				{
					if (line.TrimStart().StartsWith("m_fontAsset:"))
					{
						Match match = guidRegex.Match(line);
						if (match.Success)
						{
							sceneLines[i] = guidRegex.Replace(line, "${1}" + tmpFontGuid);
							replaced++;
						}
					}
				}
			}

			if (replaced > 0)
			{
				File.WriteAllLines(scenePath, sceneLines, Encoding.UTF8);
			}

			return replaced;
		}

		#endregion

		#region Script String Arrays

		/// <summary>
		/// 构建脚本 GUID -> 字符串字段名映射。
		/// 通过反射扫描所有 MonoScript，找出含 public string[] / List&lt;string&gt; 或
		/// [Serializable] 结构体数组（内含 string/string[] 字段）的脚本。
		/// </summary>
		private static void BuildScriptStringFieldCache()
		{
			if (_scriptStringFields != null) return;

			_scriptStringFields = new Dictionary<string, Dictionary<string, bool>>();
			_scriptNames = new Dictionary<string, string>();
			_scriptNestedFields = new Dictionary<string, Dictionary<string, List<NestedStringFieldInfo>>>();

			string[] guids = AssetDatabase.FindAssets("t:MonoScript");
			foreach (string guid in guids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
				if (script == null) continue;

				Type type = script.GetClass();
				if (type == null || !typeof(MonoBehaviour).IsAssignableFrom(type)) continue;

				Dictionary<string, bool> fields = null;
				Dictionary<string, List<NestedStringFieldInfo>> nestedFields = null;

				foreach (FieldInfo fi in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
				{
					// --- 原有：直接 string[] / List<string> ---
					bool isStringArray = fi.FieldType == typeof(string[]);
					bool isStringList = fi.FieldType == typeof(List<string>);
					if (isStringArray || isStringList)
					{
						if (fields == null) fields = new Dictionary<string, bool>();
						fields[fi.Name] = isStringList;
						continue;
					}

					// --- 新增：[Serializable] 结构体数组 / List / 单个结构体 ---
					Type elemType = null;
					if (fi.FieldType.IsArray)
						elemType = fi.FieldType.GetElementType();
					else if (fi.FieldType.IsGenericType && fi.FieldType.GetGenericTypeDefinition() == typeof(List<>))
						elemType = fi.FieldType.GetGenericArguments()[0];

					// 单个 [Serializable] 结构体（非数组）
					if (elemType == null && fi.FieldType.IsValueType && !fi.FieldType.IsPrimitive
						&& fi.FieldType != typeof(string) && fi.FieldType.IsDefined(typeof(SerializableAttribute), false))
						elemType = fi.FieldType;

					if (elemType == null) continue;
					if (!elemType.IsDefined(typeof(SerializableAttribute), false)) continue;
					if (elemType.IsPrimitive || elemType == typeof(string)) continue;

					// 扫描结构体内部的 string / string[] / List<string> 字段
					List<NestedStringFieldInfo> innerList = null;
					foreach (FieldInfo inner in elemType.GetFields(BindingFlags.Public | BindingFlags.Instance))
					{
						bool isSingle = inner.FieldType == typeof(string);
						bool isArr = inner.FieldType == typeof(string[]);
						bool isList = inner.FieldType == typeof(List<string>);
						if (!isSingle && !isArr && !isList) continue;

						if (innerList == null) innerList = new List<NestedStringFieldInfo>();
						innerList.Add(new NestedStringFieldInfo
						{
							innerFieldName = inner.Name,
							isArray = isArr || isList
						});
					}

					if (innerList != null && innerList.Count > 0)
					{
						if (nestedFields == null) nestedFields = new Dictionary<string, List<NestedStringFieldInfo>>();
						nestedFields[fi.Name] = innerList;
					}
				}

				bool hasFlat = fields != null && fields.Count > 0;
				bool hasNested = nestedFields != null && nestedFields.Count > 0;

				if (hasFlat)
					_scriptStringFields[guid] = fields;
				if (hasNested)
					_scriptNestedFields[guid] = nestedFields;
				if (hasFlat || hasNested)
					_scriptNames[guid] = type.Name;
			}
		}

		/// <summary>
		/// 判断某行是否包含已知脚本 GUID，返回匹配的 GUID 或 null
		/// </summary>
		private static string MatchScriptGuid(string line)
		{
			if (!line.Contains("m_Script:")) return null;
			foreach (var kvp in _scriptStringFields)
			{
				if (line.Contains("guid: " + kvp.Key))
					return kvp.Key;
			}
			foreach (var kvp in _scriptNestedFields)
			{
				if (line.Contains("guid: " + kvp.Key))
					return kvp.Key;
			}
			return null;
		}

		private struct ScriptStringEntry
		{
			public string fileID;
			public string gameObjectName;
			public string scriptName;
			public string fieldName;
			public int arrayIndex;
			public string text;
		}

		/// <summary>
		/// 从场景/预制体 YAML 中解析脚本字符串数组的值
		/// </summary>
		private List<ScriptStringEntry> ParseScriptStringArrays(string filePath)
		{
			BuildScriptStringFieldCache();

			List<ScriptStringEntry> results = new List<ScriptStringEntry>();
			string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);

			// First pass: GO names + component->GO mapping (reuse existing logic)
			Dictionary<string, string> goNames = new Dictionary<string, string>();
			Dictionary<string, string> compToGo = new Dictionary<string, string>();

			string currentFileID = null;
			string currentTag = null;

			for (int i = 0; i < lines.Length; i++)
			{
				string line = lines[i];
				if (line.StartsWith("--- !u!"))
				{
					int ampIdx = line.IndexOf('&');
					if (ampIdx >= 0)
					{
						currentFileID = line.Substring(ampIdx + 1);
						int tagStart = "--- !u!".Length;
						int tagEnd = line.IndexOf(' ', tagStart);
						if (tagEnd > tagStart)
							currentTag = line.Substring(tagStart, tagEnd - tagStart);
					}
					continue;
				}
				if (currentFileID == null) continue;

				if (currentTag == "1" && line.TrimStart().StartsWith("m_Name:"))
				{
					string name = line.Substring(line.IndexOf(':') + 1).Trim();
					goNames[currentFileID] = name;
				}
				if (currentTag == "114" && line.TrimStart().StartsWith("m_GameObject:"))
				{
					Match m = Regex.Match(line, @"fileID:\s*(\d+)");
					if (m.Success)
						compToGo[currentFileID] = m.Groups[1].Value;
				}
			}

			// Second pass: find MonoBehaviour components with known script GUIDs
			currentFileID = null;
			string matchedGuid = null;
			Dictionary<string, bool> targetFields = null;
			Dictionary<string, List<NestedStringFieldInfo>> nestedTargetFields = null;

			for (int i = 0; i < lines.Length; i++)
			{
				string line = lines[i];

				if (line.StartsWith("--- !u!114 &"))
				{
					currentFileID = line.Substring("--- !u!114 &".Length);
					matchedGuid = null;
					targetFields = null;
					nestedTargetFields = null;
				}
				else if (line.StartsWith("--- "))
				{
					currentFileID = null;
					matchedGuid = null;
					targetFields = null;
					nestedTargetFields = null;
				}

				if (currentFileID == null) continue;

				// Detect script reference
				if (matchedGuid == null)
				{
					string guid = MatchScriptGuid(line);
					if (guid != null)
					{
						matchedGuid = guid;
						_scriptStringFields.TryGetValue(guid, out targetFields);
						_scriptNestedFields.TryGetValue(guid, out nestedTargetFields);
					}
					continue;
				}

				if (targetFields == null && nestedTargetFields == null) continue;

				string trimmed = line.TrimStart();
				int lineIndent = line.Length - line.TrimStart().Length;

				string goName = null; // lazy resolve

				// --- 原有：平铺 string[] / List<string> ---
				if (targetFields != null)
				{
					foreach (var field in targetFields)
					{
						string fieldKey = field.Key + ":";
						if (!trimmed.StartsWith(fieldKey)) continue;

						if (goName == null) goName = ResolveGoName(currentFileID, compToGo, goNames);
						string scriptName = _scriptNames.ContainsKey(matchedGuid) ? _scriptNames[matchedGuid] : "Unknown";

						int arrayIdx = 0;
						while (i + 1 < lines.Length)
						{
							string nextLine = lines[i + 1].TrimStart();
							if (!nextLine.StartsWith("- "))
							{
								// 空行可能是多行 YAML 值的一部分，向前探测
								if (string.IsNullOrWhiteSpace(lines[i + 1]))
								{
									int peek = i + 2;
									while (peek < lines.Length && string.IsNullOrWhiteSpace(lines[peek])) peek++;
									if (peek < lines.Length && lines[peek].TrimStart().StartsWith("- "))
									{
										i++;
										continue;
									}
								}
								break;
							}
							i++;

							string val = ReadYamlArrayItemFull(lines, ref i, nextLine.Substring(2));
							string text = UnescapeYamlValue(val);

							if (!string.IsNullOrWhiteSpace(text))
							{
								results.Add(new ScriptStringEntry
								{
									fileID = currentFileID,
									gameObjectName = goName,
									scriptName = scriptName,
									fieldName = field.Key,
									arrayIndex = arrayIdx,
									text = text
								});
							}
							arrayIdx++;
						}
						break;
					}
				}

				// --- 新增：嵌套 [Serializable] 结构体（数组或单个）---
				if (nestedTargetFields != null)
				{
					foreach (var nf in nestedTargetFields)
					{
						string outerKey = nf.Key + ":";
						if (!trimmed.StartsWith(outerKey)) continue;

						if (goName == null) goName = ResolveGoName(currentFileID, compToGo, goNames);
						string scriptName = _scriptNames.ContainsKey(matchedGuid) ? _scriptNames[matchedGuid] : "Unknown";
						List<NestedStringFieldInfo> innerDefs = nf.Value;

						int baseIndent = lineIndent;

						// 判断是结构体数组还是单个结构体：看下一行是否以 "- " 开头且缩进 == baseIndent
						bool isArray = false;
						if (i + 1 < lines.Length)
						{
							string peekRaw = lines[i + 1];
							string peekTrimmed = peekRaw.TrimStart();
							int peekIndent = peekRaw.Length - peekTrimmed.Length;
							isArray = (peekTrimmed.StartsWith("- ") && peekIndent == baseIndent);
						}

						int structIdx = isArray ? -1 : 0;

						while (i + 1 < lines.Length)
						{
							string nextRaw = lines[i + 1];
							string nextTrimmed = nextRaw.TrimStart();
							int nextIndent = nextRaw.Length - nextTrimmed.Length;

							if (nextIndent < baseIndent) break;

							if (isArray && nextTrimmed.StartsWith("- ") && nextIndent == baseIndent)
							{
								structIdx++;
								i++;
								string innerContent = nextTrimmed.Substring(2);
								ParseNestedInnerField(innerContent, innerDefs, lines, ref i,
									baseIndent + 2, structIdx, currentFileID, goName, scriptName, nf.Key, results);
							}
							else if (nextIndent > baseIndent && !nextTrimmed.StartsWith("- "))
							{
								i++;
								ParseNestedInnerField(nextTrimmed, innerDefs, lines, ref i,
									baseIndent + 2, structIdx, currentFileID, goName, scriptName, nf.Key, results);
							}
							else if (nextIndent > baseIndent)
							{
								i++;
							}
							else
							{
								break;
							}
						}
						break;
					}
				}
			}

			return results;
		}

		private static string ResolveGoName(string fileID,
			Dictionary<string, string> compToGo, Dictionary<string, string> goNames)
		{
			if (compToGo.TryGetValue(fileID, out string goFileID))
			{
				if (goNames.TryGetValue(goFileID, out string name))
					return name;
			}
			return "Unknown";
		}

		/// <summary>
		/// 解析嵌套结构体内的一行内部字段，提取 string / string[] 值
		/// </summary>
		private void ParseNestedInnerField(string content, List<NestedStringFieldInfo> innerDefs,
			string[] lines, ref int i, int innerIndent, int structIdx,
			string fileID, string goName, string scriptName, string outerFieldName,
			List<ScriptStringEntry> results)
		{
			foreach (var def in innerDefs)
			{
				string key = def.innerFieldName + ":";
				if (!content.StartsWith(key)) continue;

				string fieldPath = $"{outerFieldName}[{structIdx}].{def.innerFieldName}";

				if (def.isArray)
				{
					// string[] — 读取后续 "- " 行（含多行续行）
					int arrayIdx = 0;
					while (i + 1 < lines.Length)
					{
						string nextRaw = lines[i + 1];
						string nextTrimmed = nextRaw.TrimStart();
						int nextInd = nextRaw.Length - nextTrimmed.Length;
						if (nextInd < innerIndent || !nextTrimmed.StartsWith("- ")) break;
						i++;

						string val = ReadYamlArrayItemFull(lines, ref i, nextTrimmed.Substring(2));
						string text = UnescapeYamlValue(val);
						if (!string.IsNullOrWhiteSpace(text))
						{
							results.Add(new ScriptStringEntry
							{
								fileID = fileID,
								gameObjectName = goName,
								scriptName = scriptName,
								fieldName = fieldPath,
								arrayIndex = arrayIdx,
								text = text
							});
						}
						arrayIdx++;
					}
				}
				else
				{
					// 单个 string — 冒号后面的值（含多行续行）
					string afterColon = content.Substring(key.Length).TrimStart();
					string fullVal = ReadYamlScalarFull(lines, ref i, afterColon);
					string text = UnescapeYamlValue(fullVal);
					if (!string.IsNullOrWhiteSpace(text))
					{
						results.Add(new ScriptStringEntry
						{
							fileID = fileID,
							gameObjectName = goName,
							scriptName = scriptName,
							fieldName = fieldPath,
							arrayIndex = -1,
							text = text
						});
					}
				}
				return;
			}
		}

		private void ExportScriptStringArrays()
		{
			_scriptStringFields = null; // force rebuild
			BuildScriptStringFieldCache();

			if (_scriptStringFields.Count == 0 && _scriptNestedFields.Count == 0)
			{
				logMessage = "未找到任何含 public string[]/List<string> 字段的脚本。";
				return;
			}

			string[] files = GetFilesByScope();
			if (files.Length == 0)
			{
				logMessage = "未找到任何文件。";
				return;
			}

			int totalCount = 0;

			for (int i = 0; i < files.Length; i++)
			{
				string filePath = files[i];
				string fileName = Path.GetFileNameWithoutExtension(filePath);
				EditorUtility.DisplayProgressBar("导出脚本字符串", fileName, (float)i / files.Length);

				List<ScriptStringEntry> entries = ParseScriptStringArrays(filePath);
				if (entries.Count == 0) continue;

				string subDir = Path.Combine(exportPath, "ScriptStrings", GetSubDir(filePath));
				if (!Directory.Exists(subDir))
					Directory.CreateDirectory(subDir);

				string txtPath = Path.Combine(subDir, fileName + ".txt");
				using (StreamWriter writer = new StreamWriter(txtPath, false, Encoding.UTF8))
				{
					foreach (ScriptStringEntry entry in entries)
					{
						string escapedText = entry.text.Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\r", "");
						// Format: fileID|ScriptName.fieldName[index]|GameObjectName|text
						// 嵌套单个string时 arrayIndex == -1，不加 [index] 后缀
						string fieldRef = entry.arrayIndex >= 0
							? $"{entry.scriptName}.{entry.fieldName}[{entry.arrayIndex}]"
							: $"{entry.scriptName}.{entry.fieldName}";
						writer.WriteLine($"{entry.fileID}|{fieldRef}|{entry.gameObjectName}|{escapedText}");
					}
				}

				totalCount += entries.Count;
			}

			EditorUtility.ClearProgressBar();
			AssetDatabase.Refresh();
			logMessage = $"脚本字符串导出完成！共扫描 {files.Length} 个文件，导出 {totalCount} 条字符串到 {exportPath}/ScriptStrings\n" +
			             $"已识别 {_scriptStringFields.Count} 个含字符串数组的脚本类，{_scriptNestedFields.Count} 个含嵌套结构体字符串的脚本类";
			Debug.Log(logMessage);
		}

		private void ImportScriptStringArrays()
		{
			BuildScriptStringFieldCache();

			List<string> dirList = new List<string>();
			if (scanScope != ScanScope.PrefabsOnly)
				dirList.Add(Path.Combine(exportPath, "ScriptStrings", "Scenes"));
			if (scanScope != ScanScope.ScenesOnly)
				dirList.Add(Path.Combine(exportPath, "ScriptStrings", "Prefabs"));

			List<string> allTxtFiles = new List<string>();
			foreach (string dir in dirList)
			{
				if (Directory.Exists(dir))
					allTxtFiles.AddRange(Directory.GetFiles(dir, "*.txt"));
			}

			if (allTxtFiles.Count == 0)
			{
				logMessage = "未找到 ScriptStrings 目录下的 txt 文件，请先勾选「包含脚本字符串数组」后执行步骤①导出。";
				return;
			}

			int totalReplaced = 0;

			for (int fi = 0; fi < allTxtFiles.Count; fi++)
			{
				string txtPath = allTxtFiles[fi];
				string fileName = Path.GetFileNameWithoutExtension(txtPath);
				string filePath = FindFileByName(fileName);
				if (filePath == null)
				{
					Debug.LogWarning($"找不到对应文件: {fileName}");
					continue;
				}

				EditorUtility.DisplayProgressBar("导入脚本字符串", fileName, (float)fi / allTxtFiles.Count);

				// Parse txt: fileID|ScriptName.fieldPath[index]|goName|text
				// 支持平铺: "quotes[3]" 和嵌套: "dialogue[0].line[2]" / "dialogue[0].eventName"
				var importMap = new Dictionary<string, Dictionary<string, Dictionary<int, string>>>();
				// importMap[fileID][fieldPath][index] = text

				string[] txtLines = File.ReadAllLines(txtPath, Encoding.UTF8);
				foreach (string line in txtLines)
				{
					if (string.IsNullOrWhiteSpace(line)) continue;
					string[] parts = SplitPipe(line, 4);
					if (parts == null) continue;

					string fileID = parts[0];
					string fieldRef = parts[1]; // e.g. "Script.quotes[3]" or "Script.dialogue[0].line[2]" or "Script.dialogue[0].eventName"
					string text = UnescapeText(parts[3]);

					int dotIdx = fieldRef.IndexOf('.');
					if (dotIdx < 0) continue;
					string fieldPart = fieldRef.Substring(dotIdx + 1);

					string fieldPath;
					int idx;

					// 查找最后一个 '[' 来提取 index
					int lastBracket = fieldPart.LastIndexOf('[');
					if (lastBracket >= 0)
					{
						string idxStr = fieldPart.Substring(lastBracket + 1).TrimEnd(']');
						if (!int.TryParse(idxStr, out idx)) continue;
						fieldPath = fieldPart.Substring(0, lastBracket);
					}
					else
					{
						// 嵌套单个 string，无 [index] 后缀: "dialogue[0].eventName"
						fieldPath = fieldPart;
						idx = -1;
					}

					if (!importMap.ContainsKey(fileID))
						importMap[fileID] = new Dictionary<string, Dictionary<int, string>>();
					if (!importMap[fileID].ContainsKey(fieldPath))
						importMap[fileID][fieldPath] = new Dictionary<int, string>();

					importMap[fileID][fieldPath][idx] = text;
				}

				int replaced = ReplaceScriptStringArrays(filePath, importMap);
				totalReplaced += replaced;
			}

			EditorUtility.ClearProgressBar();
			AssetDatabase.Refresh();
			logMessage = $"脚本字符串导入完成！共替换 {totalReplaced} 条字符串。";
			Debug.Log(logMessage);
		}

		/// <summary>
		/// 将翻译后的字符串写回场景/预制体 YAML
		/// </summary>
		private int ReplaceScriptStringArrays(string filePath,
			Dictionary<string, Dictionary<string, Dictionary<int, string>>> importMap)
		{
			string[] sceneLines = File.ReadAllLines(filePath, Encoding.UTF8);
			int replaced = 0;
			string currentFileID = null;
			string matchedGuid = null;
			Dictionary<string, bool> targetFields = null;
			Dictionary<string, List<NestedStringFieldInfo>> nestedTargetFields = null;

			for (int i = 0; i < sceneLines.Length; i++)
			{
				string line = sceneLines[i];

				if (line.StartsWith("--- !u!114 &"))
				{
					currentFileID = line.Substring("--- !u!114 &".Length);
					matchedGuid = null;
					targetFields = null;
					nestedTargetFields = null;
				}
				else if (line.StartsWith("--- "))
				{
					currentFileID = null;
					matchedGuid = null;
					targetFields = null;
					nestedTargetFields = null;
				}

				if (currentFileID == null) continue;
				if (!importMap.ContainsKey(currentFileID)) continue;

				if (matchedGuid == null)
				{
					string guid = MatchScriptGuid(line);
					if (guid != null)
					{
						matchedGuid = guid;
						_scriptStringFields.TryGetValue(guid, out targetFields);
						_scriptNestedFields.TryGetValue(guid, out nestedTargetFields);
					}
					continue;
				}

				if (targetFields == null && nestedTargetFields == null) continue;

				string trimmed = line.TrimStart();
				int lineIndent = line.Length - trimmed.Length;

				// --- 平铺字段 ---
				if (targetFields != null)
				{
					foreach (var field in targetFields)
					{
						string fieldKey = field.Key + ":";
						if (!trimmed.StartsWith(fieldKey)) continue;

						if (!importMap[currentFileID].ContainsKey(field.Key)) break;
						var indexMap = importMap[currentFileID][field.Key];

						int arrayIdx = 0;
						while (i + 1 < sceneLines.Length)
						{
							string nextLine = sceneLines[i + 1];
							if (!nextLine.TrimStart().StartsWith("- "))
							{
								if (string.IsNullOrWhiteSpace(nextLine))
								{
									int peek = i + 2;
									while (peek < sceneLines.Length && string.IsNullOrWhiteSpace(sceneLines[peek])) peek++;
									if (peek < sceneLines.Length && sceneLines[peek].TrimStart().StartsWith("- "))
									{
										i++;
										continue;
									}
								}
								break;
							}
							i++;

							if (indexMap.TryGetValue(arrayIdx, out string newText))
							{
								string indent = nextLine.Substring(0, nextLine.Length - nextLine.TrimStart().Length);
								sceneLines[i] = indent + "- " + EscapeYamlValue(newText);
								NullifyYamlContinuationLines(sceneLines, ref i);
								replaced++;
							}
							else
							{
								// 没有翻译也要跳过续行
								SkipYamlContinuationLines(sceneLines, ref i);
							}
							arrayIdx++;
						}
						break;
					}
				}

				// --- 嵌套结构体字段（数组或单个）---
				if (nestedTargetFields != null)
				{
					foreach (var nf in nestedTargetFields)
					{
						string outerKey = nf.Key + ":";
						if (!trimmed.StartsWith(outerKey)) continue;

						List<NestedStringFieldInfo> innerDefs = nf.Value;
						int baseIndent = lineIndent;

						bool isArray = false;
						if (i + 1 < sceneLines.Length)
						{
							string peekRaw = sceneLines[i + 1];
							string peekTrimmed = peekRaw.TrimStart();
							int peekIndent = peekRaw.Length - peekTrimmed.Length;
							isArray = (peekTrimmed.StartsWith("- ") && peekIndent == baseIndent);
						}

						int structIdx = isArray ? -1 : 0;

						while (i + 1 < sceneLines.Length)
						{
							string nextRaw = sceneLines[i + 1];
							string nextTrimmed = nextRaw.TrimStart();
							int nextIndent = nextRaw.Length - nextTrimmed.Length;

							if (nextIndent < baseIndent) break;

							if (isArray && nextTrimmed.StartsWith("- ") && nextIndent == baseIndent)
							{
								structIdx++;
								i++;
								string innerContent = nextTrimmed.Substring(2);
								replaced += ReplaceNestedInnerField(innerContent, innerDefs, sceneLines, ref i,
									baseIndent + 2, structIdx, currentFileID, nf.Key, importMap);
							}
							else if (nextIndent > baseIndent && !nextTrimmed.StartsWith("- "))
							{
								i++;
								replaced += ReplaceNestedInnerField(nextTrimmed, innerDefs, sceneLines, ref i,
									baseIndent + 2, structIdx, currentFileID, nf.Key, importMap);
							}
							else if (nextIndent > baseIndent)
							{
								i++;
							}
							else
							{
								break;
							}
						}
						break;
					}
				}
			}

			if (replaced > 0)
			{
				// 过滤掉被 NullifyYamlContinuationLines 标记为 null 的行
				var filteredLines = new List<string>(sceneLines.Length);
				for (int k = 0; k < sceneLines.Length; k++)
				{
					if (sceneLines[k] != null)
						filteredLines.Add(sceneLines[k]);
				}
				File.WriteAllLines(filePath, filteredLines.ToArray(), Encoding.UTF8);
			}

			return replaced;
		}

		/// <summary>
		/// 替换嵌套结构体内的一行内部字段（与 ParseNestedInnerField 对称）
		/// </summary>
		private static int ReplaceNestedInnerField(string content, List<NestedStringFieldInfo> innerDefs,
			string[] sceneLines, ref int i, int innerIndent, int structIdx,
			string fileID, string outerFieldName,
			Dictionary<string, Dictionary<string, Dictionary<int, string>>> importMap)
		{
			int replaced = 0;
			foreach (var def in innerDefs)
			{
				string key = def.innerFieldName + ":";
				if (!content.StartsWith(key)) continue;

				string fieldPath = $"{outerFieldName}[{structIdx}].{def.innerFieldName}";

				if (!importMap.ContainsKey(fileID)) break;
				if (!importMap[fileID].TryGetValue(fieldPath, out var indexMap)) break;

				if (def.isArray)
				{
					int arrayIdx = 0;
					while (i + 1 < sceneLines.Length)
					{
						string nextRaw = sceneLines[i + 1];
						string nextTrimmed = nextRaw.TrimStart();
						int nextInd = nextRaw.Length - nextTrimmed.Length;
						if (nextInd < innerIndent || !nextTrimmed.StartsWith("- ")) break;
						i++;

						if (indexMap.TryGetValue(arrayIdx, out string newText))
						{
							string indent = nextRaw.Substring(0, nextInd);
							sceneLines[i] = indent + "- " + EscapeYamlValue(newText);
							NullifyYamlContinuationLines(sceneLines, ref i);
							replaced++;
						}
						else
						{
							SkipYamlContinuationLines(sceneLines, ref i);
						}
						arrayIdx++;
					}
				}
				else
				{
					// 单个 string: 替换当前行冒号后的值
					if (indexMap.TryGetValue(-1, out string newText))
					{
						string lineRaw = sceneLines[i];
						string lineIndent = lineRaw.Substring(0, lineRaw.Length - lineRaw.TrimStart().Length);
						// 结构体第一行可能带 "- " 前缀
						bool hasListPrefix = lineRaw.TrimStart().StartsWith("- ");
						string prefix = hasListPrefix ? "- " : "";
						sceneLines[i] = lineIndent + prefix + def.innerFieldName + ": " + EscapeYamlValue(newText);
						NullifyYamlContinuationLines(sceneLines, ref i);
						replaced++;
					}
					else
					{
						SkipYamlContinuationLines(sceneLines, ref i);
					}
				}
				break;
			}
			return replaced;
		}

		/// <summary>
		/// 按 | 分割为 n 段（最后一段可包含 |）
		/// </summary>
		private static string[] SplitPipe(string line, int count)
		{
			string[] result = new string[count];
			int start = 0;
			for (int p = 0; p < count - 1; p++)
			{
				int idx = line.IndexOf('|', start);
				if (idx < 0) return null;
				result[p] = line.Substring(start, idx - start);
				start = idx + 1;
			}
			result[count - 1] = line.Substring(start);
			return result;
		}

		#endregion

		#region Helpers

		private static string[] GetAllSceneFiles()
		{
			string scenesFolder = "Assets/Scenes";
			if (!Directory.Exists(scenesFolder))
				return new string[0];

			return Directory.GetFiles(scenesFolder, "*.unity", SearchOption.AllDirectories);
		}

		private static string[] GetAllPrefabFiles()
		{
			string prefabFolder = "Assets";
			if (!Directory.Exists(prefabFolder))
				return new string[0];

			return Directory.GetFiles(prefabFolder, "*.prefab", SearchOption.AllDirectories);
		}

		private string[] GetFilesByScope()
		{
			switch (scanScope)
			{
				case ScanScope.ScenesOnly:
					return GetAllSceneFiles();
				case ScanScope.PrefabsOnly:
					return GetAllPrefabFiles();
				default:
					var list = new List<string>();
					list.AddRange(GetAllSceneFiles());
					list.AddRange(GetAllPrefabFiles());
					return list.ToArray();
			}
		}

		private string FindFileByName(string fileName)
		{
			string[] files = GetFilesByScope();
			foreach (string f in files)
			{
				if (Path.GetFileNameWithoutExtension(f) == fileName)
					return f;
			}
			return null;
		}

		private struct TextEntry
		{
			public string fileID;
			public string gameObjectName;
			public string text;
			public TextCompType compType;
		}

		private List<TextEntry> ParseSceneTexts(string scenePath)
		{
			List<TextEntry> results = new List<TextEntry>();
			string[] lines = File.ReadAllLines(scenePath, Encoding.UTF8);

			// First pass: build fileID -> GameObject name map and component -> GO map
			Dictionary<string, string> goNames = new Dictionary<string, string>();
			Dictionary<string, string> compToGo = new Dictionary<string, string>();

			string currentFileID = null;
			string currentTag = null;

			for (int i = 0; i < lines.Length; i++)
			{
				string line = lines[i];

				if (line.StartsWith("--- !u!"))
				{
					int ampIdx = line.IndexOf('&');
					if (ampIdx >= 0)
					{
						currentFileID = line.Substring(ampIdx + 1);
						int tagStart = "--- !u!".Length;
						int tagEnd = line.IndexOf(' ', tagStart);
						if (tagEnd > tagStart)
							currentTag = line.Substring(tagStart, tagEnd - tagStart);
					}
					continue;
				}

				if (currentFileID == null) continue;

				if (currentTag == "1" && line.TrimStart().StartsWith("m_Name:"))
				{
					string name = line.Substring(line.IndexOf(':') + 1).Trim();
					goNames[currentFileID] = name;
				}

				if (currentTag == "114" && line.TrimStart().StartsWith("m_GameObject:"))
				{
					Match m = Regex.Match(line, @"fileID:\s*(\d+)");
					if (m.Success)
						compToGo[currentFileID] = m.Groups[1].Value;
				}
			}

			// Second pass: extract text from Text and TMP components
			currentFileID = null;
			TextCompType compType = TextCompType.None;

			for (int i = 0; i < lines.Length; i++)
			{
				string line = lines[i];

				if (line.StartsWith("--- !u!114 &"))
				{
					currentFileID = line.Substring("--- !u!114 &".Length);
					compType = TextCompType.None;
				}
				else if (line.StartsWith("--- "))
				{
					currentFileID = null;
					compType = TextCompType.None;
				}

				if (currentFileID != null)
				{
					TextCompType detected = GetTextCompType(line);
					if (detected != TextCompType.None)
						compType = detected;
				}

				if (compType == TextCompType.None || currentFileID == null) continue;

				// m_Text for UI Text, m_text for TMP
				string textFieldPrefix = compType == TextCompType.UIText ? "m_Text:" : "m_text:";
				if (!line.TrimStart().StartsWith(textFieldPrefix)) continue;

				string rawText = line.Substring(line.IndexOf(textFieldPrefix) + textFieldPrefix.Length).TrimStart();

				// Handle multi-line YAML quoted strings
				if (rawText.StartsWith("\"") && !rawText.EndsWith("\""))
				{
					while (i + 1 < lines.Length)
					{
						i++;
						rawText += "\n" + lines[i];
						if (lines[i].TrimEnd().EndsWith("\""))
							break;
					}
				}

				string text = UnescapeYamlValue(rawText);

				string goName = "Unknown";
				if (compToGo.TryGetValue(currentFileID, out string goFileID))
				{
					if (goNames.TryGetValue(goFileID, out string name))
						goName = name;
				}

				results.Add(new TextEntry
				{
					fileID = currentFileID,
					gameObjectName = goName,
					text = text,
					compType = compType
				});

				compType = TextCompType.None;
			}

			return results;
		}

		private static string UnescapeYamlValue(string value)
		{
			if (value.Length == 0) return "";

			if (value.StartsWith("\"") && value.EndsWith("\""))
			{
				value = value.Substring(1, value.Length - 2);
				value = Regex.Replace(value, @"\n\s+", " ");
				value = value.Replace("\\n", "\n");
				value = value.Replace("\\t", "\t");
				value = value.Replace("\\\"", "\"");
				value = Regex.Replace(value, @"\\x([0-9a-fA-F]{2})", m =>
					((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString());
				value = value.Replace("\\\\", "\\");
				return value;
			}

			if (value.StartsWith("'") && value.EndsWith("'"))
			{
				value = value.Substring(1, value.Length - 2);
				value = value.Replace("''", "'");
				return value;
			}

			return value;
		}

		/// <summary>
		/// 读取 YAML "- value" 的完整值（含多行续行），返回拼接后的原始文本。
		/// 调用时 i 指向 "- xxx" 行，返回后 i 指向该值的最后一行。
		/// </summary>
		private static string ReadYamlArrayItemFull(string[] lines, ref int i, string firstLineValue)
		{
			int itemIndent = lines[i].Length - lines[i].TrimStart().Length;
			System.Text.StringBuilder sb = new System.Text.StringBuilder(firstLineValue);
			while (i + 1 < lines.Length)
			{
				string nextRaw = lines[i + 1];
				// 空行：检查后面是否还有续行（缩进 > itemIndent）
				if (string.IsNullOrWhiteSpace(nextRaw))
				{
					// 向前探测：跳过连续空行，看后面第一个非空行的缩进
					int peek = i + 2;
					while (peek < lines.Length && string.IsNullOrWhiteSpace(lines[peek])) peek++;
					if (peek < lines.Length)
					{
						string peekLine = lines[peek];
						int peekIndent = peekLine.Length - peekLine.TrimStart().Length;
						if (peekIndent > itemIndent)
						{
							// 空行是多行值的一部分，吞掉
							i++;
							sb.Append("\n");
							continue;
						}
					}
					break;
				}
				int nextIndent = nextRaw.Length - nextRaw.TrimStart().Length;
				if (nextIndent > itemIndent)
				{
					i++;
					sb.Append("\n").Append(nextRaw.TrimStart());
				}
				else break;
			}
			return sb.ToString();
		}

		/// <summary>
		/// 读取 YAML "fieldName: value" 的完整值（含多行续行）。
		/// 调用时 i 指向 "fieldName: xxx" 行，返回后 i 指向该值的最后一行。
		/// </summary>
		private static string ReadYamlScalarFull(string[] lines, ref int i, string firstLineValue)
		{
			int itemIndent = lines[i].Length - lines[i].TrimStart().Length;
			System.Text.StringBuilder sb = new System.Text.StringBuilder(firstLineValue);
			while (i + 1 < lines.Length)
			{
				string nextRaw = lines[i + 1];
				if (string.IsNullOrWhiteSpace(nextRaw))
				{
					int peek = i + 2;
					while (peek < lines.Length && string.IsNullOrWhiteSpace(lines[peek])) peek++;
					if (peek < lines.Length)
					{
						string peekLine = lines[peek];
						int peekIndent = peekLine.Length - peekLine.TrimStart().Length;
						if (peekIndent > itemIndent)
						{
							i++;
							sb.Append("\n");
							continue;
						}
					}
					break;
				}
				int nextIndent = nextRaw.Length - nextRaw.TrimStart().Length;
				if (nextIndent > itemIndent)
				{
					i++;
					sb.Append("\n").Append(nextRaw.TrimStart());
				}
				else break;
			}
			return sb.ToString();
		}

		/// <summary>
		/// 跳过并标记（置 null）当前 "- " 行之后的多行续行，用于替换时清除旧续行。
		/// </summary>
		private static void NullifyYamlContinuationLines(string[] lines, ref int i)
		{
			int itemIndent = lines[i].Length - lines[i].TrimStart().Length;
			while (i + 1 < lines.Length)
			{
				string nextRaw = lines[i + 1];
				if (string.IsNullOrWhiteSpace(nextRaw))
				{
					int peek = i + 2;
					while (peek < lines.Length && string.IsNullOrWhiteSpace(lines[peek])) peek++;
					if (peek < lines.Length && lines[peek] != null)
					{
						int peekIndent = lines[peek].Length - lines[peek].TrimStart().Length;
						if (peekIndent > itemIndent)
						{
							i++;
							lines[i] = null;
							continue;
						}
					}
					break;
				}
				int nextIndent = nextRaw.Length - nextRaw.TrimStart().Length;
				if (nextIndent > itemIndent)
				{
					i++;
					lines[i] = null;
				}
				else break;
			}
		}

		/// <summary>
		/// 跳过当前行之后的多行续行（不修改内容），仅移动索引。
		/// </summary>
		private static void SkipYamlContinuationLines(string[] lines, ref int i)
		{
			int itemIndent = lines[i].Length - lines[i].TrimStart().Length;
			while (i + 1 < lines.Length)
			{
				string nextRaw = lines[i + 1];
				if (string.IsNullOrWhiteSpace(nextRaw))
				{
					int peek = i + 2;
					while (peek < lines.Length && string.IsNullOrWhiteSpace(lines[peek])) peek++;
					if (peek < lines.Length)
					{
						int peekIndent = lines[peek].Length - lines[peek].TrimStart().Length;
						if (peekIndent > itemIndent)
						{
							i++;
							continue;
						}
					}
					break;
				}
				int nextIndent = nextRaw.Length - nextRaw.TrimStart().Length;
				if (nextIndent > itemIndent)
					i++;
				else break;
			}
		}

		private static string EscapeYamlValue(string text)
		{
			if (string.IsNullOrEmpty(text)) return "";

			if (text.Contains("\n") || text.Contains("\"") || text.Contains("\\") ||
			    text.Contains(":") || text.Contains("#"))
			{
				string escaped = text
					.Replace("\\", "\\\\")
					.Replace("\"", "\\\"")
					.Replace("\n", "\\n")
					.Replace("\t", "\\t")
					.Replace("\r", "");
				return "\"" + escaped + "\"";
			}

			return text;
		}

		#endregion
	}
}
