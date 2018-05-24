#if __ANDROID__
using System;
using System.IO;

namespace Windows.Storage
{
	public  partial class ApplicationData 
	{
		private static string GetLocalCacheFolder() 
			=> Android.App.Application.Context.CacheDir.AbsolutePath;

		private static string GetTemporaryFolder()
			=> Path.GetTempPath();

		private static string GetLocalFolder() 
			=> Android.App.Application.Context.FilesDir.AbsolutePath;
	}
}
#endif