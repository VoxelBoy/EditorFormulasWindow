using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace EditorFormulas
{
	public static partial class Formulas {

		public static void SelectEditorGUISkin()
		{
			Selection.activeObject = typeof(GUISkin).GetField("current", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).GetValue(null) as GUISkin;
		}

	}
}
