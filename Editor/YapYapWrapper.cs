using Nomnom.UnityProjectPatcher.Editor;
using Nomnom.UnityProjectPatcher.Editor.Steps;

namespace YapYapModding.YapYapProjectPatcher.Editor
{
	[UPPatcher("com.yapyapmodding.unity-yapyap-project-patcher")]
	public static class YapYapWrapper
	{
		public static void GetSteps(StepPipeline stepPipeline)
		{
			stepPipeline.Steps.Clear();

			stepPipeline.InsertLast(new GenerateDefaultProjectStructureStep());
			// stepPipeline.InsertLast(new ImportTextMeshProStep());  // Doesnt work with Unity 6+
			stepPipeline.InsertLast(new ImportTMPEssentialsExtrasStep());
			stepPipeline.InsertLast(new GenerateGitIgnoreStep());
			// stepPipeline.InsertLast(new GenerateReadmeStep());
			// stepPipeline.InsertLast(new FModPackageInstallStep());
			stepPipeline.InsertLast(new PackagesInstallerStep()); // recompile
			stepPipeline.InsertLast(new CacheProjectCatalogueStep());
			stepPipeline.InsertLast(new AssetRipperStep());
			stepPipeline.InsertLast(new CopyGamePluginsStep()); // recompile
			stepPipeline.InsertLast(new CopyExplicitScriptFolderStep());  //restart
			stepPipeline.InsertLast(new PreGenerateAssembliesStep()); //recompile (may be better here idk, need to test)
			stepPipeline.InsertLast(new EnableUnsafeCodeStep());  //recompile
			stepPipeline.InsertLast(new CopyProjectSettingsStep(allowUnsafeCode: true));
			stepPipeline.InsertLast(new GuidRemapperStep());
			stepPipeline.InsertLast(new CopyAssetRipperExportToProjectStep());  //restart (throws safe mode error here?)
			stepPipeline.InsertLast(new FixProjectFileIdsStep());
			stepPipeline.InsertLast(new SortAssetTypesSteps());
			stepPipeline.InsertLast(new RestartEditorStep());  //restart (who would've thought lol)

			stepPipeline.SetInputSystem(InputSystemType.Both);
			stepPipeline.SetGameViewResolution("16:9");
			stepPipeline.OpenSceneAtEnd("Bootstrap");
		}
	}
}