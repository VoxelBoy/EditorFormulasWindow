namespace EditorFormulas 
{
	public static class Constants {
		
		public const string formulasRepoContentsURL = "https://api.github.com/repos/VoxelBoy/EditorFormulas/contents";
		public const string assetPath = "Assets/Editor Formulas/Editor/FormulaDataStore.asset";
		public const string formulasFolderUnityPath = "Assets/Editor Formulas/Editor/Formulas/";

		public const double getOnlineFormulasFrequencyInMinutes = 5d;
		public const double checkForFormulaUpdateFrequencyInMinutes = 60d;

		public const string debugModePrefKey = "EditorFormulasWindow_DebugMode";
		public const string showHiddenFormulasPrefKey = "EditorFormulasWindow_ShowHiddenFormulas";
		public const string showOnlineFormulasPrefKey = "EditorFormulasWindow_ShowOnlineFormulas";
	}
}