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

	public GUIStyle foldout;
	private bool initStyles = false;

	private Vector2 scrollPos;

	[MenuItem ("Window/Editor Formulas %#e")]
	public static void DoWindow()
	{
		EditorWindow.GetWindow<EditorFormulasWindow>();
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
	}

	void OnGUI()
	{
		if(!initStyles)
		{
			initStyles = true;
		}

		scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

		for(int i=0; i<formulaMethods.Length; i++)
		{
			var method = formulaMethods[i];
			if(method == null) { continue; }

			var parameters = parametersDictionary[method];
			var parameterValuesArray = parameterValuesDictionary[method];

			var niceName = ObjectNames.NicifyVariableName(method.Name);

			GUI.enabled = parameters.Length == 0 || parameterValuesArray.All(x => x != null);
			if(GUILayout.Button(niceName))
			{
				method.Invoke(null, parameterValuesArray);
			}
			GUI.enabled = true;

			if(parameters.Length > 0)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Space(15f);
				GUILayout.BeginVertical();
				//Draw parameter fields
				for (int p=0; p<parameters.Length; p++) {
					var parameter = parameters[p];
					var parameterType = parameter.ParameterType;
					var valueObj = parameterValuesArray[p];
					GUILayout.BeginHorizontal();
					GUILayout.Label(new GUIContent(parameter.Name, parameter.Name));//, GUILayout.Width(100));
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
						newValue = EditorGUILayout.IntField(valueObj != null ? ((int) valueObj) : 0 );
					}
					else if(parameterType == typeof(float))
					{
						newValue = EditorGUILayout.FloatField(valueObj != null ? ((float) valueObj) : 0f );
					}
					else if(parameterType == typeof(string))
					{
						newValue = EditorGUILayout.TextField(valueObj != null ? ((string) valueObj) : string.Empty );
					}
					else if(parameterType == typeof(Rect))
					{
						newValue = EditorGUILayout.RectField(valueObj != null ? ((Rect) valueObj) : new Rect() );
					}
					//TODO: Don't do this, instead use RectOffset as a class
					else if(parameterType == typeof(RectOffset))
					{
						//We use a Vector4Field for RectOffset type because there isn't 
						var rectOffset = (RectOffset) valueObj;
						var vec4 = EditorGUILayout.Vector4Field(string.Empty, valueObj != null ? new Vector4(rectOffset.left, rectOffset.right, rectOffset.top, rectOffset.bottom) : Vector4.zero );
						newValue = new RectOffset((int)vec4.x, (int)vec4.y, (int)vec4.z, (int)vec4.w);
					}
					else if(parameterType == typeof(Vector2))
					{
						newValue = EditorGUILayout.Vector2Field(string.Empty, valueObj != null ? ((Vector2) valueObj) : Vector2.zero );
					}
					else if(parameterType == typeof(Vector3))
					{
						newValue = EditorGUILayout.Vector3Field(string.Empty, valueObj != null ? ((Vector3) valueObj) : Vector3.zero );
					}
					else if(parameterType == typeof(Vector4))
					{
						newValue = EditorGUILayout.Vector4Field(string.Empty, valueObj != null ? ((Vector4) valueObj) : Vector4.zero );
					}
					else if(parameterType == typeof(Color))
					{
						newValue = EditorGUILayout.ColorField(valueObj != null ? ((Color) valueObj) : Color.white);
					}
					else if(parameterType == typeof(Object))
					{
						newValue = EditorGUILayout.ObjectField(valueObj != null ? ((Object) valueObj) : null, parameterType, true);
					}
					else if(parameterType.IsEnum)
					{
						newValue = EditorGUILayout.EnumPopup(valueObj != null ? ((System.Enum)valueObj) : default(System.Enum));
					}
					if(EditorGUI.EndChangeCheck())
					{
						parameterValuesArray[p] = newValue;
					}
					GUILayout.EndHorizontal();
				}
				GUILayout.EndVertical();
				GUILayout.EndHorizontal();
			}
		}

		EditorGUILayout.EndScrollView();
	}
}
