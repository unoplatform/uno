using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Uno.Sdk.Models;

#nullable enable
internal readonly struct NuGetVersion
{
	public NuGetVersion(string version)
	{
		OriginalVersion = version;
		var match = Regex.Match(version, @"^(\d+\.\d+(\.\d+)?(\.\d+)?)");
		if (match.Success)
		{
			var versionString = match.Groups[0].Value;
			Version = new Version(versionString);
		}
		else
		{
			Version = new Version();
		}
	}

	public bool IsPreview => OriginalVersion.Contains('-');

	public Version Version { get; }

	public string OriginalVersion { get; }

	public override string ToString() => OriginalVersion;

	public static bool TryParse(string? version, out NuGetVersion nugetVersion)
	{
		nugetVersion = default;
		if (string.IsNullOrEmpty(version))
		{
			return false;
		}

		try
		{
			nugetVersion = new NuGetVersion(version!);
		}
		catch
		{
			return false;
		}

		return true;
	}
}
