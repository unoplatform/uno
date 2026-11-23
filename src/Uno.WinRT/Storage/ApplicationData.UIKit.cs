using System;
using Foundation;
using System.IO;
using System.Threading.Tasks;

namespace Windows.Storage
{
	partial class ApplicationData
	{
		private static string GetLocalCacheFolder()
			=> NSFileManager.DefaultManager.GetUrls(NSSearchPathDirectory.CachesDirectory, NSSearchPathDomain.User)[0].Path;

		/// <summary>
		/// On this platform, persistence is always enabled or not applicable, so this method is a no-op.
		/// Returns a completed task to satisfy the cross-platform interface.
		/// </summary>
		internal Task EnablePersistenceAsync() => Task.CompletedTask;

		private static string GetTemporaryFolder()
			=> Path.GetTempPath();

		private static string GetLocalFolder()
		{
			var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			return Path.GetFullPath(Path.Combine(documents, "..", "Library", "Data"));
		}

		private static string GetRoamingFolder()
			=> Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

		private static string GetSharedLocalFolder()
			=> Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

	}
}
