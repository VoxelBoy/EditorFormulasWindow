using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;

public class FormulaDataStore : ScriptableObject {

	public List<FormulaData> FormulaData;
	public long lastUpdateTimeBinary;
	public DateTime LastUpdateTime
	{
		get
		{
			return DateTime.FromBinary(lastUpdateTimeBinary);
		}
	}

	public static FormulaDataStore LoadFromAssetDatabaseOrCreate()
	{
		var store = AssetDatabase.LoadAssetAtPath<FormulaDataStore>(EditorFormulasConstants.assetPath);
		if(store == null)
		{
			store = ScriptableObject.CreateInstance<FormulaDataStore>();

			AssetDatabase.CreateAsset(store, EditorFormulasConstants.assetPath);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}
		return store;
	}

	void OnEnable()
	{
		if(FormulaData == null)
		{
			FormulaData = new List<FormulaData>();
		}
	}
}
