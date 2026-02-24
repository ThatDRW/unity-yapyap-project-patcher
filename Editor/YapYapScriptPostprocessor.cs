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

                if (!normalizedPath.EndsWith(".cs") || !normalizedPath.Contains("YAPYAP"))
                    continue;

                // 1 Delete brocken/unnecessary Files, Folders, and Demos
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
                    string safeContent = @"public class RecognizedPhrase {
    public const string ConfidenceKey = ""confidence"";
    public const string TextKey = ""text"";
    public string Text = """";
    public float Confidence;
    public RecognizedPhrase() { }
}";
                    File.WriteAllText(path, safeContent);
                    modifiedDatabase = true;
                    continue;
                }
                
                if (normalizedPath.EndsWith("RecognitionResult.cs"))
                {
                    string safeContent = @"public class RecognitionResult { 
    public RecognizedPhrase[] Phrases = new RecognizedPhrase[0];
    public string Partial = """";
    public string Text = """";
    public RecognitionResult(string json) {} 
}";
                    File.WriteAllText(path, safeContent);
                    modifiedDatabase = true;
                    continue;
                }

                if (normalizedPath.EndsWith("VoskSpeechToText.cs"))
                {
                    string safeContent = @"using System; using System.Collections.Generic; using UnityEngine;
public class VoskSpeechToText : MonoBehaviour {
    public string ModelPath = """";
    public MonoBehaviour VoiceProcessor;
    public int MaxAlternatives = 3;
    public bool LogResults;
    public List<string> KeyPhrases = new List<string>();
    public Action<RecognitionResult> OnTranscriptionResult;
    public Action<string> OnTranscriptionPartial;
    public Action<string> OnTranscriptionWord;
    public void StartVosk(List<string> grammer = null, string modelPath = null, int maxAlternatives = 3) {}
    public void StartRecording() {}
    public void StopRecording() {}
    public void UpdateGrammar(List<string> keyPhrases) {}
}";
                    File.WriteAllText(path, safeContent);
                    modifiedDatabase = true;
                    continue;
                }

                if (normalizedPath.EndsWith("UINetwork.cs"))
                {
                    string safeContent = @"using System; using UnityEngine;
namespace YAPYAP {
    public class UINetwork : MonoBehaviour {
        public void SetActions(Action onHost, Action onServer, Action<string> onClient) {}
        public void Initialise() {}
        public void RefreshUI() {}
        public void CloseSaveWindow() {}
        public void ShowPanel(bool show) {}
    }
}";
                    File.WriteAllText(path, safeContent);
                    modifiedDatabase = true;
                    continue;
                }

                string content = File.ReadAllText(path);
                string originalContent = content;

                // 3 Attempt to fix FMOD settings/configuration issues
                if (normalizedPath.EndsWith("RuntimeManager.cs"))
                {
                    // Add null checks for currentPlatform
                    content = content.Replace("currentPlatform = settings.FindCurrentPlatform();", "currentPlatform = settings?.FindCurrentPlatform();");
                    content = content.Replace("int sampleRate = currentPlatform.SampleRate;", "int sampleRate = currentPlatform != null ? currentPlatform.SampleRate : 48000;");
                    content = content.Replace("int softwareChannels = Math.Min(currentPlatform.RealChannelCount, 256);", "int softwareChannels = currentPlatform != null ? Math.Min(currentPlatform.RealChannelCount, 256) : 256;");
                    content = content.Replace("int virtualChannelCount = currentPlatform.VirtualChannelCount;", "int virtualChannelCount = currentPlatform != null ? currentPlatform.VirtualChannelCount : 1024;");
                    content = content.Replace("uint dSPBufferLength = (uint)currentPlatform.DSPBufferLength;", "uint dSPBufferLength = currentPlatform != null ? (uint)currentPlatform.DSPBufferLength : 0;");
                    content = content.Replace("int dSPBufferCount = currentPlatform.DSPBufferCount;", "int dSPBufferCount = currentPlatform != null ? currentPlatform.DSPBufferCount : 0;");
                    content = content.Replace("SPEAKERMODE speakerMode = currentPlatform.SpeakerMode;", "SPEAKERMODE speakerMode = currentPlatform != null ? currentPlatform.SpeakerMode : SPEAKERMODE.STEREO;");
                    content = content.Replace("OUTPUTTYPE output = currentPlatform.GetOutputType();", "OUTPUTTYPE output = currentPlatform != null ? currentPlatform.GetOutputType() : OUTPUTTYPE.AUTODETECT;");
                    content = content.Replace("currentPlatform.PreSystemCreate(CheckInitResult);", "if (currentPlatform != null) currentPlatform.PreSystemCreate(CheckInitResult);");
                    content = content.Replace("if (currentPlatform.IsLiveUpdateEnabled)", "if (currentPlatform != null && currentPlatform.IsLiveUpdateEnabled)");
                    content = content.Replace("currentPlatform.PreInitialize(studioSystem);", "if (currentPlatform != null) currentPlatform.PreInitialize(studioSystem);");
                    content = content.Replace("PlatformCallbackHandler callbackHandler = currentPlatform.CallbackHandler;", "PlatformCallbackHandler callbackHandler = currentPlatform?.CallbackHandler;");
                    content = content.Replace("currentPlatform.LoadPlugins(coreSystem, CheckInitResult);", "if (currentPlatform != null) currentPlatform.LoadPlugins(coreSystem, CheckInitResult);");
                    content = content.Replace("SetThreadAffinities(currentPlatform);", "if (currentPlatform != null) SetThreadAffinities(currentPlatform);");
                    content = content.Replace("isOverlayEnabled = currentPlatform.IsOverlayEnabled;", "isOverlayEnabled = currentPlatform != null && currentPlatform.IsOverlayEnabled;");
                    content = content.Replace("string text = Instance.currentPlatform.GetBankFolder();", "string text = Instance.currentPlatform != null ? Instance.currentPlatform.GetBankFolder() : Application.streamingAssetsPath;");
                    content = content.Replace("float num = currentPlatform.OverlayFontSize * 20;", "float num = currentPlatform != null ? currentPlatform.OverlayFontSize * 20 : 200;");
                    content = content.Replace("float num2 = currentPlatform.OverlayFontSize * 7;", "float num2 = currentPlatform != null ? currentPlatform.OverlayFontSize * 7 : 70;");
                    content = content.Replace("switch (currentPlatform.OverlayRect)", "switch (currentPlatform != null ? currentPlatform.OverlayRect : ScreenPosition.TopLeft)");
                    content = content.Replace("if (currentPlatform.OverlayRect != ScreenPosition.VR)", "if (currentPlatform == null || currentPlatform.OverlayRect != ScreenPosition.VR)");
                    content = content.Replace("style.fontSize = currentPlatform.OverlayFontSize;", "style.fontSize = currentPlatform != null ? currentPlatform.OverlayFontSize : 14;");
                    
                    // Fix a specific CodecChannelCount lookup, which the compiler sometimes seems to break?
                    string brokenChannelCheck = @"CodecChannelCount codecChannelCount = currentPlatform.CodecChannels.Find\(\(CodecChannelCount x\) => x.format == format\);";
                    string fixedChannelCheck = @"if (currentPlatform == null || currentPlatform.CodecChannels == null) return 0; CodecChannelCount codecChannelCount = currentPlatform.CodecChannels.Find((CodecChannelCount x) => x.format == format);";
                    content = Regex.Replace(content, brokenChannelCheck, fixedChannelCheck);

                    // Add null checks for Settings.Instance to prevent NullRefExceptions
                    content = content.Replace("Settings.Instance.EncryptionKey", "(Settings.Instance != null ? Settings.Instance.EncryptionKey : null)");
                    content = content.Replace("Settings.Instance.MasterBanks", "(Settings.Instance != null && Settings.Instance.MasterBanks != null ? Settings.Instance.MasterBanks : new System.Collections.Generic.List<string>())");
                    content = content.Replace("settings.EnableErrorCallback", "(settings != null && settings.EnableErrorCallback)");
                    content = content.Replace("settings.EncryptionKey", "(settings != null ? settings.EncryptionKey : null)");
                    content = content.Replace("settings.EnableMemoryTracking", "(settings != null && settings.EnableMemoryTracking)");
                    content = content.Replace("fmodSettings.ImportType", "(fmodSettings != null ? fmodSettings.ImportType : ImportType.StreamingAssets)");
                    content = content.Replace("fmodSettings.AutomaticSampleLoading", "(fmodSettings != null && fmodSettings.AutomaticSampleLoading)");
                    content = content.Replace("fmodSettings.BankLoadType", "(fmodSettings != null ? fmodSettings.BankLoadType : BankLoadType.All)");
                    content = content.Replace("fmodSettings.MasterBanks", "(fmodSettings != null && fmodSettings.MasterBanks != null ? fmodSettings.MasterBanks : new System.Collections.Generic.List<string>())");
                    content = content.Replace("fmodSettings.Banks", "(fmodSettings != null && fmodSettings.Banks != null ? fmodSettings.Banks : new System.Collections.Generic.List<string>())");
                    content = content.Replace("fmodSettings.BanksToLoad", "(fmodSettings != null && fmodSettings.BanksToLoad != null ? fmodSettings.BanksToLoad : new System.Collections.Generic.List<string>())");
                }
				
                // Small FMOD fix
                if (normalizedPath.EndsWith("RemoteStatistics.cs"))
                {
                    content = content.Replace("protected void UserCode_TargetRpcSync__Stats", "internal void UserCode_TargetRpcSync__Stats");
                }

                // Comment out BeautifySettings refs
                if (normalizedPath.EndsWith("LUTBlending.cs"))
                {
                    content = Regex.Replace(content, @"^.*BeautifySettings.*$", "// omitted BeautifySettings", RegexOptions.Multiline);
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
                    
                    // TODO: Handle OccaSoftware references properly
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
                if (!AssetDatabase.IsValidFolder(targetPath))
                    AssetDatabase.CreateFolder("Assets/YAPYAP/Game", "Scenes");
                
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