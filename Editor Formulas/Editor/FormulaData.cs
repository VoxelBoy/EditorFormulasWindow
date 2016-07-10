using System;
using System.Reflection;

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
	//Only valid for downloaded formulas
	public MethodInfo methodInfo;
	public long downloadTimeUTCBinary;

	public DateTime DownloadTimeUTC
	{
		get
		{
			return DateTime.FromBinary(downloadTimeUTCBinary);
		}
	}
}