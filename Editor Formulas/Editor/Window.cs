using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace EditorFormulas 
{
	public class Window : EditorWindow {

		[SerializeField]
		private Texture2D downloadTexture;

		[SerializeField]
		private Texture2D optionsTexture;

		WebHelper webHelper;

		Dictionary<MethodInfo, object[]> parameterValuesDictionary;
		Dictionary<MethodInfo, ParameterInfo[]> parametersDictionary;

		List<FormulaData> searchResults = new List<FormulaData>();

		public GUIStyle foldout;
		private bool initStyles = false;

		private Vector2 scrollPos;

		public string searchText = string.Empty;

		Vector2 windowSize = new Vector2(300, 400);

		FormulaDataStore formulaDataStore;

		GUIContent downloadButtonGUIContent;
		GUIContent optionsButtonGUIContent;
		GUIContent[] waitSpinGUIContents;

		bool doRepaint = false;

		private static Window instance;

		[MenuItem ("Window/Editor Formulas %#e")]
		public static void DoWindow()
		{
			var window = EditorWindow.GetWindow<Window>("Editor Formulas");
			var pos = window.position;
			pos.width = window.windowSize.x;
			pos.height = window.windowSize.y;
			window.position = pos;
		}

		void OnEnable()
		{
			instance = this;
			//Can be used to get the path to this class' path and use relative paths if necessary
			//Debug.Log("Path: " + AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this)));
			formulaDataStore = FormulaDataStore.LoadFromAssetDatabaseOrCreate();

			webHelper = ScriptableObject.CreateInstance<WebHelper>();
			webHelper.Init(formulaDataStore);
			webHelper.FormulaDataUpdated += FormulaDataUpdated;

			downloadButtonGUIContent = new GUIContent(downloadTexture, "Download Formula");
			optionsButtonGUIContent = new GUIContent(optionsTexture, "Options");

			waitSpinGUIContents = new GUIContent[12];
			for(int i=0; i<12; i++)
			{
				waitSpinGUIContents[i] = new GUIContent(EditorGUIUtility.FindTexture("WaitSpin" + i.ToString("00")));
			}

			LoadLocalFormulas();

			//Set up parameters
			var usableFormulas = formulaDataStore.FormulaData.FindAll(x => x.IsUsable);
			parametersDictionary = new Dictionary<MethodInfo, ParameterInfo[]>(usableFormulas.Count);
			parameterValuesDictionary = new Dictionary<MethodInfo, object[]>(usableFormulas.Count);
			foreach(var formula in usableFormulas)
			{
				var methodInfo = formula.methodInfo;
				parametersDictionary.Add(methodInfo, methodInfo.GetParameters());
				parameterValuesDictionary.Add(methodInfo, new object[methodInfo.GetParameters().Length]);
			}

			FilterBySearchText(searchText);

			webHelper.GetOnlineFormulas();

			EditorApplication.update += OnUpdate;
		}

		void OnDisable()
		{
			if(webHelper != null)
			{
				UnityEngine.Object.DestroyImmediate(webHelper);
				webHelper.FormulaDataUpdated -= FormulaDataUpdated;
			}
			EditorApplication.update -= OnUpdate;
			instance = null;
		}

		void LoadLocalFormulas()
		{
			var editorFormulasDirectory = new DirectoryInfo(Utils.GetFullPathFromAssetsPath(Constants.formulasFolderUnityPath));
			var files = new List<FileInfo>(editorFormulasDirectory.GetFiles());
			//Remove all files that don't have a .cs extension
			files.RemoveAll(x => ! x.Extension.Equals(".cs", StringComparison.InvariantCultureIgnoreCase));

			foreach(var file in files)
			{
				var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.FullName);
				var formula = formulaDataStore.FormulaData.Find(x => x.name == fileNameWithoutExtension);
				var methodInfo = Utils.GetFormulaMethod(fileNameWithoutExtension);

				//Process only if valid method was found
				if(methodInfo != null)
				{
					//If formula doesn't exist in formulaDataStore
					if(formula == null)
					{
						formula = new FormulaData();
						formula.name = fileNameWithoutExtension;
						formula.projectFilePath = Constants.formulasFolderUnityPath + file.Name;
						formula.localFileExists = new FileInfo(Utils.GetFullPathFromAssetsPath(formula.projectFilePath)).Exists;
						formulaDataStore.FormulaData.Add(formula);
						EditorUtility.SetDirty(formulaDataStore);
					}
					//Update formula's methodInfo reference
					formula.methodInfo = methodInfo;
				}
			}

			//If there are local formulas in data store that point to local files that don't exist
			//and also don't have downloadURLs, remove them
			for(int i=formulaDataStore.FormulaData.Count-1; i>=0; i--)
			{
				var formulaData = formulaDataStore.FormulaData[i];
				var fullPath = Utils.GetFullPathFromAssetsPath(formulaData.projectFilePath);
				var fi = new FileInfo(fullPath);
				if(!fi.Exists && string.IsNullOrEmpty(formulaData.downloadURL))
				{
					formulaDataStore.FormulaData.RemoveAt(i);
					EditorUtility.SetDirty(formulaDataStore);
				}
			}
		}

		[UnityEditor.Callbacks.DidReloadScripts]
		private static void OnScriptsReloaded() {
//			Debug.Log("On scripts reload window is " + (instance == null ? "null" : "not null"));
			if(instance != null)
			{
				instance.Repaint();
			}
		}

		void OnUpdate()
		{
			if(webHelper.DownloadingFormula)
			{
				doRepaint = true;
			}

			if(doRepaint)
			{
				this.Repaint();
				doRepaint = false;
			}
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

			//Draw search results
			for(int i=0; i<searchResults.Count; i++)
			{
				var formula = searchResults[i];

				if(formula.IsUsable)
				{
					DrawUsableFormula(formula);
				}
				else if(!formula.localFileExists)
				{
					DrawOnlineFormula(formula);
				}
			}

			EditorGUILayout.EndScrollView();
		}

		void DrawUsableFormula(FormulaData formula)
		{
			var niceName = ObjectNames.NicifyVariableName(formula.name);
			var method = formula.methodInfo;

			if(method == null)
			{
				return;
			}

			var parameters = parametersDictionary[method];
			var parameterValuesArray = parameterValuesDictionary[method];

			if(parameters.Length > 0)
			{
				GUILayout.BeginVertical(GUI.skin.box, GUILayout.MaxWidth(this.position.width));
			}

			GUILayout.BeginHorizontal();

			//Button is only enabled if parameters have been initialized
			GUI.enabled = parameters.Length == 0 || parameterValuesArray.All(x => x != null);
			if(GUILayout.Button(new GUIContent(niceName, niceName), GUILayout.MaxWidth(this.position.width - 30)))
			{
				method.Invoke(null, parameterValuesArray);
			}
			GUI.enabled = true;
			if(GUILayout.Button(optionsButtonGUIContent, GUILayout.MaxWidth(20), GUILayout.MaxHeight(18)))
			{
				var menu = new GenericMenu();
				menu.AddItem(new GUIContent("Open in External Script Editor"), false, OpenFormulaInExternalScriptEditor, formula);
				menu.AddItem(new GUIContent("Go to download URL"), false, GoToFormulaDownloadURL, formula);
				menu.AddItem(new GUIContent("Delete"), false, DeleteFormula, formula);
				menu.ShowAsContext();
			}
			GUILayout.EndHorizontal();

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

	//				if(parameterType.IsClass && parameterType.IsSerializable)
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
						//We use a Vector4Field for RectOffset type because there isn't an Editor GUI drawer for rect offset
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
					else if(parameterType == typeof(UnityEngine.Object))
					{
						newValue = EditorGUILayout.ObjectField(new GUIContent(niceParameterName, niceParameterName), valueObj != null ? ((UnityEngine.Object) valueObj) : null, parameterType, true);
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

			if(parameters.Length > 0)
			{
				GUILayout.EndVertical();
			}
		}

		void DrawOnlineFormula(FormulaData formula)
		{
			var niceName = ObjectNames.NicifyVariableName(formula.name);
			//Button is disabled until formula is downloaded
			var guiEnabled = GUI.enabled;
			GUI.enabled = false;
			GUILayout.BeginHorizontal();
			GUILayout.Button(new GUIContent(niceName, niceName), GUILayout.MaxWidth(this.position.width - 34));
			GUI.enabled = guiEnabled;

			var guiContent = downloadButtonGUIContent;
			var diffInMilliseconds = DateTime.UtcNow.Subtract(formula.DownloadTimeUTC).TotalMilliseconds;
			bool compilingOrDownloadingFormula = (EditorApplication.isCompiling && diffInMilliseconds < 20000) || webHelper.IsDownloadingFormula(formula);
			//If the formula is in WebHelper's download queue or
			//the editor is compiling and download was less than 20 seconds ago, show spinner
			if(compilingOrDownloadingFormula)
			{
				int waitSpinIndex = Mathf.FloorToInt(((float)(diffInMilliseconds % 2000d) / 2000f) * 12f);
				guiContent = waitSpinGUIContents[waitSpinIndex];
				doRepaint = true;
			}

			if(GUILayout.Button(guiContent, GUILayout.MaxWidth(24), GUILayout.MaxHeight(18)))
			{
				//Button should do nothing if compiling or downloading formula
				if(!compilingOrDownloadingFormula)
				{
					webHelper.DownloadFormula(formula);
				}
			}

			GUILayout.EndHorizontal();
		}

		void FilterBySearchText(string text)
		{
			searchResults.Clear();
			searchResults.AddRange(formulaDataStore.FormulaData);

			if(string.IsNullOrEmpty(text.Trim()))
			{
				return;
			}

			//If search text has multiple words, check each one and AND them
			var words = text.Split(new char[] {' '}, System.StringSplitOptions.RemoveEmptyEntries);
			if(words.Length == 0) { return; }

			//If there's only one word, check against normal method name, which has no spaces in it
			if(words.Length == 1)
			{
				//Remove all methods whose name doesn't contain search text
				searchResults.RemoveAll(x => 
					!x.name.ToLower ().Contains (text.Trim().ToLower ())
				);
			}
			//If there are multiple words, check that each one is contained in the nicified method name
			else
			{
				searchResults.RemoveAll(x => 
					{
						var niceMethodName = ObjectNames.NicifyVariableName(x.name).ToLower();
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

			searchResults.Sort((x,y) => x.name.CompareTo(y.name));
		}

		void FormulaDataUpdated()
		{
			FilterBySearchText(searchText);
			this.Repaint();
		}

		void OpenFormulaInExternalScriptEditor (object obj)
		{
			var formulaData = obj as FormulaData;
			if(formulaData == null)
			{
				return;
			}
			AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<MonoScript>(formulaData.projectFilePath).GetInstanceID());
		}

		void GoToFormulaDownloadURL (object obj)
		{
			var formulaData = obj as FormulaData;
			if(formulaData == null)
			{
				return;
			}
			Application.OpenURL(formulaData.downloadURL);
		}

		void DeleteFormula (object obj)
		{
			var formulaData = obj as FormulaData;
			if(formulaData == null)
			{
				return;
			}
			var fi = new FileInfo(Utils.GetFullPathFromAssetsPath(formulaData.projectFilePath));
			if(fi.Exists)
			{
				fi.Delete();
				AssetDatabase.Refresh();
			}
			formulaData.projectFilePath = string.Empty;
			formulaData.DownloadTimeUTC = DateTime.MinValue;
			formulaData.methodInfo = null;
			EditorUtility.SetDirty(formulaDataStore);
			FilterBySearchText(searchText);
		}
	}
}