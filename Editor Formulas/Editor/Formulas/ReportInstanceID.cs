using UnityEngine;
using UnityEditor;

public static partial class EditorFormulas {

	public static void ReportInstanceID()
	{
		if(Selection.activeObject != null)
		{
			Debug.Log(Selection.activeObject.GetInstanceID());
		}
	}

}
