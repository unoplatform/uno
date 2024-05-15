using System;
using Foundation;
using System.IO;

namespace Windows.Storage;

partial class ApplicationData
{
	private const string SandboxContainerIdKey = "APP_SANDBOX_CONTAINER_ID";

	private static string GetLocalCacheFolder()
	{
		var url = NSFileManager.DefaultManager.GetUrls(NSSearchPathDirectory.CachesDirectory, NSSearchPathDomain.User)[0];
		return url.Path;
	}

	private static string GetTemporaryFolder()
		=> Path.GetTempPath();

	private static string GetLocalFolder()
	{
		var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		string applicationDataPath;
		if (IsAppSandboxed())
		{
			// In a sandbox, the home directory refers to the app's root directory
			applicationDataPath = Path.GetFullPath(
			Path.Combine(
				homeDirectory,
				"Data",
				"Application Support",
				"Local"));
		}
		else
		{
			applicationDataPath = Path.GetFullPath(
			Path.Combine(
				homeDirectory,
				"Library",
				"Application Support",
				NSBundle.MainBundle.BundleIdentifier,
				"Local"));
		}

		// Ensure LocalFolder directory exists
		Directory.CreateDirectory(applicationDataPath);
		return applicationDataPath;
	}

	private static string GetRoamingFolder()
		=> Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

	private static string GetSharedLocalFolder()
		=> Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

	private static bool IsAppSandboxed()
	{
		var environment = NSProcessInfo.ProcessInfo.Environment;
		return environment[SandboxContainerIdKey] != null;
	}
}
