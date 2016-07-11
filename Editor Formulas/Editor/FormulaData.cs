using System;
using System.Reflection;

namespace EditorFormulas 
{
	[System.Serializable]
	public class FormulaData
	{
		//Should match method info name
		public string name;
		public string downloadURL;
		public string localFilePath;
		public bool IsUsable
		{
			get { return methodInfo != null; }
		}

		public long downloadTimeUTCBinary;

		[System.NonSerialized]
		public MethodInfo methodInfo;
		[System.NonSerialized]
		public bool updateAvailable = false;

		public long updateCheckTimeUTCBinary;
	}
}