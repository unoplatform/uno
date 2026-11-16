using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Uno.UI.RemoteControl.VS.Helpers;

/// <summary>
/// Picks the Dev Server host that <see cref="EntryPoint"/> launches from the
/// <c>Uno.WinUI.DevServer</c> package's <c>tools/rc/host/net{X}.{Y}</c> folders.
///
/// <para>
/// The package only ships hosts for the .NET versions its Uno release was built with
/// (Uno 6.x: <c>net9.0</c> and <c>net10.0</c>). When the default .NET SDK is newer
/// (Visual Studio 18.12 installs the .NET 11 SDK), there is no exact match. Asking for
/// <c>net11.0</c> anyway made the start fail and retry every 5 s. So, like the CLI
/// (<c>UnoToolsLocator.TryResolveHostTfm</c>, unoplatform/uno#23123), we take the
/// highest host at or below the SDK's version and at most one major below it, and
/// the caller sets <c>DOTNET_ROLL_FORWARD=Major</c> for a major fallback so the host
/// also starts on a machine that only has the newer runtime.
/// </para>
///
/// <para>
/// This mirrors the CLI rules instead of sharing its file because the CLI code uses
/// span APIs that net48 (the Visual Studio extension) doesn't have.
/// </para>
/// </summary>
internal static class DevServerHostLocator
{
	internal const string HostAssemblyName = "Uno.UI.RemoteControl.Host.dll";

	// DOTNET_ROLL_FORWARD=Major allows one major jump: a host two majors below the
	// runtime would be picked but could not be loaded.
	private const int MaxMajorGap = 1;

	/// <summary>
	/// Returns the host to launch for the given .NET SDK major version, or <c>null</c>
	/// when the package has no compatible host.
	/// </summary>
	/// <param name="hostRoot">The package's <c>tools/rc/host</c> folder.</param>
	/// <param name="dotnetMajorVersion">The major version of <c>dotnet --version</c> in the solution folder.</param>
	internal static (string HostPath, bool RequiresMajorRollForward)? TryResolve(string hostRoot, int dotnetMajorVersion)
	{
		var requestedTfm = string.Format(CultureInfo.InvariantCulture, "net{0}.0", dotnetMajorVersion);
		var resolvedTfm = TryResolveHostTfm(EnumerateHostTfms(hostRoot), requestedTfm);
		if (resolvedTfm is null)
		{
			return null;
		}

		var hostPath = Path.Combine(hostRoot, resolvedTfm, HostAssemblyName);
		if (!File.Exists(hostPath) || !TryParseNetTfm(resolvedTfm, out var resolvedMajor, out _))
		{
			return null;
		}

		return (hostPath, resolvedMajor < dotnetMajorVersion);
	}

	/// <summary>
	/// Lists the <c>net{X}.{Y}</c> folders under <paramref name="hostRoot"/>, sorted by version.
	/// Returns an empty array when the folder doesn't exist.
	/// </summary>
	internal static string[] EnumerateHostTfms(string hostRoot)
	{
		if (!Directory.Exists(hostRoot))
		{
			return [];
		}

		var parsed = new List<(string Name, int Major, int Minor)>();
		foreach (var path in Directory.EnumerateDirectories(hostRoot))
		{
			var name = Path.GetFileName(path);
			if (TryParseNetTfm(name, out var major, out var minor))
			{
				parsed.Add((name, major, minor));
			}
		}

		parsed.Sort(static (a, b) => a.Major != b.Major ? a.Major.CompareTo(b.Major) : a.Minor.CompareTo(b.Minor));

		var result = new string[parsed.Count];
		for (var i = 0; i < parsed.Count; i++)
		{
			result[i] = parsed[i].Name;
		}

		return result;
	}

	/// <summary>
	/// Returns <paramref name="requestedTfm"/> when available, otherwise the highest TFM that is
	/// not newer than it and at most one major below it, or <c>null</c>.
	/// </summary>
	internal static string? TryResolveHostTfm(string[] availableTfms, string requestedTfm)
	{
		if (availableTfms.Length == 0 || !TryParseNetTfm(requestedTfm, out var requestedMajor, out var requestedMinor))
		{
			return null;
		}

		string? best = null;
		var bestMajor = -1;
		var bestMinor = -1;

		foreach (var tfm in availableTfms)
		{
			if (string.Equals(tfm, requestedTfm, StringComparison.OrdinalIgnoreCase))
			{
				return tfm;
			}

			if (!TryParseNetTfm(tfm, out var major, out var minor)
				|| major < requestedMajor - MaxMajorGap
				|| major > requestedMajor
				|| (major == requestedMajor && minor > requestedMinor))
			{
				continue;
			}

			if (major > bestMajor || (major == bestMajor && minor > bestMinor))
			{
				best = tfm;
				bestMajor = major;
				bestMinor = minor;
			}
		}

		return best;
	}

	/// <summary>
	/// Parses a short <c>net{X}.{Y}</c> TFM with X &gt;= 5. Rejects <c>netstandard</c>,
	/// <c>netcoreapp</c>, .NET Framework monikers and OS-suffixed TFMs.
	/// </summary>
	internal static bool TryParseNetTfm(string? name, out int major, out int minor)
	{
		major = 0;
		minor = 0;

		if (string.IsNullOrEmpty(name) || !name!.StartsWith("net", StringComparison.OrdinalIgnoreCase) || name.IndexOf('-') >= 0)
		{
			return false;
		}

		var dot = name.IndexOf('.', 3);
		if (dot < 0)
		{
			return false;
		}

		return int.TryParse(name.Substring(3, dot - 3), NumberStyles.None, CultureInfo.InvariantCulture, out major)
			&& int.TryParse(name.Substring(dot + 1), NumberStyles.None, CultureInfo.InvariantCulture, out minor)
			&& major >= 5;
	}
}
