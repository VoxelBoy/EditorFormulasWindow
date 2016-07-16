using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace EditorFormulas
{
	public class WebHelper : ScriptableObject {

		HttpWebRequest getOnlineFormulasWebRequest;
		HttpWebResponse getOnlineFormulasResponse;

		DateTime lastGetOnlineFormulasAttemptTime;

		public bool connectionProblem = false;

		bool doDirtyFormulaDataStore = false;
		bool useLastGetOnlineFormulasResponse = false;

		public bool GettingOnlineFormulas
		{
			get { return getOnlineFormulasWebRequest != null; }
		}

		public bool DownloadingFormula
		{
			get { return downloadFormulaActions.Count > 0; }
		}

		public bool IsDownloadingFormula(FormulaData formulaData)
		{
			return downloadFormulaActions.Any(x => x.formulaData == formulaData);
		}

		public class DownloadFormulaAction
		{
			public FormulaData formulaData;
			public HttpWebRequest request;
			public HttpWebResponse response;
		}

		public List<DownloadFormulaAction> downloadFormulaActions = new List<DownloadFormulaAction>();

		FormulaDataStore formulaDataStore;

		public event System.Action FormulaDataUpdated;

		public void Init(FormulaDataStore formulaDataStore)
		{
			this.formulaDataStore = formulaDataStore;
		}

		void OnEnable()
		{
			EditorApplication.update += OnUpdate;
		}

		void OnDisable()
		{
			EditorApplication.update -= OnUpdate;
		}

		void OnUpdate()
		{
			//Check whether we need to process getOnlineFormulasResponse
			if(getOnlineFormulasResponse != null || useLastGetOnlineFormulasResponse)
			{
				string responseStreamString = null;
				if(useLastGetOnlineFormulasResponse)
				{
					useLastGetOnlineFormulasResponse = false;
					responseStreamString = formulaDataStore.lastGetOnlineFormulasResponse;
				}
				else if(getOnlineFormulasResponse.StatusCode == HttpStatusCode.OK)
				{
					responseStreamString = new StreamReader(getOnlineFormulasResponse.GetResponseStream()).ReadToEnd();
				}
				//Not modified is handled in try/catch so what's the issue here?
				else
				{
					Debug.Log(getOnlineFormulasResponse.StatusCode);
					formulaDataStore.LastUpdateTime = DateTime.UtcNow;
					EditorUtility.SetDirty(formulaDataStore);
				}

				if(getOnlineFormulasResponse != null)
				{
					getOnlineFormulasResponse.Close();
					getOnlineFormulasResponse = null;
				}

				if(responseStreamString != null)
				{
					var repositoryContentsList = MiniJSON.Json.Deserialize(responseStreamString) as List<object>;
					//If json deserialized into a proper object
					if(repositoryContentsList != null)
					{
						//Set formulaDataStore.lastGetOnlineFormulasResponse to the response string
						formulaDataStore.lastGetOnlineFormulasResponse = responseStreamString;
					}
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
								formula.projectFilePath = Constants.formulasFolderUnityPath + fileName;
								formula.localFileExists = new FileInfo(Utils.GetFullPathFromAssetsPath(formula.projectFilePath)).Exists;
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
					formulaDataStore.LastUpdateTime = DateTime.UtcNow;
					EditorUtility.SetDirty(formulaDataStore);

					if(FormulaDataUpdated != null)
					{
						FormulaDataUpdated();
					}
				}
			}

			//If it's been more than 5 minutes since last attempt to get online formulas, check again
			if(getOnlineFormulasWebRequest == null && DateTime.UtcNow.Subtract(lastGetOnlineFormulasAttemptTime).Minutes >= 5)
			{
				GetOnlineFormulas();
			}

			for(int i = downloadFormulaActions.Count - 1; i >= 0; i--)
			{
				var downloadFormulaAction = downloadFormulaActions[i];
				if(downloadFormulaAction.response != null)
				{
					var response = downloadFormulaAction.response;
					if(response.StatusCode == HttpStatusCode.OK)
					{
						var responseStreamString = new StreamReader(response.GetResponseStream()).ReadToEnd();
						var fi = new FileInfo(Utils.GetFullPathFromAssetsPath(downloadFormulaAction.formulaData.projectFilePath));
						// Delete the file if it exists.
						if (fi.Exists) 
						{
							fi.Delete();
						}
						var bytes = System.Text.Encoding.Default.GetBytes(responseStreamString);
						// Create and write file
						//TODO: Can get Unauthorized access exception if we delete a formula and
						//try to download a formula before compilation is finished
						using(var fs = fi.Create())
						{
							fs.Write(bytes, 0, bytes.Length);
						}
						var now = DateTime.UtcNow;
						downloadFormulaAction.formulaData.DownloadTimeUTC = now;
						downloadFormulaAction.formulaData.UpdateCheckTimeUTC = now;
						EditorUtility.SetDirty(formulaDataStore);
						AssetDatabase.Refresh();
					}
					//Not modified is handled in try/catch so what's the issue here?
					else
					{
						Debug.Log(response.StatusCode);
					}

					response.Close();
					downloadFormulaActions.RemoveAt(i);
				}
			}

			if(doDirtyFormulaDataStore)
			{
				doDirtyFormulaDataStore = false;
				EditorUtility.SetDirty(formulaDataStore);
			}
		}

		public void GetOnlineFormulas()
		{
			if(GettingOnlineFormulas)
			{
				Debug.Log("Already getting online formulas");
				return;
			}
			getOnlineFormulasWebRequest = WebRequest.Create(new Uri(Constants.formulasRepoContentsURL)) as HttpWebRequest;
			getOnlineFormulasWebRequest.UserAgent = "EditorFormulas";
			getOnlineFormulasWebRequest.Method = "GET";
			//Only set IfModifiedSince if formulaDataStore.lastGetOnlineFormulasResponse is not null or empty
			if(!string.IsNullOrEmpty(formulaDataStore.lastGetOnlineFormulasResponse))
			{
				getOnlineFormulasWebRequest.IfModifiedSince = formulaDataStore.LastUpdateTime;
			}
			getOnlineFormulasWebRequest.BeginGetResponse(HandleAsync_GetOnlineFormulas, null);
			lastGetOnlineFormulasAttemptTime = DateTime.UtcNow;
		}

		void HandleAsync_GetOnlineFormulas (IAsyncResult ar)
		{
			if(!ar.IsCompleted)
			{
				return;
			}

			try
			{
				getOnlineFormulasResponse = getOnlineFormulasWebRequest.EndGetResponse(ar) as HttpWebResponse;
				getOnlineFormulasWebRequest = null;
			}
			catch (WebException ex)
			{
				var response = ex.Response as HttpWebResponse;
				//Not modified
				if(response.StatusCode == HttpStatusCode.NotModified)
				{
					formulaDataStore.LastUpdateTime = DateTime.UtcNow;
					useLastGetOnlineFormulasResponse = true;
					doDirtyFormulaDataStore = true;
					connectionProblem = false;
				}
				else
				{
					connectionProblem = true;
				}
				getOnlineFormulasWebRequest = null;
			}
		}

		public void DownloadFormula(FormulaData formulaData)
		{
			if(downloadFormulaActions.Any(x => x.formulaData == formulaData))
			{
				Debug.Log("Formula already being downloaded");
				return;
			}

			var downloadFormulaAction = new DownloadFormulaAction();
			downloadFormulaAction.formulaData = formulaData;
			var downloadURL = formulaData.downloadURL;
			var request = WebRequest.Create(new Uri(downloadURL)) as HttpWebRequest;
			downloadFormulaAction.request = request;
			request.UserAgent = "EditorFormulas";
			request.Method = "GET";
			request.IfModifiedSince = formulaData.DownloadTimeUTC;
			request.BeginGetResponse(HandleAsync_DownloadFormula, downloadFormulaAction);

			downloadFormulaActions.Add(downloadFormulaAction);
		}

		void HandleAsync_DownloadFormula (IAsyncResult ar)
		{
			if(!ar.IsCompleted)
			{
				return;
			}

			var downloadFormulaAction = ar.AsyncState as DownloadFormulaAction;
			try
			{
				downloadFormulaAction.response = downloadFormulaAction.request.EndGetResponse(ar) as HttpWebResponse;
				downloadFormulaAction.request = null;
				connectionProblem = false;
			}
			catch (WebException ex)
			{
				//TODO: We could get Name Resolution Failure if there's no connection, how to handle?
				var response = ex.Response as HttpWebResponse;
				//Not modified
				if(response.StatusCode == HttpStatusCode.NotModified)
				{
					downloadFormulaAction.formulaData.updateCheckTimeUTCBinary = DateTime.UtcNow.ToBinary();
					connectionProblem = false;
				}
				else
				{
					connectionProblem = true;
				}
				downloadFormulaAction.request = null;
			}
		}
	}
}
