using System;
using System.IO;

namespace Windows.Storage;

partial class ApplicationData
{
	private static string GetLocalCacheFolder()
		=> GetAndroidAppContext().CacheDir.AbsolutePath;

	private static string GetTemporaryFolder()
		=> Path.GetTempPath();

	private static string GetLocalFolder()
		=> GetAndroidAppContext().FilesDir.AbsolutePath;

	private static string GetRoamingFolder()
	{
		var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		Directory.CreateDirectory(path);
		return path;
	}

	private static string GetSharedLocalFolder()
		=> Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

	internal static Android.Content.Context GetAndroidAppContext()
		=> Android.App.Application.Context
			?? throw new InvalidOperationException(
				"The Android Application context is not yet available. " +
				"Your Android head must declare a Microsoft.UI.Xaml.NativeApplication subclass, " +
				"marked with [Application], overriding CreateHost().");
}
