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
		=> Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

	private static string GetSharedLocalFolder()
		=> Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

	internal static Android.Content.Context GetAndroidAppContext()
		=> Android.App.Application.Context
			?? throw new InvalidOperationException(
				"The Android Application context is not yet available. " +
				"You need to initialize Windows.UI.Xaml.NativeApplication using the constructor " +
				"with the Windows.UI.Xaml.NativeApplication.AppBuilder delegate.");
}
