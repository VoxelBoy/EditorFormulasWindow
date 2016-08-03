using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;
using System.Net.Security;

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
			public bool isUpdateCheck;
			public FormulaData formulaData;
			public HttpWebRequest request;
			public HttpWebResponse response;
		}

		public List<DownloadFormulaAction> downloadFormulaActions = new List<DownloadFormulaAction>();

		FormulaDataStore formulaDataStore;

		public event System.Action FormulaDataUpdated;

		List<string> debugMessagesFromOtherThreads = new List<string>();

		public bool DebugMode
		{
			get; set;
		}

		public void Init(FormulaDataStore formulaDataStore)
		{
			this.formulaDataStore = formulaDataStore;
		}

		void OnEnable()
		{
			DebugLog("Web Helper On Enable");
			EditorApplication.update += OnUpdate;
            //Required for HttpWebRequest to not complain about certificate errors
            ServicePointManager.ServerCertificateValidationCallback = (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) => true;
        }

        void OnDisable()
		{
			DebugLog("Web Helper On Disable");
			EditorApplication.update -= OnUpdate;
		}

		void OnUpdate()
		{
			//If there were debug messages from other threads, process them here
			if(debugMessagesFromOtherThreads.Count > 0)
			{
				for(int i=0; i<debugMessagesFromOtherThreads.Count; i++)
				{
					DebugLog(debugMessagesFromOtherThreads[i]);
				}
				debugMessagesFromOtherThreads.Clear();
			}

			ProcessGetOnlineFormulasResponse ();

			ProcessDownloadFormulaResponse ();

			//Periodically get online formulas
			if(getOnlineFormulasWebRequest == null && DateTime.UtcNow.Subtract(lastGetOnlineFormulasAttemptTime).TotalMinutes >= Constants.getOnlineFormulasFrequencyInMinutes)
			{
				DebugLog("Get online formulas again since it's been 5 minutes since we last attempted to get them.");
				GetOnlineFormulas();
			}

			//Periodically check for updates to formulas
			for(int i=0; i<formulaDataStore.FormulaData.Count; i++)
			{
				var formula = formulaDataStore.FormulaData[i];
				//If local file doesn't exist, skip formula
				if(! formula.localFileExists)
				{
					continue;
				}
				//If enough time has passed since last update check time for this formula, check again
				if(DateTime.UtcNow.Subtract(formula.UpdateCheckTimeUTC).TotalMinutes >= Constants.checkForFormulaUpdateFrequencyInMinutes)
				{
					DownloadFormula(formula, true);
				}
			}

			//If there was a request to dirty the formula store from another thread, do it here
			if(doDirtyFormulaDataStore)
			{
				doDirtyFormulaDataStore = false;
				EditorUtility.SetDirty(formulaDataStore);
			}
		}

		void ProcessGetOnlineFormulasResponse ()
		{
			if (getOnlineFormulasResponse != null || useLastGetOnlineFormulasResponse)
			{
				string responseStreamString = null;
				DebugLog ("Get online formulas response");
				if (useLastGetOnlineFormulasResponse)
				{
					DebugLog ("Using last response");
					useLastGetOnlineFormulasResponse = false;
					responseStreamString = formulaDataStore.lastGetOnlineFormulasResponse;
				}
				else if (getOnlineFormulasResponse.StatusCode == HttpStatusCode.OK)
				{
					DebugLog ("Got response with OK status code");
					responseStreamString = new StreamReader (getOnlineFormulasResponse.GetResponseStream ()).ReadToEnd ();
				}
				//Not modified is handled in try/catch so what's the issue here?
				else
				{
					Debug.Log ("Something went wrong in get online formulas response: " + getOnlineFormulasResponse.StatusCode);
					EditorUtility.SetDirty (formulaDataStore);
				}
				DebugLog ("Response in next line\n" + responseStreamString);
				if (getOnlineFormulasResponse != null)
				{
					getOnlineFormulasResponse.Close ();
					getOnlineFormulasResponse = null;
				}
				if (responseStreamString != null)
				{
					var repositoryContentsList = MiniJSON.Json.Deserialize (responseStreamString) as List<object>;
					//If json deserialized into a proper object
					if (repositoryContentsList != null)
					{
						//Set formulaDataStore.lastGetOnlineFormulasResponse to the response string
						formulaDataStore.lastGetOnlineFormulasResponse = responseStreamString;
					}
					foreach (Dictionary<string, object> content in repositoryContentsList)
					{
						var apiURL = content["url"] as string;
						var htmlURL = content["html_url"] as string;
						var downloadURL = content ["download_url"] as string;
						var extension = Path.GetExtension (downloadURL);
						//If it's a .cs file
						if (string.Equals (extension, ".cs", StringComparison.InvariantCultureIgnoreCase))
						{
							var fileName = Path.GetFileName (downloadURL);
							var fileNameWithoutExtension = Path.GetFileNameWithoutExtension (downloadURL);
							//Check to see if it exists in formulaDataStore
							var formula = formulaDataStore.FormulaData.Find (x => x.name == fileNameWithoutExtension);
							if (formula == null)
							{
								formula = new FormulaData ();
								formula.name = fileNameWithoutExtension;
								formula.projectFilePath = Constants.formulasFolderUnityPath + fileName;
								formula.localFileExists = new FileInfo (Utils.GetFullPathFromAssetsPath (formula.projectFilePath)).Exists;
								formula.downloadURL = downloadURL;
								formula.htmlURL = htmlURL;
								formula.apiURL = apiURL;
								formulaDataStore.FormulaData.Add (formula);
							}
							else
							{
								//If download URL doesn't match (can happen if file was copied to project, instead of downloaded)
								if (!string.Equals (formula.downloadURL, downloadURL))
								{
									//Update download url
									formula.downloadURL = downloadURL;
								}
							}
						}
					}
					//Update lastUpdateTime if we didn't use last response
					if (!useLastGetOnlineFormulasResponse)
					{
						formulaDataStore.LastUpdateTime = DateTime.UtcNow;
					}
					EditorUtility.SetDirty (formulaDataStore);
					if (FormulaDataUpdated != null)
					{
						FormulaDataUpdated ();
					}
				}
			}
		}

		void ProcessDownloadFormulaResponse ()
		{
			for (int i = downloadFormulaActions.Count - 1; i >= 0; i--)
			{
				var downloadFormulaAction = downloadFormulaActions [i];
				if (downloadFormulaAction.response != null)
				{
					DebugLog ("Got a response for a download formula action");
					var response = downloadFormulaAction.response;
					if (response.StatusCode == HttpStatusCode.OK)
					{
						DebugLog ("Status code OK");
						//If it was an update check
						if (downloadFormulaAction.isUpdateCheck)
						{
							DebugLog ("It was an update check, and there's an update");
							downloadFormulaAction.formulaData.updateAvailable = true;
							downloadFormulaAction.formulaData.UpdateCheckTimeUTC = DateTime.UtcNow;
							EditorUtility.SetDirty (formulaDataStore);
							if (FormulaDataUpdated != null)
							{
								FormulaDataUpdated ();
							}
						}
						//Not an update check, so write to file
						else
						{
							DebugLog ("It was an not update check, write to file");
							var responseStreamString = new StreamReader (response.GetResponseStream ()).ReadToEnd ();
							DebugLog ("Response in next line\n" + responseStreamString);
							var fi = new FileInfo (Utils.GetFullPathFromAssetsPath (downloadFormulaAction.formulaData.projectFilePath));
							// Delete the file if it exists.
							if (fi.Exists)
							{
								DebugLog ("Delete the already existing file at: " + fi.FullName);
								fi.Delete ();
							}
							var bytes = System.Text.Encoding.Default.GetBytes (responseStreamString);
							// Create and write file
							DebugLog ("Create and write file");
							using (var fs = fi.Create ())
							{
								fs.Write (bytes, 0, bytes.Length);
							}
							var now = DateTime.UtcNow;
							downloadFormulaAction.formulaData.DownloadTimeUTC = now;
							downloadFormulaAction.formulaData.UpdateCheckTimeUTC = now;
							downloadFormulaAction.formulaData.updateAvailable = false;
							EditorUtility.SetDirty (formulaDataStore);
							AssetDatabase.Refresh ();
							if (FormulaDataUpdated != null)
							{
								FormulaDataUpdated ();
							}
						}
					}
					//Not modified is handled in try/catch so what's the issue here?
					else
					{
						Debug.Log ("Soemthing went wrong in download formula response: " + response.StatusCode);
					}
					response.Close ();
					downloadFormulaActions.RemoveAt (i);
				}
			}
		}

		public void GetOnlineFormulas()
		{
			DebugLog("Get Online Formulas");
			if(GettingOnlineFormulas)
			{
				Debug.Log("Already getting online formulas");
				return;
			}
			getOnlineFormulasWebRequest = WebRequest.Create(new Uri(Constants.formulasRepoContentsURL)) as HttpWebRequest;
			getOnlineFormulasWebRequest.UserAgent = "EditorFormulas";
			getOnlineFormulasWebRequest.Method = "GET";
			getOnlineFormulasWebRequest.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
			//Only set IfModifiedSince if formulaDataStore.lastGetOnlineFormulasResponse is not null or empty
			if(!string.IsNullOrEmpty(formulaDataStore.lastGetOnlineFormulasResponse))
			{
				DebugLog("Setting IfModifiedSince header of getOnlineFormulasWebRequest to " + formulaDataStore.LastUpdateTime.ToString());
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
				debugMessagesFromOtherThreads.Add("Try getting getOnlineFormulas response");
				getOnlineFormulasResponse = getOnlineFormulasWebRequest.EndGetResponse(ar) as HttpWebResponse;
				getOnlineFormulasWebRequest = null;
			}
			catch (WebException ex)
			{
				debugMessagesFromOtherThreads.Add("Exception! " + ex.GetType().FullName);
				var response = ex.Response as HttpWebResponse;
				//Not modified
				if(response != null)
				{
                    if (response.StatusCode == HttpStatusCode.NotModified)
                    {
                        debugMessagesFromOtherThreads.Add("Not modified");
                        formulaDataStore.LastUpdateTime = DateTime.UtcNow;
                        useLastGetOnlineFormulasResponse = true;
                        doDirtyFormulaDataStore = true;
                        connectionProblem = false;
                    }
				    else
				    {
					    debugMessagesFromOtherThreads.Add("Something else: " + response.StatusCode);
					    connectionProblem = true;
				    }
				}
                else
                {
                    debugMessagesFromOtherThreads.Add("Response in Exception is null");
                }
				getOnlineFormulasWebRequest = null;
			}
		}

		public void DownloadFormula(FormulaData formulaData, bool isUpdateCheck)
		{
			DebugLog("Download Formula: " + formulaData.name);
			if(downloadFormulaActions.Any(x => x.formulaData == formulaData))
			{
				Debug.Log("Formula already being downloaded");
				return;
			}

			var downloadFormulaAction = new DownloadFormulaAction();
			downloadFormulaAction.formulaData = formulaData;
			downloadFormulaAction.isUpdateCheck = isUpdateCheck;
			if(isUpdateCheck)
			{
				formulaData.UpdateCheckTimeUTC = DateTime.UtcNow;
				EditorUtility.SetDirty(formulaDataStore);
			}
			var request = WebRequest.Create(new Uri(isUpdateCheck ? formulaData.apiURL : formulaData.downloadURL)) as HttpWebRequest;
			downloadFormulaAction.request = request;
			request.UserAgent = "EditorFormulas";
			request.Method = "GET";
			request.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
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
				debugMessagesFromOtherThreads.Add("Try getting downloadFormulaAction response");
				downloadFormulaAction.response = downloadFormulaAction.request.EndGetResponse(ar) as HttpWebResponse;
				downloadFormulaAction.request = null;
				connectionProblem = false;
			}
			catch (WebException ex)
			{
				debugMessagesFromOtherThreads.Add("Exception! " + ex.GetType().FullName);
				//TODO: We could get Name Resolution Failure if there's no connection, how to handle?
				var response = ex.Response as HttpWebResponse;
				//Not modified
				if(response.StatusCode == HttpStatusCode.NotModified)
				{
					debugMessagesFromOtherThreads.Add("Not modified");
					downloadFormulaAction.formulaData.UpdateCheckTimeUTC = DateTime.UtcNow;
					downloadFormulaAction.formulaData.updateAvailable = false;
					doDirtyFormulaDataStore = true;
					connectionProblem = false;
				}
				else
				{
					debugMessagesFromOtherThreads.Add("Something else: " + response.StatusCode);
					connectionProblem = true;
				}
				downloadFormulaAction.request = null;
				downloadFormulaActions.Remove(downloadFormulaAction);
			}
		}

		public void CheckForAllFormulaUpdates()
		{
			foreach(var formula in formulaDataStore.FormulaData)
			{
				//If local file doesn't exist, skip formula
				if(! formula.localFileExists)
				{
					continue;
				}
				DownloadFormula(formula, true);
			}
		}

		void DebugLog(string message)
		{
			if(DebugMode)
			{
				Debug.Log(message);
			}
		}
	}
}
