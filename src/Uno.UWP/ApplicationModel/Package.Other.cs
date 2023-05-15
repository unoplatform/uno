#if !(__IOS__ || __ANDROID__ || __MACOS__)
#nullable enable
using System;
using System.Reflection;
using System.Xml;
using Uno.Foundation.Logging;

namespace Windows.ApplicationModel;

public partial class Package
{
	private const string PackageManifestName = "Package.appxmanifest";

	private static Assembly? _entryAssembly;
	private string _displayName = "";
	private string _logo = "ms-appx://logo";
	private bool _manifestParsed;

	private bool GetInnerIsDevelopmentMode() => false;

	private DateTimeOffset GetInstallDate() => DateTimeOffset.Now;

	private string GetInstalledPath()
	{
		if (_entryAssembly?.Location is { Length: > 0 } location)
		{
			return global::System.IO.Path.GetDirectoryName(location) ?? "";
		}
		else if (AppContext.BaseDirectory is { Length: > 0 } baseDirectory)
		{
			return global::System.IO.Path.GetDirectoryName(baseDirectory) ?? "";
		}

		return Environment.CurrentDirectory;
	}

	public string DisplayName
	{
		get
		{
			TryParsePackageManifest();
			return _displayName;
		}
	}

	public Uri Logo
	{
		get
		{
			TryParsePackageManifest();
			return new Uri(_logo, UriKind.RelativeOrAbsolute);
		}
	}

	internal static void SetEntryAssembly(Assembly entryAssembly) => _entryAssembly = entryAssembly;

	private void TryParsePackageManifest()
	{
		if (_entryAssembly != null && !_manifestParsed)
		{
			var manifest = _entryAssembly.GetManifestResourceStream(PackageManifestName);

			if (manifest != null)
			{
				try
				{
					var doc = new XmlDocument();
					doc.Load(manifest);

					var nsmgr = new XmlNamespaceManager(doc.NameTable);
					nsmgr.AddNamespace("d", "http://schemas.microsoft.com/appx/manifest/foundation/windows10");

					_displayName = doc.SelectSingleNode("/d:Package/d:Properties/d:DisplayName", nsmgr)?.InnerText ?? "";
					_logo = doc.SelectSingleNode("/d:Package/d:Properties/d:Logo", nsmgr)?.InnerText ?? "";

					var idNode = doc.SelectSingleNode("/d:Package/d:Identity", nsmgr);

					if (idNode is not null)
					{
						Id.Name = idNode.Attributes?.GetNamedItem("Name")?.Value ?? "";

						var versionString = idNode.Attributes?.GetNamedItem("Version")?.Value ?? "";
						if (Version.TryParse(versionString, out var version))
						{
							Id.Version = new PackageVersion(version);
						}

						Id.Publisher = idNode.Attributes?.GetNamedItem("Publisher")?.Value ?? "";
					}

					_manifestParsed = true;
				}
				catch (Exception ex)
				{
					if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Error))
					{
						this.Log().Error($"Failed to read manifest [{PackageManifestName}]", ex);
					}
				}
			}
			else
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"Skipping manifest reading, unable to find [{PackageManifestName}]");
				}
			}
		}
	}
}
#endif
