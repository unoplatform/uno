using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Uno.UI.RemoteControl.Helpers;

namespace Uno.UI.RemoteControl.Host.Extensibility;

public class AddIns
{
	public static IImmutableList<string> Discover(string solutionFile)
		=> ProcessHelper.RunProcess("dotnet", $"build \"{solutionFile}\" /t:GetRemoteControlAddIns /nowarn:MSB4057") switch // Ignore missing target
		{
			// Note: We ignore the exitCode not being 0: even if flagged as nowarn, we can still get MSB4057 for project that does not have the target GetRemoteControlAddIns
			{ error: { Length: > 0 } err } => throw new InvalidOperationException($"Failed to get add-ins for solution '{solutionFile}' (cf. inner exception for details).", new Exception(err)),
			var result => GetConfigurationValue(result.output, "RemoteControlAddIns")
				?.Split(['\r', '\n', ';', ','], StringSplitOptions.RemoveEmptyEntries)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToImmutableList() ?? ImmutableList<string>.Empty,
		};

	private static string? GetConfigurationValue(string msbuildResult, string nodeName)
		=> Regex.Match(msbuildResult, $"<{nodeName}>(?<value>.*)</{nodeName}>") is { Success: true } match
			? match.Groups["value"].Value
			: null;
}
