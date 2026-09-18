#if !(__APPLE_UIKIT__ || __ANDROID__ || __TVOS__)
#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Xml;
using Uno.Foundation.Logging;

namespace Windows.ApplicationModel;

public partial class Package
{
	private const string PackageManifestName = "Package.appxmanifest";

	private static Assembly? _entryAssembly;
	private string _displayName = "";

	partial void InitializePlatform()
	{
	}

	internal static bool IsManifestInitialized { get; private set; }

	private bool GetInnerIsDevelopmentMode() => false;

	private DateTimeOffset GetInstallDate() => DateTimeOffset.Now;

	private string GetInstalledPath()
		=> GetAppInstallDirectory(_entryAssembly) ?? Environment.CurrentDirectory;

	internal static string? GetAppInstallDirectory(Assembly? appAssembly)
	{
		if (GetAssemblyLocation(appAssembly) is string location &&
				global::System.IO.Path.GetDirectoryName(location) is string directory)
		{
			return directory;
		}
		else if (AppContext.BaseDirectory is { Length: > 0 } baseDirectory)
		{
			return global::System.IO.Path.GetDirectoryName(baseDirectory) ?? "";
		}

		return null;

		// "Assembly.Location.get' always returns an empty string for assemblies embedded in a single-file app."
		// We check the return value; it should be safe to ignore this.
		[UnconditionalSuppressMessage("Trimming", "IL3000", Justification = "On an empty string, we fallback to other locations.")]
		static string? GetAssemblyLocation(Assembly? assembly)
		{
			var loc = assembly?.Location;
			if (!string.IsNullOrEmpty(loc))
			{
				return loc;
			}
			return null;
		}
	}

	public string DisplayName
	{
		get => EnsureLocalized(_displayName);
		private set => _displayName = value;
	}

	public Uri? Logo { get; set; }

	internal static void SetEntryAssembly(Assembly entryAssembly)
	{
		_entryAssembly = entryAssembly;
		var assemblyName = entryAssembly.GetName();
		Current.Id.Name = assemblyName.Name; // Set the package name to the entry assembly name by default.
		if (assemblyName.Version is not null)
		{
			Current.Id.Version = new PackageVersion(assemblyName.Version);
		}
		Current.ParsePackageManifest();
		IsManifestInitialized = true;
	}

	internal static string EnsureLocalized(string stringToLocalize)
	{
		if (stringToLocalize.StartsWith("ms-resource:", StringComparison.OrdinalIgnoreCase))
		{
			var resourceKey = stringToLocalize["ms-resource:".Length..].Trim();
			var resourceString = Resources.ResourceLoader.GetForViewIndependentUse().GetString(resourceKey);

			if (!string.IsNullOrEmpty(resourceString))
			{
				stringToLocalize = resourceString;
			}
		}

		return stringToLocalize;
	}

	private void ParsePackageManifest()
	{
		if (_entryAssembly is null)
		{
			return;
		}

		if (_entryAssembly.GetManifestResourceStream(PackageManifestName) is not { } manifest)
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Skipping manifest reading, unable to find [{PackageManifestName}]");
			}

			return;
		}

		try
		{
			var doc = new XmlDocument();
			doc.Load(manifest);

			var nsmgr = new XmlNamespaceManager(doc.NameTable);
			nsmgr.AddNamespace("d", "http://schemas.microsoft.com/appx/manifest/foundation/windows10");

			DisplayName = doc.SelectSingleNode("/d:Package/d:Properties/d:DisplayName", nsmgr)?.InnerText ?? "";

#if __SKIA__
			Description = doc.SelectSingleNode("/d:Package/d:Properties/d:Description", nsmgr)?.InnerText ?? "";
			PublisherDisplayName = doc.SelectSingleNode("/d:Package/d:Properties/d:PublisherDisplayName", nsmgr)?.InnerText ?? "";
#endif

			var logoUri = doc.SelectSingleNode("/d:Package/d:Properties/d:Logo", nsmgr)?.InnerText ?? "";
			if (Uri.TryCreate(logoUri, UriKind.RelativeOrAbsolute, out var logo))
			{
				Logo = logo;
			}

			var idNode = doc.SelectSingleNode("/d:Package/d:Identity", nsmgr);
			if (idNode is not null)
			{
				Id.Name = idNode.Attributes?.GetNamedItem("Name")?.Value ?? "";

				// By default we use the entry assembly version, which is usually set by the <AssemblyDisplayVersion> MSBuild property.
				// If not set yet, we try to get the version from the manifest instead.
				if (Id.Version == default)
				{
					var versionString = idNode.Attributes?.GetNamedItem("Version")?.Value ?? "";
					if (Version.TryParse(versionString, out var version))
					{
						Id.Version = new PackageVersion(version);
					}
				}

				Id.Publisher = idNode.Attributes?.GetNamedItem("Publisher")?.Value ?? "";
			}
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Error))
			{
				this.Log().Error($"Failed to read manifest [{PackageManifestName}]", ex);
			}
		}
	}
}
#endif
