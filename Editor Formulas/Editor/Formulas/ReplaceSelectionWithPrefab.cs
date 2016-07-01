using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public static partial class EditorFormulas {
	
	public static void ReplaceSelectedSceneObjectsWithSelectedPrefab()
	{
		var prefab = new List<Object>(Selection.GetFiltered(typeof(GameObject), SelectionMode.Assets)).Find(x => AssetDatabase.IsMainAsset (x)) as GameObject;
		if(prefab == null)
		{
			Debug.LogError("No prefab found among selected objects.");
			return;
		}

		var sceneTransforms = Selection.GetFiltered(typeof(Transform), SelectionMode.ExcludePrefab);
		foreach(Transform t in sceneTransforms)
		{
			var instantiatedPrefab = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
			Undo.RegisterCreatedObjectUndo(instantiatedPrefab, "Instantiate prefab " + instantiatedPrefab.name);
			instantiatedPrefab.transform.parent = t.parent;
			instantiatedPrefab.transform.localPosition = t.localPosition;
			instantiatedPrefab.transform.localRotation = t.localRotation;
			instantiatedPrefab.transform.localScale = t.localScale;
			Undo.DestroyObjectImmediate(t.gameObject);
		}
	}
}