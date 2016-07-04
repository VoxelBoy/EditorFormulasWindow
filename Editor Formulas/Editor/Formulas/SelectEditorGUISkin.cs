using UnityEngine;
using UnityEditor;
using System.Reflection;

public static partial class EditorFormulas {

	public static void SelectEditorGUISkin()
	{
		Selection.activeObject = typeof(GUISkin).GetField("current", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).GetValue(null) as GUISkin;
	}

}
