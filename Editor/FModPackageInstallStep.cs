/*using System.IO;
using Cysharp.Threading.Tasks;
using Nomnom.UnityProjectPatcher.Editor.Steps;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Jettcodey.YapYapProjectPatcher.Editor
{
	public struct FModPackageInstallStep : IPatcherStep
	{
		public async UniTask<StepResult> Run()
		{
			// Verify if FMOD is already in the project
			string fmodPath = Path.Combine(Application.dataPath, "Plugins", "FMOD");
			if (Directory.Exists(fmodPath))
			{
				return StepResult.Success;
			}

			Debug.Log("[ImportFmodPackageStep] Downloading FMOD...");
			
			string url = "https://api.jettcodey.de/unitypackages/fmodstudio20312.unitypackage";
			string tempPath = Path.Combine(Application.temporaryCachePath, "fmodstudio20312.unitypackage");

			using (UnityWebRequest www = UnityWebRequest.Get(url))
			{
				await www.SendWebRequest().ToUniTask();

				if (www.result != UnityWebRequest.Result.Success)
				{
					Debug.LogError($"[ImportFmodPackageStep] Download failed: {www.error}");
					return StepResult.Failure;
				}

				File.WriteAllBytes(tempPath, www.downloadHandler.data);
			}

			Debug.Log("[ImportFmodPackageStep] Importing FMOD...");
			AssetDatabase.ImportPackage(tempPath, false);

			return StepResult.Success;
		}
		
		public void OnComplete(bool failed) 
		{
			// Clean up the downloaded file
			string tempPath = Path.Combine(Application.temporaryCachePath, "fmodstudio20312.unitypackage");
			if (File.Exists(tempPath))
			{
				File.Delete(tempPath);
			}
		}
	}
}*/