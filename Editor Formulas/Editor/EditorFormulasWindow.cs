using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

public class EditorFormulasWindow : EditorWindow {

	MethodInfo[] formulaMethods;
	Dictionary<MethodInfo, object[]> parameterValuesDictionary;
	Dictionary<MethodInfo, ParameterInfo[]> parametersDictionary;
	Dictionary<MethodInfo, bool> toggleStateDictionary;

	List<MethodInfo> searchResults = new List<MethodInfo>();

	public GUIStyle foldout;
	private bool initStyles = false;

	private Vector2 scrollPos;

	string searchText = string.Empty;

	Vector2 windowSize = new Vector2(300, 400);

	[MenuItem ("Window/Editor Formulas %#e")]
	public static void DoWindow()
	{
		var window = EditorWindow.GetWindow<EditorFormulasWindow>("Editor Formulas");
		var pos = window.position;
		pos.width = window.windowSize.x;
		pos.height = window.windowSize.y;
		window.position = pos;
	}

	void OnEnable()
	{
		Refresh();
	}

	void Refresh()
	{
		formulaMethods = ReflectionHelper.GetTypeInfo("EditorFormulas", "Assembly-CSharp-Editor").type.GetMethods(BindingFlags.Static | BindingFlags.Public);
		parametersDictionary = new Dictionary<MethodInfo, ParameterInfo[]>(formulaMethods.Length);
		parameterValuesDictionary = new Dictionary<MethodInfo, object[]>(formulaMethods.Length);
		toggleStateDictionary = new Dictionary<MethodInfo, bool>(formulaMethods.Length);
		foreach(var method in formulaMethods)
		{
			parametersDictionary.Add(method, method.GetParameters());
			parameterValuesDictionary.Add(method, new object[method.GetParameters().Length]);
			toggleStateDictionary.Add(method, false);
		}
		FilterBySearchText(searchText);
	}

	void OnGUI()
	{
		if(!initStyles)
		{
			initStyles = true;
		}

		EditorGUI.BeginChangeCheck();
		searchText = EditorGUILayout.TextField(searchText);
		if(EditorGUI.EndChangeCheck())
		{
			FilterBySearchText(searchText);
		}

		scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

		for(int i=0; i<searchResults.Count; i++)
		{
			var method = searchResults[i];
			if(method == null) { continue; }

			var parameters = parametersDictionary[method];
			var parameterValuesArray = parameterValuesDictionary[method];

			var niceName = ObjectNames.NicifyVariableName(method.Name);

			GUILayout.BeginVertical(GUI.skin.box, GUILayout.MaxWidth(this.position.width));

			GUI.enabled = parameters.Length == 0 || parameterValuesArray.All(x => x != null);
			if(GUILayout.Button(new GUIContent(niceName, niceName), GUILayout.MaxWidth(this.position.width - 10)))
			{
				method.Invoke(null, parameterValuesArray);
			}
			GUI.enabled = true;

			if(parameters.Length > 0)
			{
				//Draw parameter fields
				for (int p=0; p<parameters.Length; p++) {
					var parameter = parameters[p];
					var parameterType = parameter.ParameterType;
					var niceParameterName = ObjectNames.NicifyVariableName(parameter.Name);
					var valueObj = parameterValuesArray[p];
					GUILayout.BeginHorizontal();
					object newValue = null;

	//				if(parameterType == typeof(Object))
	//				{
	//					newValue = EditorGUILayout.ObjectField(valueObj != null ? ((Object) valueObj) : null, parameterType, true);
	//				}
	//				else if(parameterType.IsClass && parameterType.IsSerializable)
	//				{
	//					var fieldInfos = parameterType.GetFields(BindingFlags.Instance | BindingFlags.Public);
	//					//TODO: Draw a field for each public instance field of class
	//				}

					EditorGUI.BeginChangeCheck();
					if (parameterType == typeof(int)) {
						newValue = EditorGUILayout.IntField(new GUIContent(niceParameterName, niceParameterName), valueObj != null ? ((int) valueObj) : 0 );
					}
					else if(parameterType == typeof(float))
					{
						newValue = EditorGUILayout.FloatField(new GUIContent(niceParameterName, niceParameterName), valueObj != null ? ((float) valueObj) : 0f );
					}
					else if(parameterType == typeof(string))
					{
						newValue = EditorGUILayout.TextField(new GUIContent(niceParameterName, niceParameterName), valueObj != null ? ((string) valueObj) : string.Empty );
					}
					else if(parameterType == typeof(Rect))
					{
						newValue = EditorGUILayout.RectField(new GUIContent(niceParameterName, niceParameterName), valueObj != null ? ((Rect) valueObj) : new Rect() );
					}
					//TODO: Don't do this, instead use RectOffset as a class
					else if(parameterType == typeof(RectOffset))
					{
						//We use a Vector4Field for RectOffset type because there isn't 
						var rectOffset = (RectOffset) valueObj;
						var vec4 = EditorGUILayout.Vector4Field(niceParameterName, valueObj != null ? new Vector4(rectOffset.left, rectOffset.right, rectOffset.top, rectOffset.bottom) : Vector4.zero );
						newValue = new RectOffset((int)vec4.x, (int)vec4.y, (int)vec4.z, (int)vec4.w);
					}
					else if(parameterType == typeof(Vector2))
					{
						var fieldWidth = EditorGUIUtility.fieldWidth;
						EditorGUIUtility.fieldWidth = 1f;
						newValue = EditorGUILayout.Vector2Field(new GUIContent(niceParameterName, niceParameterName), valueObj != null ? ((Vector2) valueObj) : Vector2.zero );
						EditorGUIUtility.fieldWidth = fieldWidth;
					}
					else if(parameterType == typeof(Vector3))
					{
						newValue = EditorGUILayout.Vector3Field(new GUIContent(niceParameterName, niceParameterName), valueObj != null ? ((Vector3) valueObj) : Vector3.zero );
					}
					else if(parameterType == typeof(Vector4))
					{
						newValue = EditorGUILayout.Vector4Field(niceParameterName, valueObj != null ? ((Vector4) valueObj) : Vector4.zero );
					}
					else if(parameterType == typeof(Color))
					{
						newValue = EditorGUILayout.ColorField(new GUIContent(niceParameterName, niceParameterName), valueObj != null ? ((Color) valueObj) : Color.white);
					}
					else if(parameterType == typeof(Object))
					{
						newValue = EditorGUILayout.ObjectField(new GUIContent(niceParameterName, niceParameterName), valueObj != null ? ((Object) valueObj) : null, parameterType, true);
					}
					else if(parameterType.IsEnum)
					{
						newValue = EditorGUILayout.EnumPopup(new GUIContent(niceParameterName, niceParameterName), valueObj != null ? ((System.Enum)valueObj) : default(System.Enum));
					}
					if(EditorGUI.EndChangeCheck())
					{
						parameterValuesArray[p] = newValue;
					}
					GUILayout.EndHorizontal();
				}
			}
			GUILayout.EndVertical();
		}

		EditorGUILayout.EndScrollView();
	}

	void FilterBySearchText(string text)
	{
		searchResults.Clear();
		searchResults.AddRange(formulaMethods);

		//If search text has multiple words, check each one and AND them
		var words = text.Split(new char[] {' '}, System.StringSplitOptions.RemoveEmptyEntries);
		if(words.Length == 0) { return; }

		//If there's only one word, check against normal method name, which has no spaces in it
		if(words.Length == 1)
		{
			searchResults.RemoveAll(x => 
				!x.Name.ToLower ().Contains (text.Trim().ToLower ())
			);
			//
			

			//Remove all methods whose name doesn't contain search text
		}
		//If there are multiple words, check that each one is contained in the nicified method name
		else
		{
			searchResults.RemoveAll(x => 
				{
					var niceMethodName = ObjectNames.NicifyVariableName(x.Name).ToLower();
					bool allWordsContained = true;
					foreach(var word in words)
					{
						if(!niceMethodName.Contains(word.ToLower()))
						{
							allWordsContained = false;
							break;
						}
					}
					return !allWordsContained;
				}
			);
		}
	}
}
