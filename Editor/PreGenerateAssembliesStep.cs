using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Nomnom.UnityProjectPatcher.Editor.Steps;
using UnityEngine;

namespace YapYapModding.YapYapProjectPatcher.Editor
{
	public struct PreGenerateAssembliesStep : IPatcherStep
	{
		public UniTask<StepResult> Run()
		{
			var yapyapPath = Path.Combine(Application.dataPath, "YAPYAP");
			var pluginsPath = Path.Combine(yapyapPath, "Plugins");
			var scriptsFolder = Path.Combine(yapyapPath, "Game", "Scripts");

			// 1 Delete conflicting DLLs	// could (maybe) remove them from getting copied over but this works rn
			if (Directory.Exists(pluginsPath))
			{
				string[] dllsToDelete = {
					"Mirror.dll", "Mirror.Components.dll", "Mirror.Authenticators.dll", "Mirror.Transports.dll", "Mirror.Examples.dll",
					"Photon3Unity3D.dll", "PhotonChat.dll", "PhotonRealtime.dll", "PhotonUnityNetworking.dll", "PhotonUnityNetworking.Utilities.dll", "PhotonVoice.dll", "PhotonVoice.API.dll", "PhotonVoice.PUN.dll",
					"FMODUnity.dll", "FMODUnityResonance.dll"
				};

				foreach (var dll in Directory.GetFiles(pluginsPath, "*.dll", SearchOption.AllDirectories))
				{
					if (Array.IndexOf(dllsToDelete, Path.GetFileName(dll)) >= 0)
					{
						File.Delete(dll);
						string metaFile = dll + ".meta";
						if (File.Exists(metaFile)) File.Delete(metaFile);
					}
				}
			}

			// 2 Delete Brocken source Folders
			string[] brokenSourceFolders = {
				"kcp2k", "Telepathy", "SimpleWebTransport", "Mirror.BouncyCastle.Cryptography"
			};
			foreach (string folder in brokenSourceFolders)
			{
				string targetPath = Path.Combine(scriptsFolder, folder);
				if (Directory.Exists(targetPath)) Directory.Delete(targetPath, true);
				if (File.Exists(targetPath + ".meta")) File.Delete(targetPath + ".meta");
			}

			// 3. Create ASMDEF files manually
			// FMOD
			WriteAsmDef(scriptsFolder, "FMODUnity", new[] { "Unity.Timeline" });
			WriteAsmDef(scriptsFolder, "FMODUnityResonance", new[] { "FMODUnity", "Unity.Timeline" });

			// Mirror 
			WriteAsmDef(scriptsFolder, "Mirror", new string[0]);
			WriteAsmDef(scriptsFolder, "Mirror.Components", new[] { "Mirror" });
			WriteAsmDef(scriptsFolder, "Mirror.Authenticators", new[] { "Mirror" });
			WriteAsmDef(scriptsFolder, "Mirror.Transports", new[] { "Mirror" });

			// Photon
			WriteAsmDef(scriptsFolder, "Photon3Unity3D", new string[0]);
			WriteAsmDef(scriptsFolder, "PhotonChat", new[] { "Photon3Unity3D" });
			WriteAsmDef(scriptsFolder, "PhotonRealtime", new[] { "Photon3Unity3D" });
			WriteAsmDef(scriptsFolder, "PhotonUnityNetworking", new[] { "Photon3Unity3D", "PhotonRealtime" });
			WriteAsmDef(scriptsFolder, "PhotonUnityNetworking.Utilities", new[] { "Photon3Unity3D", "PhotonRealtime", "PhotonUnityNetworking" });
			WriteAsmDef(scriptsFolder, "PhotonVoice", new[] { "Photon3Unity3D", "PhotonRealtime", "PhotonVoice.API" });
			WriteAsmDef(scriptsFolder, "PhotonVoice.API", new[] { "Photon3Unity3D", "PhotonRealtime" });
			WriteAsmDef(scriptsFolder, "PhotonVoice.PUN", new[] { "Photon3Unity3D", "PhotonRealtime", "PhotonUnityNetworking", "PhotonVoice", "PhotonVoice.API" });

			return UniTask.FromResult(StepResult.Success);
			// return UniTask.FromResult(StepResult.Recompile); // <-- Maybe a Recompile would be better?
		}

		private void WriteAsmDef(string basePath, string assembly, string[] dependencies)
		{
			string folderPath = Path.Combine(basePath, assembly);
			Directory.CreateDirectory(folderPath); 

			string asmdefPath = Path.Combine(folderPath, assembly + ".asmdef");
			string depsArray = dependencies.Length > 0 ? "\"" + string.Join("\", \"", dependencies) + "\"" : "";
			string json = $@"{{ ""name"": ""{assembly}"", ""references"": [{depsArray}], ""autoReferenced"": true, ""allowUnsafeCode"": true }}";
			File.WriteAllText(asmdefPath, json);
		}
		public void OnComplete(bool failed) { }
	}
}