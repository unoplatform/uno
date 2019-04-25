#if __ANDROID__
using System;
using System.IO;

namespace Windows.Storage
{
	partial class ApplicationData 
	{
		private static string GetLocalCacheFolder() 
			=> Android.App.Application.Context.CacheDir.AbsolutePath;

		private static string GetTemporaryFolder()
			=> Path.GetTempPath();

		private static string GetLocalFolder() 
			=> Android.App.Application.Context.FilesDir.AbsolutePath;

		private static string GetRoamingFolder()
			=> Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

		private static string GetSharedLocalFolder()
			=> Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
	}
}
#endif
