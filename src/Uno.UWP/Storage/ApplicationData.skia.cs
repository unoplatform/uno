#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
	private const string SharedLocalFolderName = "SharedLocalState";
	private const string RoamingFolderName = "RoamingState";
	private const string SettingsFolderName = "Settings";

	private static string? _appSpecificSubpath;

	private string GetLocalCacheFolder() => EnsurePath(Path.Combine(GetApplicationDataFolderRootPath(), LocalCacheFolderName));

	private string GetTemporaryFolder() => EnsurePath(Path.Combine(GetTemporaryFolderRootPath(), TemporaryFolderName));

	private string GetLocalFolder() =>
		// Uses XDG_DATA_HOME on Unix: https://github.com/dotnet/runtime/blob/b5705587347d29d79cec830dc22b389e1ad9a9e0/src/libraries/System.Private.CoreLib/src/System/Environment.GetFolderPathCore.Unix.cs#L105
		EnsurePath(Path.Combine(GetApplicationDataFolderRootPath(), LocalFolderName));

	private string GetRoamingFolder() =>
		EnsurePath(Path.Combine(GetApplicationDataFolderRootPath(), RoamingFolderName));

	private string GetSharedLocalFolder() =>
		EnsurePath(Path.Combine(GetApplicationDataFolderRootPath(), SharedLocalFolderName));

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

	private string GetApplicationDataFolderRootPath()
	{
		if (WinRTFeatureConfiguration.ApplicationData.ApplicationDataPathOverride is { } path)
		{
			return path;
		}

		string applicationDataRootFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		if (string.IsNullOrEmpty(applicationDataRootFolder))
		{
			var myDocumentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			if (string.IsNullOrEmpty(myDocumentsFolder))
			{
				throw new InvalidOperationException(
					"The current environment does not have a user application data nor user documents folder set up. " +
					"Please use WinRTFeatureConfiguration.ApplicationData.ApplicationDataPathOverride to set your own.");
			}
			else
			{
				applicationDataRootFolder = Path.Combine(myDocumentsFolder, ".local", "share");
			}
		}

		return Path.Combine(applicationDataRootFolder, GetAppSpecificSubPath());
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
