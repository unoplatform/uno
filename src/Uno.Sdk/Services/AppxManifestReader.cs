using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;
using Uno.Sdk.Tasks;

namespace Uno.Sdk.Services;
internal class AppxManifestReader
{
	public AppxManifestReader(string path, ISdkTask task)
	{
		if (!File.Exists(path))
		{
			task.LogError($"The file '{path}' was not found.");
			Name = string.Empty;
			Version = string.Empty;
			DisplayName = string.Empty;
			PublisherDisplayName = string.Empty;
			Logo = string.Empty;
			return;
		}

		var appx = XDocument.Load(path);
		var xmlns = appx.Root!.GetDefaultNamespace();

		var xidentity = xmlns + "Identity";
		var identity = appx.Root.Element(xidentity);

		if (identity is null)
		{
			task.LogError("The Identity node was not found in the Package.appxmanifest.");
			Name = string.Empty;
			Version = string.Empty;
			DisplayName = string.Empty;
			PublisherDisplayName = string.Empty;
			Logo = string.Empty;
			return;
		}

		Name = identity.Attribute("Name")?.Value ?? string.Empty;
		Version = identity.Attribute("Version")?.Value ?? string.Empty;

		var xproperties = xmlns + "Properties";
		var properties = appx.Root.Element(xproperties);

		if (properties is null)
		{
			task.LogError("The Properties node could not be found in the Package.appxmanifest.");
			DisplayName = string.Empty;
			PublisherDisplayName = string.Empty;
			Logo = string.Empty;
			return;
		}

		var xdisplayname = xmlns + "DisplayName";
		var displayName = properties.Element(xdisplayname);
		DisplayName = displayName.Value;

		var xpublisherDisplayName = xmlns + "PublisherDisplayName";
		var publisherDisplayName = properties.Element(xpublisherDisplayName);
		PublisherDisplayName = publisherDisplayName.Value;

		var xLogo = xmlns + "Logo";
		var logo = properties.Element(xLogo);
		Logo = logo.Value;
	}

	public string Name { get; }

	public string DisplayName { get; }

	public string PublisherDisplayName { get; }

	public string Version { get; }

	public string Logo { get; }

	public bool Loaded => !string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(DisplayName) &&
			!string.IsNullOrEmpty(PublisherDisplayName) && !string.IsNullOrEmpty(Version) &&
			!string.IsNullOrEmpty(Logo);
}
