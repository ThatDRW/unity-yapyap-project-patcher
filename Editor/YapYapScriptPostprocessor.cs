using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;

namespace YapYapModding.YapYapProjectPatcher.Editor
{
	public class YapYapScriptPostprocessor : AssetPostprocessor
	{
		static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
		{
			bool modifiedDatabase = false;

			foreach (string path in importedAssets)
			{
				string normalizedPath = path.Replace("\\", "/");

				if (normalizedPath.EndsWith("FMODStudioSettings.asset") && !normalizedPath.Contains("/Resources/"))
				{
					string targetDir = "Assets/YAPYAP/Game/Resources";
					if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
					
					string newPath = targetDir + "/FMODStudioSettings.asset";
					if (!File.Exists(newPath)) 
					{
						File.Move(path, newPath);
						if (File.Exists(path + ".meta")) File.Move(path + ".meta", newPath + ".meta");
						modifiedDatabase = true;
					}
					continue;
				}

				if (!normalizedPath.EndsWith(".cs") || !normalizedPath.Contains("YAPYAP")) continue;

				// 1 Delete broken/unnecessary Files, Folders, and Demos
				if (normalizedPath.Contains("__JobReflectionRegistrationOutput__") ||
					normalizedPath.EndsWith("FogDensityMaskRotator.cs") ||
					normalizedPath.Contains("Mirror.Examples") ||
					normalizedPath.Contains("Beautify/Demos") || 
					normalizedPath.EndsWith("Demo.cs") || 
					normalizedPath.EndsWith("ToggleDoF.cs") ||
					normalizedPath.EndsWith("GeneratedNetworkCode.cs")) 
				{
					File.Delete(path);
					continue; 
				}

				// 2 Overwrite problematic files with Stubs
				if (normalizedPath.EndsWith("RecognizedPhrase.cs"))
				{
					File.WriteAllText(path, @"using UnityEngine;
[System.Serializable]
public class RecognizedPhrase {
	public string text;
	public float confidence;
	public string Text { get { return text; } }
	public float Confidence { get { return confidence; } }
}");
					modifiedDatabase = true; continue;
				}

				if (normalizedPath.EndsWith("RecognitionResult.cs"))
				{
					File.WriteAllText(path, @"using UnityEngine;
public class RecognitionResult { 
	public RecognizedPhrase[] Phrases = new RecognizedPhrase[0]; 
	public string Partial = """"; 
	public string Text = """"; 
	[System.Serializable] private class JsonData { public RecognizedPhrase[] result; public string partial; public string text; }
	public RecognitionResult(string json) { 
		try {
			JsonData data = JsonUtility.FromJson<JsonData>(json);
			if (data != null) { Phrases = data.result ?? new RecognizedPhrase[0]; Partial = data.partial ?? """"; Text = data.text ?? """"; }
		} catch {}
	} 
}");
					modifiedDatabase = true; continue;
				}

				if (normalizedPath.EndsWith("VoskSpeechToText.cs"))
				{
					File.WriteAllText(path, @"using System; using System.Collections.Generic; using UnityEngine;
public class VoskSpeechToText : MonoBehaviour { 
	public string ModelPath = """"; public MonoBehaviour VoiceProcessor; public int MaxAlternatives = 3; public bool LogResults; public List<string> KeyPhrases = new List<string>();
	public Action<RecognitionResult> OnTranscriptionResult; public Action<string> OnTranscriptionPartial; public Action<string> OnTranscriptionWord;
	public void StartVosk(List<string> g = null, string m = null, int a = 3) {} public void StartRecording() {} public void StopRecording() {} public void UpdateGrammar(List<string> k) {}
}");
					modifiedDatabase = true; continue;
				}

				string content = File.ReadAllText(path);
				string originalContent = content;

				// 3. PROPER EDITOR PATHING FOR STREAMING ASSETS
				if (normalizedPath.EndsWith("VoiceManager.cs")) 
				{
					string properPathing = @"
#if UNITY_EDITOR
			return System.IO.Path.Combine(Application.dataPath, ""YAPYAP/Game/StreamingAssets/Vosk/Localisation"");
#else
			return System.IO.Path.Combine(Application.streamingAssetsPath, ""Vosk"", ""Localisation"");
#endif
";
					content = Regex.Replace(content, @"return Path\.Combine\(Application\.streamingAssetsPath, ""Vosk"", ""Localisation""\);", properPathing);
				}

				// 4 Decomp/Syntax Fixes
				if (normalizedPath.EndsWith("UINetwork.cs")) 
				{
					content = content.Replace("static void SetActiveIfChanged(GameObject obj, bool flag)", "static void SetActiveIfChanged(GameObject obj, bool state)");
					content = content.Replace("if (obj.activeSelf != flag)", "if (obj.activeSelf != state)");
					content = content.Replace("obj.SetActive(flag);", "obj.SetActive(state);");
					
					content = content.Replace("static void SetButtonActiveIfChanged(Button btn, bool flag)", "static void SetButtonActiveIfChanged(Button btn, bool state)");
					content = content.Replace("if (btn.gameObject.activeSelf != flag)", "if (btn.gameObject.activeSelf != state)");
					content = content.Replace("btn.gameObject.SetActive(flag);", "btn.gameObject.SetActive(state);");
				}
				
				// Small FMOD fix
				if (normalizedPath.EndsWith("RemoteStatistics.cs")) 
				{
					content = content.Replace("protected void UserCode_TargetRpcSync__Stats", "internal void UserCode_TargetRpcSync__Stats");
				}

				// Comment out BeautifySettings refs
				if (normalizedPath.EndsWith("LUTBlending.cs")) 
				{
					content = content.Replace("BeautifySettings.settings.lut.Override(x: true);", "// BeautifySettings omitted");
					content = content.Replace("BeautifySettings.settings.lutIntensity.Override(x);", "// BeautifySettings omitted");
					content = content.Replace("BeautifySettings.settings.lutTexture.Override(rt);", "// BeautifySettings omitted");
				}
				
				// Replace WriteBlittable/ReadBlittable with standard methods
				if (normalizedPath.EndsWith("PredictedSyncDataReadWrite.cs")) 
				{
					content = content.Replace("writer.WriteBlittable(data)", "writer.Write(data)");
					content = content.Replace("reader.ReadBlittable<PredictedSyncData>()", "reader.Read<PredictedSyncData>()");
				}
				
				// Steam fixes for in-editor play
				if (normalizedPath.EndsWith("MenuController.cs"))
				{
					content = content.Replace("string title = service.CurrentTranslator.Translate", "if (service.CurrentTranslator == null) return;\nstring title = service.CurrentTranslator.Translate");
					content = content.Replace("_modal.Init()", "if (_modal != null) _modal.Init()");
					
					// Handle OccaSoftware references properly
					content = Regex.Replace(content, @"using OccaSoftware[^;]*;", "");
					content = Regex.Replace(content, @"^.*AutoExposureOverride.*$", "// omitted AutoExposureOverride", RegexOptions.Multiline);
					content = Regex.Replace(content, @"^.*_exposureOverride.*$", "// omitted _exposureOverride", RegexOptions.Multiline);
				}
				
				// Fix FMOD vector type
				if (normalizedPath.EndsWith("FmodResonanceAudio.cs")) 
				{
					content = content.Replace("out UnityEngine.Vector3 vel", "out FMOD.VECTOR vel");
					content = content.Replace("out var vel", "out FMOD.VECTOR vel");
				}

				// General fixes for multiple files
				if (content.Contains("struct Stats")) 
				{
					content = Regex.Replace(content, @"(?:public\s+|private\s+|protected\s+|internal\s+)*(readonly\s+)?(?:partial\s+)?struct\s+Stats\b", "public $1struct Stats");
				}

				if (normalizedPath.Contains("YAPYAP")) 
				{
					content = Regex.Replace(content, @"(?:public\s+|private\s+|protected\s+|internal\s+)*enum\s+State\b", "public enum State");
					content = Regex.Replace(content, @"(?:public\s+|private\s+|protected\s+|internal\s+)*enum\s+OrbState\b", "public enum OrbState");
				}
				
				// Adjust sync var method visibility and remove Mirrors Generated Net Code refs
				content = content.Replace("public override void SerializeSyncVars", "protected override void SerializeSyncVars");
				content = content.Replace("public override void DeserializeSyncVars", "protected override void DeserializeSyncVars");
				content = content.Replace("GeneratedNetworkCode.", "");
				
				// Suppress problematic read/write calls
				content = Regex.Replace(content, @"_Write_[A-Za-z0-9_]+\s*\([^)]*\);", "/* write omitted */");
				content = Regex.Replace(content, @"_Read_[A-Za-z0-9_]+\s*\([^)]*\)", "default");

				if (content != originalContent) 
				{
					File.WriteAllText(path, content);
					modifiedDatabase = true;
				}
			}

			// Move scene files from incorrectly nested folders to the correct location
			string sourcePath = "Assets/YAPYAP/Game/Unknown/Prototype/Scenes";
			string targetPath = "Assets/YAPYAP/Game/Scenes";
			if (AssetDatabase.IsValidFolder(sourcePath))
			{
				if (!AssetDatabase.IsValidFolder(targetPath)) AssetDatabase.CreateFolder("Assets/YAPYAP/Game", "Scenes");
				string[] files = Directory.GetFiles(sourcePath);
				foreach (string file in files)
				{
					if (file.EndsWith(".meta"))
						continue;
					string destination = Path.Combine(targetPath, Path.GetFileName(file));
					AssetDatabase.MoveAsset(file.Replace("\\", "/"), destination.Replace("\\", "/"));
				}
				FileUtil.DeleteFileOrDirectory("Assets/YAPYAP/Game/Unknown");
				modifiedDatabase = true;
			}

			if (modifiedDatabase) 
			{
				AssetDatabase.Refresh();
			}
		}
	}
}