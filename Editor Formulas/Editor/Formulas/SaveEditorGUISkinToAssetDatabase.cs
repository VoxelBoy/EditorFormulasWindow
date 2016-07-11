using UnityEngine;
using UnityEditor;

namespace EditorFormulas
{
	public static partial class Formulas {

		public static void SaveEditorGUISkinToAssetDatabase()
		{
			var currentGUISkin = GetEditorGUISkin();

			var newGUISkin = Object.Instantiate(currentGUISkin);
			newGUISkin.hideFlags = HideFlags.None;

			var assetPath = AssetDatabase.GenerateUniqueAssetPath("Assets/EditorGUISkin.guiskin");

			AssetDatabase.CreateAsset (newGUISkin, assetPath);
			AssetDatabase.SaveAssets ();
			AssetDatabase.Refresh();
		}

	}
}
