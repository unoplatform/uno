using System;
using System.IO;

namespace Windows.Storage
{
	partial class ApplicationData
	{
		/// <summary>
		/// On Android, persistence is always enabled and requires no additional setup.
		/// This method is provided for cross-platform compatibility and returns a completed task.
		/// </summary>
		internal Task EnablePersistenceAsync() => Task.CompletedTask;

		private static string GetLocalCacheFolder()
			=> GetAndroidAppContext().CacheDir.AbsolutePath;

		private static string GetTemporaryFolder()
			=> Path.GetTempPath();

		private static string GetLocalFolder()
			=> GetAndroidAppContext().FilesDir.AbsolutePath;

		private static string GetRoamingFolder()
		{
			var p = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			Directory.CreateDirectory(p);
			return p;
		}

		private static string GetSharedLocalFolder()
			=> Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

		internal static Android.Content.Context GetAndroidAppContext()
			=> Android.App.Application.Context
				?? throw new InvalidOperationException(
					"The Android Application context is not yet available. " +
					"You need to initialize Windows.UI.Xaml.NativeApplication using the constructor " +
					"with the Windows.UI.Xaml.NativeApplication.AppBuilder delegate.");
	}
}
