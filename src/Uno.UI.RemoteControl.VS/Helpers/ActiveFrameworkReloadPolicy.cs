using System;
using System.Text.RegularExpressions;

namespace Uno.UI.RemoteControl.VS.Helpers;

/// <summary>
/// Decides whether changing the active target framework has to reload the startup project.
/// Linked into Uno.UI.RemoteControl.DevServer.Tests, keep it free of Visual Studio dependencies.
/// </summary>
internal static class ActiveFrameworkReloadPolicy
{
	internal const string DesktopTargetFrameworkIdentifier = "desktop";
	internal const string Windows10TargetFrameworkIdentifier = "windows10";
	internal const string WasmTargetFrameworkIdentifier = "browserwasm";

	private static readonly Version _anyTargetFrameworkPositionVersion = new(17, 12);
	private static readonly Regex _targetFrameworkIdentifierRegex = new(@"net(\d+\.\d+)-(?<tfi>\w+)(\d+\.\d+\.\d+)?");

	internal static string? GetTargetFrameworkIdentifier(string framework)
	{
		var match = _targetFrameworkIdentifierRegex.Match(framework);

		return match.Success
			? match.Groups["tfi"].Value
			: null;
	}

	/// <param name="previousFramework">The previously active framework, null for the first report after a project load.</param>
	/// <param name="newFramework">The newly active framework.</param>
	/// <param name="visualStudioVersion">The running Visual Studio release, null when it could not be determined.</param>
	/// <param name="isSelectedFrameworkUnset">The .user file has no _UnoSelectedTargetFramework yet.</param>
	internal static bool RequiresReload(string? previousFramework, string newFramework, Version? visualStudioVersion, bool isSelectedFrameworkUnset)
	{
		if (isSelectedFrameworkUnset)
		{
			// Writing the .user value is enough, unless evaluating the project depends on it.
			return GetTargetFrameworkIdentifier(newFramework) is { } identifier
				&& RequiresFirstPosition(identifier, visualStudioVersion);
		}

		return previousFramework is not null
			&& GetTargetFrameworkIdentifier(previousFramework) is { } previousIdentifier
			&& (RequiresFirstPosition(previousIdentifier, visualStudioVersion)
				|| (GetTargetFrameworkIdentifier(newFramework) is { } newIdentifier && RequiresFirstPosition(newIdentifier, visualStudioVersion)));
	}

	// VS 17.12+ handles a target framework anywhere in the list, except wasm.
	private static bool RequiresFirstPosition(string identifier, Version? visualStudioVersion)
		=> visualStudioVersion is not null && visualStudioVersion < _anyTargetFrameworkPositionVersion
			? identifier is WasmTargetFrameworkIdentifier or DesktopTargetFrameworkIdentifier or Windows10TargetFrameworkIdentifier
			: identifier is WasmTargetFrameworkIdentifier;
}
