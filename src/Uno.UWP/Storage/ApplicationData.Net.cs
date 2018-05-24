using System;
using System.IO;

namespace Windows.Storage
{
	public  partial class ApplicationData 
	{
		private static string GetLocalCacheFolder()
			=> Path.GetTempPath();

		private static string GetTemporaryFolder()
			=> Path.GetTempPath();

		private static string GetLocalFolder()
			=> AppDomain.CurrentDomain.BaseDirectory;
	}
}