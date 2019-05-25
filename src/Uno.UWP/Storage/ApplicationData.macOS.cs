#if __MACOS__
using System;
using Foundation;
using System.IO;

namespace Windows.Storage
{
	partial class ApplicationData 
	{
		private static string GetLocalCacheFolder()
		{
			var url = NSFileManager.DefaultManager.GetUrls(NSSearchPathDirectory.CachesDirectory, NSSearchPathDomain.User)[0];
			return url.Path;
		}

		private static string GetTemporaryFolder() 
			=> Path.GetTempPath();

		private static string GetLocalFolder()
		{
			var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			var folder = Path.GetFullPath(Path.Combine(documents, "..", "Library", "Data"));
			Directory.CreateDirectory(folder);
			return folder;
		}

		private static string GetRoamingFolder()
			=> Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

		private static string GetSharedLocalFolder()
			=> Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

	}
}
#endif
