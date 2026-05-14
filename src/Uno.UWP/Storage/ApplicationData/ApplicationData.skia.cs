#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Uno;
using Windows.ApplicationModel;

namespace Windows.Storage;

partial class ApplicationData
{
	private const string DistinguishedNameOrganizationPrefix = "O=";
	private const string DistinguishedNameCommonNamePrefix = "CN=";
	private const string LocalCacheFolderName = "LocalCache";
	private const string TemporaryFolderName = "TempState";
	private const string LocalFolderName = "LocalState";
	private const string RoamingFolderName = "RoamingState";
	private const string SettingsFolderName = "Settings";

	private static string? _appSpecificSubpath;

	internal Task EnablePersistenceAsync() => Task.CompletedTask;

	partial void InitializePartial() => WinRTFeatureConfiguration.ApplicationData.IsApplicationDataInitialized = true;

	private string GetLocalCacheFolder() =>
		EnsurePath(Path.Combine(GetLocalCacheFolderRootPath(), LocalCacheFolderName));

	private string GetTemporaryFolder() =>
		EnsurePath(Path.Combine(GetTemporaryFolderRootPath(), TemporaryFolderName));

	private string GetLocalFolder() =>
		EnsurePath(Path.Combine(GetApplicationDataFolderRootPath(), LocalFolderName));

	private string GetRoamingFolder() =>
		EnsurePath(Path.Combine(GetApplicationDataFolderRootPath(), RoamingFolderName));

	internal string GetSettingsFolderPath() =>
		EnsurePath(Path.Combine(GetApplicationDataFolderRootPath(), SettingsFolderName));

	private string EnsurePath(string path)
	{
		Directory.CreateDirectory(path);
		return path;
	}

	[MemberNotNull(nameof(_appSpecificSubpath))]
	private string GetAppSpecificSubPath()
	{
		if (_appSpecificSubpath is null)
		{
			if (!Package.IsManifestInitialized)
			{
				throw new InvalidOperationException("The Package.Id is not initialized yet.");
			}

			var appName = Package.Current.Id.Name;
			var appNameSafe = GetFileNameSafeString(appName);

			var publisherDistinguishedName = Package.Current.Id.Publisher;
			string? publisherName = null;
			if (!string.IsNullOrEmpty(publisherDistinguishedName))
			{
				var parts = publisherDistinguishedName.Split(',');
				if (parts.FirstOrDefault(p => p.StartsWith(DistinguishedNameOrganizationPrefix, StringComparison.OrdinalIgnoreCase)) is { } organizationPart)
				{
					publisherName = organizationPart.Substring(DistinguishedNameOrganizationPrefix.Length);
				}

				if (string.IsNullOrEmpty(publisherName) &&
					parts.FirstOrDefault(p => p.StartsWith(DistinguishedNameCommonNamePrefix, StringComparison.OrdinalIgnoreCase)) is { } commonNamePart)
				{
					publisherName = commonNamePart.Substring(DistinguishedNameCommonNamePrefix.Length);
				}
			}

			var publisherNameSafe = !string.IsNullOrEmpty(publisherName) ? GetFileNameSafeString(publisherName) : null;

			if (publisherNameSafe is not null)
			{
				_appSpecificSubpath = Path.Combine(publisherNameSafe, appNameSafe);
			}
			else
			{
				_appSpecificSubpath = appNameSafe;
			}
		}

		return _appSpecificSubpath;
	}

	private string GetTemporaryFolderRootPath()
	{
		if (WinRTFeatureConfiguration.ApplicationData.TemporaryFolderPathOverride is { } path)
		{
			return path;
		}

		return Path.Combine(Path.GetTempPath(), GetAppSpecificSubPath());
	}

	private string GetLocalCacheFolderRootPath()
	{
		if (WinRTFeatureConfiguration.ApplicationData.LocalCacheFolderPathOverride is { } path)
		{
			return path;
		}

		string? localCacheRootFolder = OperatingSystem.IsLinux() ?
			Environment.GetEnvironmentVariable("XDG_CACHE_HOME") :
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

		if (string.IsNullOrEmpty(localCacheRootFolder))
		{
			var userHomeFolderPath = GetUserHomeFolderPath();

			localCacheRootFolder = Path.Combine(userHomeFolderPath, ".cache");
		}

		return Path.Combine(localCacheRootFolder, GetAppSpecificSubPath());
	}

	private string GetApplicationDataFolderRootPath()
	{
		if (WinRTFeatureConfiguration.ApplicationData.ApplicationDataPathOverride is { } path)
		{
			return path;
		}

		string? applicationDataRootFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

		if (string.IsNullOrEmpty(applicationDataRootFolder))
		{
			applicationDataRootFolder = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
		}

		if (string.IsNullOrEmpty(applicationDataRootFolder))
		{
			var userHomeFolderPath = GetUserHomeFolderPath();

			applicationDataRootFolder = Path.Combine(userHomeFolderPath, ".local", "share");
		}

		return Path.Combine(applicationDataRootFolder, GetAppSpecificSubPath());
	}

	private static string GetUserHomeFolderPath()
	{
		var myDocumentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

		if (string.IsNullOrEmpty(myDocumentsFolder))
		{
			throw new InvalidOperationException(
				"The current environment does not have a user application data nor user documents folder set up. " +
				"Please use WinRTFeatureConfiguration.ApplicationData.ApplicationDataPathOverride and " +
				"WinRTFeatureConfiguration.ApplicationData.LocalCacheFolderPathOverride to override the default locations.");
		}

		return myDocumentsFolder;
	}

	private static string GetFileNameSafeString(string fileName)
	{
		foreach (char c in Path.GetInvalidFileNameChars())
		{
			fileName = fileName.Replace(c, '_');
		}
		return fileName;
	}
}
