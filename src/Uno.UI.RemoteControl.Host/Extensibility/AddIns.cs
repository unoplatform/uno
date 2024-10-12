using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.UI.RemoteControl.Helpers;

namespace Uno.UI.RemoteControl.Host.Extensibility;

public class AddIns
{
	private static readonly ILogger _log = typeof(AddIns).Log();

	public static IImmutableList<string> Discover(string solutionFile)
	{
		// Note: With .net 9 we need to specify --verbosity detailed to get messages with High importance.
		var result = ProcessHelper.RunProcess("dotnet", $"build \"{solutionFile}\" --target:UnoDumpTargetFrameworks --verbosity detailed");
		var targetFrameworks = GetConfigurationValue(result.output ?? "", "TargetFrameworks")
			.SelectMany(tfms => tfms.Split(['\r', '\n', ';', ','], StringSplitOptions.RemoveEmptyEntries))
			.Select(tfm => tfm.Trim())
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToImmutableList();

		if (targetFrameworks.IsEmpty)
		{
			if (_log.IsEnabled(LogLevel.Warning))
			{
				_log.Log(LogLevel.Warning, new Exception(result.error), $"Failed to get target frameworks of solution '{solutionFile}' (cf. inner exception for details).");
			}

			return ImmutableArray<string>.Empty;
		}


		foreach (var targetFramework in targetFrameworks)
		{
			result = ProcessHelper.RunProcess("dotnet", $"build \"{solutionFile}\" --target:UnoDumpRemoteControlAddIns --verbosity detailed --framework \"{targetFramework}\" -nowarn:MSB4057");
			if (!string.IsNullOrWhiteSpace(result.error))
			{
				if (_log.IsEnabled(LogLevel.Warning))
				{
					_log.Log(LogLevel.Warning, new Exception(result.error), $"Failed to get add-ins for solution '{solutionFile}' for tfm {targetFramework} (cf. inner exception for details).");
				}

				continue;
			}

			var addIns = GetConfigurationValue(result.output, "RemoteControlAddIns")
				.SelectMany(tfms => tfms.Split(['\r', '\n', ';', ','], StringSplitOptions.RemoveEmptyEntries))
				.Select(tfm => tfm.Trim())
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToImmutableList();

			if (!addIns.IsEmpty)
			{
				return addIns;
			}
		}

		if (_log.IsEnabled(LogLevel.Information))
		{
			_log.Log(LogLevel.Information, $"Didn't find any add-ins for solution '{solutionFile}'.");
		}

		return ImmutableArray<string>.Empty;
	}

	private static IEnumerable<string> GetConfigurationValue(string msbuildResult, string nodeName)
		=> Regex
			.Matches(msbuildResult, $"<{nodeName}>(?<value>.*)</{nodeName}>")
			.Where(match => match.Success)
			.Select(match => match.Groups["value"].Value);
}
