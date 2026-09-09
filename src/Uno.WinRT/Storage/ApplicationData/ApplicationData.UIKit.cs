using System;
using Foundation;
using System.IO;
using System.Threading.Tasks;

namespace Windows.Storage;

partial class ApplicationData
{
	// Persistence needs no setup on this platform, but Application.StartPartial calls it unconditionally.
	internal Task EnablePersistenceAsync() => Task.CompletedTask;

	private static string GetLocalCacheFolder()
	{
		if (UIKit.UIDevice.CurrentDevice.CheckSystemVersion(8, 0))
		{
			var url = NSFileManager.DefaultManager.GetUrls(NSSearchPathDirectory.CachesDirectory, NSSearchPathDomain.User)[0];
			return url.Path;
		}
		else
		{
			var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			var folder = Path.GetFullPath(Path.Combine(documents, "..", "Library", "Caches"));
			Directory.CreateDirectory(folder);
			return folder;
		}
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
