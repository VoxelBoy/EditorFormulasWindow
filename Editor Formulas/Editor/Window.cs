using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace EditorFormulas 
{
	public class Window : EditorWindow {

		[SerializeField]
		private Texture2D downloadTexture;

		Dictionary<MethodInfo, object[]> parameterValuesDictionary;
		Dictionary<MethodInfo, ParameterInfo[]> parametersDictionary;

		List<FormulaData> searchResults = new List<FormulaData>();
		List<FormulaData> formulasToDownload = new List<FormulaData>();
		FormulaData formulaBeingDownloaded = null;
		HttpWebResponse downloadFormulaResponse = null;

		public GUIStyle foldout;
		private bool initStyles = false;

		private Vector2 scrollPos;

		public string searchText = string.Empty;

		Vector2 windowSize = new Vector2(300, 400);

		FormulaDataStore formulaDataStore;

		HttpWebRequest webRequest;

		GUIContent downloadButtonGUIContent;

		HttpWebResponse getOnlineFormulasResponse = null;

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
			Debug.Log("On Enable");
			formulaDataStore = FormulaDataStore.LoadFromAssetDatabaseOrCreate();

			downloadButtonGUIContent = new GUIContent(downloadTexture, "Download Formula");

			EditorApplication.update += OnUpdate;

			LoadLocalFormulas();

			//Set up parameters
			var usableFormulas = formulaDataStore.FormulaData.FindAll(x => x.methodInfo != null);
			parametersDictionary = new Dictionary<MethodInfo, ParameterInfo[]>(usableFormulas.Count);
			parameterValuesDictionary = new Dictionary<MethodInfo, object[]>(usableFormulas.Count);
			foreach(var formula in usableFormulas)
			{
				var methodInfo = formula.methodInfo;
				parametersDictionary.Add(methodInfo, methodInfo.GetParameters());
				parameterValuesDictionary.Add(methodInfo, new object[methodInfo.GetParameters().Length]);
			}

			FilterBySearchText(searchText);

			GetOnlineFormulas();
		}

		void OnDisable()
		{
			Debug.Log("On Disable");
			EditorApplication.update -= OnUpdate;
		}

		void LoadLocalFormulas()
		{
			var assetsDirectory = new DirectoryInfo(Application.dataPath);
			var editorFormulasDirectory = new DirectoryInfo(Path.Combine(assetsDirectory.Parent.FullName, Constants.formulasFolderUnityPath));
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
						formula.localFilePath = Constants.formulasFolderUnityPath + file.Name;
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
				var formula = formulaDataStore.FormulaData[i];
				var fi = new FileInfo(formula.localFilePath);
				if(! fi.Exists && string.IsNullOrEmpty(formula.downloadURL))
				{
					formulaDataStore.FormulaData.RemoveAt(i);
					EditorUtility.SetDirty(formulaDataStore);
				}
			}
		}

		//TODO: Offer to update each formula based on download time
		void GetOnlineFormulas()
		{
			if(webRequest != null)
			{
				Debug.Log("Another web request is already in progress");
				return;
			}
			webRequest = WebRequest.Create(new Uri(Constants.formulasRepoContentsURL)) as HttpWebRequest;
			webRequest.UserAgent = "EditorFormulas";
			webRequest.Method = "GET";
			webRequest.IfModifiedSince = formulaDataStore.LastUpdateTime;

			webRequest.BeginGetResponse(HandleAsync_GetOnlineFormulas, null);
		}

		void HandleAsync_GetOnlineFormulas (IAsyncResult ar)
		{
			if(!ar.IsCompleted)
			{
				return;
			}

			try
			{
				getOnlineFormulasResponse = webRequest.EndGetResponse(ar) as HttpWebResponse;
				webRequest = null;
			}
			catch (WebException ex)
			{
				webRequest = null;
				//TODO: We could get Name Resolution Failure if there's no connection, how to handle?
			}
		}

		void HandleAsync_DownloadFormula (IAsyncResult ar)
		{
			if(!ar.IsCompleted)
			{
				return;
			}

			try
			{
				downloadFormulaResponse = webRequest.EndGetResponse(ar) as HttpWebResponse;
				webRequest = null;
			}
			catch (WebException ex)
			{
				webRequest = null;
				//TODO: We could get Name Resolution Failure if there's no connection, how to handle?
			}
		}

		void OnUpdate()
		{
			if(getOnlineFormulasResponse != null)
			{
				//OK
				if(getOnlineFormulasResponse.StatusCode == HttpStatusCode.OK)
				{
					var responseStreamString = new StreamReader(getOnlineFormulasResponse.GetResponseStream()).ReadToEnd();
					var repositoryContentsList = MiniJSON.Json.Deserialize(responseStreamString) as List<object>;
					responseStreamString = string.Empty;
					foreach(Dictionary<string,object> content in repositoryContentsList)
					{
						var downloadURL = content["download_url"] as string;
						var extension = Path.GetExtension(downloadURL);
						//If it's a .cs file
						if(string.Equals(extension, ".cs", StringComparison.InvariantCultureIgnoreCase))
						{
							var fileName = Path.GetFileName(downloadURL);
							var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(downloadURL);
							//Check to see if it exists in formulaDataStore
							var formula = formulaDataStore.FormulaData.Find(x => x.name == fileNameWithoutExtension);
							if(formula == null)
							{
								formula = new FormulaData();
								formula.name = fileNameWithoutExtension;
								formula.localFilePath = Constants.formulasFolderUnityPath + fileName;
								formula.downloadURL = downloadURL;
								formulaDataStore.FormulaData.Add(formula);
							}
							else
							{
								//If download URL doesn't match (can happen if file was copied to project, instead of downloaded)
								if(! string.Equals(formula.downloadURL, downloadURL))
								{
									//Update download url
									formula.downloadURL = downloadURL;
								}
							}
						}
					}

					//Update lastUpdateTime and dirty the formulaDataStore, then re-search and repaint
					formulaDataStore.lastUpdateTimeBinary = DateTime.UtcNow.ToBinary();
					EditorUtility.SetDirty(formulaDataStore);
					FilterBySearchText(searchText);
					this.Repaint();
				}
				//Not modified
				else if(getOnlineFormulasResponse.StatusCode == HttpStatusCode.NotModified)
				{

				}

				getOnlineFormulasResponse.Close();
				getOnlineFormulasResponse = null;
			}

			if(downloadFormulaResponse != null)
			{
				if(downloadFormulaResponse.StatusCode == HttpStatusCode.OK)
				{
					var responseStreamString = new StreamReader(downloadFormulaResponse.GetResponseStream()).ReadToEnd();
					var fi = new FileInfo(formulaBeingDownloaded.localFilePath);
					// Delete the file if it exists.
					if (fi.Exists) 
					{
						fi.Delete();
					}
					var bytes = System.Text.Encoding.Default.GetBytes(responseStreamString);
					// Create and write file
					using(var fs = fi.Create())
					{
						fs.Write(bytes, 0, bytes.Length);
					}
					formulaBeingDownloaded.downloadTimeUTCBinary = DateTime.UtcNow.ToBinary();
					formulaBeingDownloaded.updateCheckTimeUTCBinary = formulaBeingDownloaded.downloadTimeUTCBinary;
					EditorUtility.SetDirty(formulaDataStore);
					AssetDatabase.Refresh();
				}
				//Not modified
				else if(downloadFormulaResponse.StatusCode == HttpStatusCode.NotModified)
				{
					formulaBeingDownloaded.updateCheckTimeUTCBinary = DateTime.UtcNow.ToBinary();
				}

				formulasToDownload.Remove(formulaBeingDownloaded);
				formulaBeingDownloaded = null;

				downloadFormulaResponse.Close();
				downloadFormulaResponse = null;
			}

			//If there are formulas to be downloaded
			if(formulasToDownload.Count > 0)
			{
				//And there isn't already a WebRequest in progress
				if(webRequest == null)
				{
					formulaBeingDownloaded = formulasToDownload[formulasToDownload.Count - 1];
					var downloadURL = formulaBeingDownloaded.downloadURL;
					webRequest = WebRequest.Create(new Uri(downloadURL)) as HttpWebRequest;
					webRequest.UserAgent = "EditorFormulas";
					webRequest.Method = "GET";
					webRequest.IfModifiedSince = DateTime.FromBinary(formulaBeingDownloaded.downloadTimeUTCBinary);
					webRequest.BeginGetResponse(HandleAsync_DownloadFormula, null);
				}				
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
				else
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

			//Button is only enabled if parameters have been initialized
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
			GUILayout.Button(new GUIContent(niceName, niceName), GUILayout.MaxWidth(this.position.width - 30));
			GUI.enabled = guiEnabled;
			if(GUILayout.Button(downloadButtonGUIContent, GUILayout.MaxWidth(20), GUILayout.MaxHeight(18)))
			{
				DownloadFormula(formula);
			}

			GUILayout.EndHorizontal();
		}

		void DownloadFormula(FormulaData formula)
		{
			if(formulasToDownload.Contains(formula))
			{
				return;
			}
			formulasToDownload.Add(formula);
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

			//If it has been 5 minutes since last check, retrieve formulas from web
			if(webRequest != null)
			{
				//TODO
			}
		}

	}
}