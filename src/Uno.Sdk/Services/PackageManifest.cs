using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Uno.Sdk.Models;

namespace Uno.Sdk.Services;

internal class PackageManifest
{
	private static readonly ManifestGroup[] _defaultManifest;

	static PackageManifest()
	{
		var location = Path.GetDirectoryName(typeof(PackageManifest).Assembly.Location);
		IEnumerable<ManifestGroup> manifest = [];
		if (!string.IsNullOrEmpty(location))
		{
			// Disable warning: we only do this once
#pragma warning disable CA1869 // Cache and reuse 'JsonSerializerOptions' instances
			var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
#pragma warning restore CA1869 // Cache and reuse 'JsonSerializerOptions' instances
			var path = Path.Combine(location, "packages.json");
			var json = File.ReadAllText(path);
			manifest = JsonSerializer.Deserialize<IEnumerable<ManifestGroup>>(json, options) ?? [];
		}

		_defaultManifest = manifest.ToArray();
	}

	private readonly TaskLoggingHelper _log;

	public PackageManifest(TaskLoggingHelper log)
	{
		_log = log;

		Manifest = new List<ManifestGroup>(_defaultManifest);

		var unoVersion = GetGroupVersion(Group.Core);
		if (string.IsNullOrEmpty(unoVersion))
		{
			// This should never happen.
			throw new InvalidOperationException("No Uno Version was set.");
		}

		UnoVersion = unoVersion;
	}

	public string UnoVersion { get; }

	public List<ManifestGroup> Manifest { get; private set; }

	public string? GetPackageVersion(string packageId, string targetFrameworkVersion, string? userSpecifiedVersion)
	{
		if (!string.IsNullOrEmpty(userSpecifiedVersion))
		{
			_log.LogMessage(MessageImportance.Low, "Using user specified package version '{0}' for the package '{1}'.", userSpecifiedVersion, packageId);
			return userSpecifiedVersion;
		}

		var group = _defaultManifest.SingleOrDefault(group => group.Packages.Any(p => p.Equals(packageId, System.StringComparison.InvariantCultureIgnoreCase)));
		if (group is null)
		{
			_log.LogMessage(MessageImportance.Normal, "Could not locate a package version for the package '{0}'.", packageId);
			return null;
		}

		return group.VersionOverride is not null && group.VersionOverride.TryGetValue(targetFrameworkVersion, out var versionOverride) ? versionOverride : group.Version;
	}

	public PackageManifest UpdateManifest(string groupName, string? version)
	{
		if (groupName.Equals(Group.Core, StringComparison.InvariantCultureIgnoreCase))
		{
			throw new InvalidOperationException("You cannot override the Core Package group.");
		}

		if (!string.IsNullOrEmpty(version))
		{
			if (Manifest.Any(x => x.Group.Equals(groupName, StringComparison.InvariantCultureIgnoreCase)))
			{
				var group = Manifest.Single(x => x.Group.Equals(groupName, StringComparison.InvariantCultureIgnoreCase));
				var updated = group with { Version = version! };
				Manifest.Remove(group);
				Manifest.Add(updated);
			}
			else
			{
				throw new InvalidOperationException($"No Manifest Group '{groupName}' has been added to the current Package Manifest.");
			}
		}

		return this;
	}

	public PackageManifest AddManifestGroup(string groupName, string? version, params string[] packages)
	{
		// This needs to work even when no version is specified
		if (!string.IsNullOrEmpty(version))
		{
			if (Manifest.Any(x => x.Group.Equals(groupName, StringComparison.InvariantCultureIgnoreCase)))
			{
				throw new InvalidOperationException($"Cannot add the Manifest Group '{groupName}' as it already exists in the Package Manifest");
			}

			Manifest.Add(new ManifestGroup(groupName, version!, packages));
		}

		return this;
	}

	public string? GetPackageVersion(string packageId) =>
		Manifest.SingleOrDefault(group => group.Packages.Any(p => p.Equals(packageId, StringComparison.InvariantCulture)))
			?.Version;

	private string? GetGroupVersion(string groupName)
	{
		var group = Manifest.SingleOrDefault(x => x.Group.Equals(groupName, StringComparison.InvariantCultureIgnoreCase));
		return group?.Group;
	}

	public class Group
	{
		public const string Core = nameof(Core);
		public const string Extensions = nameof(Extensions);
		public const string Themes = nameof(Themes);
		public const string Toolkit = nameof(Toolkit);
		public const string CSharpMarkup = nameof(CSharpMarkup);
		public const string WasmBootstrap = nameof(WasmBootstrap);
		public const string OSLogging = nameof(OSLogging);
		public const string CoreLogging = nameof(CoreLogging);
		public const string UniversalImageLoading = nameof(UniversalImageLoading);
		public const string Dsp = nameof(Dsp);
		public const string Resizetizer = nameof(Resizetizer);
		public const string SkiaSharp = nameof(SkiaSharp);
		public const string WinAppSdk = nameof(WinAppSdk);
		public const string WinAppSdkBuildTools = nameof(WinAppSdkBuildTools);
		public const string MicrosoftLoggingConsole = nameof(MicrosoftLoggingConsole);
		public const string WindowsCompatibility = nameof(WindowsCompatibility);
		public const string MsalClient = nameof(MsalClient);
		public const string Mvvm = nameof(Mvvm);
		public const string Prism = nameof(Prism);
		public const string AndroidMaterial = nameof(AndroidMaterial);
		public const string AndroidXLegacySupportV4 = nameof(AndroidXLegacySupportV4);
		public const string AndroidXAppCompat = nameof(AndroidXAppCompat);
		public const string AndroidXRecyclerView = nameof(AndroidXRecyclerView);
		public const string AndroidXActivity = nameof(AndroidXActivity);
		public const string AndroidXBrowser = nameof(AndroidXBrowser);
		public const string AndroidXSwipeRefreshLayout = nameof(AndroidXSwipeRefreshLayout);
		public const string AndroidXNavigation = nameof(AndroidXNavigation);
		public const string AndroidXCollection = nameof(AndroidXCollection);
		public const string Maui = nameof(Maui);
	}
}
