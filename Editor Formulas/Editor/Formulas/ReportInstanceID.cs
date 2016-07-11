using UnityEngine;
using UnityEditor;

namespace EditorFormulas
{
	public static partial class Formulas {

		public static void ReportInstanceID()
		{
			if(Selection.activeObject != null)
			{
				Debug.Log(Selection.activeObject.GetInstanceID());
			}
		}

	}
}
