using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace EditorFormulas
{
	public static partial class Formulas {

		public static void LogActiveEditorWindow()
		{
			Debug.Log(string.Format("{0} of type {1}", EditorWindow.focusedWindow.titleContent.text, EditorWindow.focusedWindow.GetType().FullName));
		}

		public static void ReportAllAssemblies()
		{
			var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
			foreach(var assembly in assemblies) {
				Debug.Log("Assembly: " + assembly.FullName);
			}
		}

		private static GUISkin GetEditorGUISkin()
		{
			return typeof(GUISkin).GetField("current", ReflectionHelper.fullBindingStatic).GetValue(null) as GUISkin;
		}

		public static void ReportAssetPathsOfSelectedObjects()
		{
			var selectedObjects = Selection.objects;
			foreach(var obj in selectedObjects)
			{
				if(AssetDatabase.Contains(obj))
				{
					Debug.Log(AssetDatabase.GetAssetPath(obj));
				}
			}
		}

		public static void CopyEditorStyleToSelectedGUISkin(string styleName)
		{
			var selectedGUISkin = Selection.activeObject as GUISkin;
			if(selectedGUISkin == null)
			{
				Debug.Log("Selected object is not a GUISkin");
				return;
			}

			var editorGUISkin = GetEditorGUISkin();
			var style = editorGUISkin.FindStyle(styleName);
			if(style == null)
			{
				Debug.Log(string.Format("No style with name {0} found in editor GUI skin", styleName));
				return;
			}

			var existingStyle = selectedGUISkin.FindStyle(styleName);
			if(existingStyle != null)
			{
				Debug.Log(string.Format("Selected gui skin already contains a style with name {0}", styleName));
				return;
			}

			var listOfStyles = new List<GUIStyle>(selectedGUISkin.customStyles);
			var styleInstance = new GUIStyle(style);
			listOfStyles.Add(styleInstance);
			selectedGUISkin.customStyles = listOfStyles.ToArray();

			DirtyAndSaveAsset(selectedGUISkin);
		}

		public static void ClearStyleCacheOfSelectedGUISkin()
		{
			var selectedGUISkin = Selection.activeObject as GUISkin;
			if(selectedGUISkin == null)
			{
				Debug.Log("Selected object is not a GUISkin");
				return;
			}

			typeof(GUISkin).GetField("m_Styles", ReflectionHelper.fullBinding).SetValue(selectedGUISkin, null);
		}

		public static void SetBorderOfStylesStartingWithNameInSelectedGUISkin(string styleNameStartsWith, RectOffset border)
		{
			var skin = Selection.activeObject as GUISkin;
			DoActionOnStylesStartingWithNameInGUISkin(skin, styleNameStartsWith, SetBorder, border);
			DirtyAndSaveAsset(skin);
		}

		public static void SetMarginOfStylesStartingWithNameInSelectedGUISkin(string styleNameStartsWith, RectOffset margin)
		{
			var skin = Selection.activeObject as GUISkin;
			DoActionOnStylesStartingWithNameInGUISkin(skin, styleNameStartsWith, SetMargin, margin);
			DirtyAndSaveAsset(skin);
		}

		public static void SetPaddingOfStylesStartingWithNameInSelectedGUISkin(string styleNameStartsWith, RectOffset padding)
		{
			var skin = Selection.activeObject as GUISkin;
			DoActionOnStylesStartingWithNameInGUISkin(skin, styleNameStartsWith, SetPadding, padding);
			DirtyAndSaveAsset(skin);
		}

		public static void SetOverflowOfStylesStartingWithNameInSelectedGUISkin(string styleNameStartsWith, RectOffset overflow)
		{
			var skin = Selection.activeObject as GUISkin;
			DoActionOnStylesStartingWithNameInGUISkin(skin, styleNameStartsWith, SetOverflow, overflow);
			DirtyAndSaveAsset(skin);
		}

		public static void SetContentOffsetOfStylesStartingWithNameInSelectedGUISkin(string styleNameStartsWith, Vector2 contentOffset)
		{
			var skin = Selection.activeObject as GUISkin;
			DoActionOnStylesStartingWithNameInGUISkin(skin, styleNameStartsWith, SetContentOffset, contentOffset);
			DirtyAndSaveAsset(skin);
		}

		public static void TestFindAsset()
		{
			var asset = Selection.activeObject;
			if(asset == null) { return; }
			if(!AssetDatabase.Contains(asset))
			{
				return;
			}
			var searchFilterType = ReflectionHelper.GetTypeInfo("SearchFilter");
			var searchFilterConstructor = searchFilterType.type.GetConstructor(System.Type.EmptyTypes);
			var searchFilter = searchFilterConstructor.Invoke(null);
			//Search all assets
			searchFilterType.GetPropertyInfo("searchArea").SetValue(searchFilter, 0, null);
			//Set referencing instance ids to guid of material asset
			searchFilterType.GetPropertyInfo("referencingInstanceIDs").SetValue(searchFilter, new [] {asset.GetInstanceID()}, null);
			var assetDatabaseType = typeof(AssetDatabase);
			var findAssetsMethod = assetDatabaseType.GetMethod("FindAssets", ReflectionHelper.fullBinding, null, new [] {searchFilterType.type}, null);
			var guids = (string[]) findAssetsMethod.Invoke(null, new System.Object[] {searchFilter});
			foreach(var guid in guids)
			{
				Debug.Log(AssetDatabase.GUIDToAssetPath(guid));
			}
		}

		public static void SceneObjectInstanceIDTest()
		{
			var objs = UnityEditorInternal.InternalEditorUtility.LoadSerializedFileAndForget("Assets/testScene1.unity");
			for(int i=0; i<objs.Length; i++)
			{
				var obj = objs[i];
				if(obj is GameObject && obj.name == "Sphere")
				{
					Debug.Log(obj.GetInstanceID());
					// destroy the scene Objects
					Object.DestroyImmediate(obj);
					System.GC.Collect();
					Resources.UnloadUnusedAssets();
				}
			}

			objs = UnityEditorInternal.InternalEditorUtility.LoadSerializedFileAndForget("Assets/testScene1.unity");
			for(int i=0; i<objs.Length; i++)
			{
				var obj = objs[i];
				if(obj is GameObject && obj.name == "Sphere")
				{
					Debug.Log(obj.GetInstanceID());
					// destroy the scene Objects
					Object.DestroyImmediate(obj);
					System.GC.Collect();
					Resources.UnloadUnusedAssets();
				}
			}
		}

		private static void SetBorder(GUIStyle style, object parameter)
		{
			style.border = (RectOffset) parameter;
		}

		private static void SetMargin(GUIStyle style, object parameter)
		{
			style.margin = (RectOffset) parameter;
		}

		private static void SetPadding(GUIStyle style, object parameter)
		{
			style.padding = (RectOffset) parameter;
		}

		private static void SetOverflow(GUIStyle style, object parameter)
		{
			style.overflow = (RectOffset) parameter;
		}

		private static void SetContentOffset(GUIStyle style, object parameter)
		{
			style.contentOffset = (Vector2) parameter;
		}

		private static void DoActionOnStylesStartingWithNameInGUISkin(GUISkin skin, string styleNameStartsWith, System.Action<GUIStyle, object> action, object parameter)
		{
			if(skin == null) { return; }
			foreach (var style in skin.customStyles) {
				if (!style.name.StartsWith (styleNameStartsWith)) {
					continue;
				}
				action (style, parameter);
			}
		}


		private static void DirtyAndSaveAsset(Object obj)
		{
			if(obj == null) { return; }
			EditorUtility.SetDirty(obj);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		private static void CreateAndSaveAsset(Object obj, string path)
		{
			if(obj == null) { return; }
			AssetDatabase.CreateAsset (obj, path);
			AssetDatabase.SaveAssets ();
			AssetDatabase.Refresh();
		}

		private static void CreateAsset<T> () where T : ScriptableObject
		{
			T asset = ScriptableObject.CreateInstance<T> ();

			string path = AssetDatabase.GetAssetPath (Selection.activeObject);
			if (path == "" || !path.StartsWith("Assets"))
			{
				path = "Assets";
			}
			else if (Path.GetExtension (path) != "") 
			{
				path = path.Replace (Path.GetFileName (AssetDatabase.GetAssetPath (Selection.activeObject)), "");
			}

			string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath (path + "/New " + typeof(T).ToString() + ".asset");

			Debug.Log(assetPathAndName);
			CreateAndSaveAsset(asset, assetPathAndName);
			EditorUtility.FocusProjectWindow ();
			Selection.activeObject = asset;
		}

	}
}
