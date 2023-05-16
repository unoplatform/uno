#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
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

	private static string _appSpecificSubpath = null!;

	partial void PartialCtor() => EnsureAppSubpath();

	private string GetLocalCacheFolder() => EnsurePath(Path.Combine(Path.GetTempPath(), _appSpecificSubpath, LocalCacheFolderName));

	private string GetTemporaryFolder() => EnsurePath(Path.Combine(Path.GetTempPath(), _appSpecificSubpath, TemporaryFolderName));

	private string GetLocalFolder() =>
		// Uses XDG_DATA_HOME on Unix: https://github.com/dotnet/runtime/blob/b5705587347d29d79cec830dc22b389e1ad9a9e0/src/libraries/System.Private.CoreLib/src/System/Environment.GetFolderPathCore.Unix.cs#L105
		EnsurePath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), _appSpecificSubpath, LocalFolderName));

	private string GetRoamingFolder() =>
		EnsurePath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), _appSpecificSubpath, RoamingFolderName));

	private string GetSharedLocalFolder() =>
		EnsurePath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), _appSpecificSubpath, SharedLocalFolderName));

	internal string GetSettingsFolderPath() =>
		EnsurePath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), _appSpecificSubpath, SettingsFolderName));

	private string EnsurePath(string path)
	{
		Directory.CreateDirectory(path);
		return path;
	}

	[MemberNotNull(nameof(_appSpecificSubpath))]
	private static void EnsureAppSubpath()
	{
		if (_appSpecificSubpath is not null)
		{
			return;
		}

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

	private static string GetFileNameSafeString(string fileName)
	{
		foreach (char c in Path.GetInvalidFileNameChars())
		{
			fileName = fileName.Replace(c, '_');
		}
		return fileName;
	}
}
