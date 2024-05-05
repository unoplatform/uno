using System.Collections.Generic;

namespace Uno.Sdk.Models;

internal record ManifestGroup(string Group, string Version, string[] Packages, Dictionary<string, string>? VersionOverride = null);
