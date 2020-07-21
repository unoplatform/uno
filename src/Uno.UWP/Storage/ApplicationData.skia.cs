using System;
using System.IO;

namespace Windows.Storage
{
	partial class ApplicationData 
	{
		private static string GetLocalCacheFolder()
			=> Path.GetTempPath();

		private static string GetTemporaryFolder()
			=> Path.GetTempPath();

		private static string GetLocalFolder()
			=> AppDomain.CurrentDomain.BaseDirectory;

		private static string GetRoamingFolder()
			=> Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

		private static string GetSharedLocalFolder()
			=> Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
	}
}
