using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Uno.Sdk.Models;

#nullable enable
internal readonly partial struct NuGetVersion : IComparable<NuGetVersion>
{
	private NuGetVersion(string originalVersion, Version version)
	{
		OriginalVersion = originalVersion;
		IsPreview = originalVersion.Contains('-');
		Version = version;
	}

	public bool IsPreview { get; }

	public Version Version { get; }

	public string OriginalVersion { get; }

	public override string ToString() => OriginalVersion;

	public static bool TryParse(string? originalVersion, out NuGetVersion nugetVersion)
	{
		nugetVersion = default;
		if (originalVersion is null || string.IsNullOrEmpty(originalVersion))
		{
			return false;
		}

		try
		{
			var match = VersionExpression().Match(originalVersion);
			if (!(match.Success && match.Groups.Count > 0))
			{
				return false;
			}

			var versionString = match.Groups[0].Value;
			if (!string.IsNullOrEmpty(versionString))
			{
				nugetVersion = new NuGetVersion(originalVersion, new Version(versionString));
				return true;
			}
		}
		catch
		{
			// Suppress any exceptions
		}

		return false;
	}

	public int CompareTo(NuGetVersion other)
	{
		var versionComparison = Version.CompareTo(other.Version);
		if (versionComparison != 0)
		{
			return versionComparison;
		}

		// If the numeric parts are equal, non-preview versions should come before preview versions
		var thisIsPreview = IsPreview;
		var otherIsPreview = other.IsPreview;
		if (thisIsPreview && !otherIsPreview)
		{
			return 1; // This instance is a preview, and should come after
		}
		else if (!thisIsPreview && otherIsPreview)
		{
			return -1; // The other instance is a preview, and should come after
		}

		// If both are previews or both are stable, maintain their order (consider them equal in terms of sorting)
		return 0;
	}

	[GeneratedRegex(@"^(\d+\.\d+(\.\d+)?(\.\d+)?)")]
	private static partial Regex VersionExpression();
}
