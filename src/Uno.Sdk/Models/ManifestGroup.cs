using System.Collections.Generic;

#nullable enable
namespace Uno.Sdk.Models;

internal record ManifestGroup(string Group, string Version, string[] Packages, Dictionary<string, string>? VersionOverride = null);

#nullable disable
