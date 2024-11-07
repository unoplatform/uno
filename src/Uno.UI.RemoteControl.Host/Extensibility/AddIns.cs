using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.UI.RemoteControl.Helpers;

namespace Uno.UI.RemoteControl.Host.Extensibility;

public class AddIns
{
	private static readonly ILogger _log = typeof(AddIns).Log();

	public static IImmutableList<string> Discover(string solutionFile)
	{
		var tmp = Path.GetTempFileName();
		var result = ProcessHelper.RunProcess("dotnet", $"build \"{solutionFile}\" --target:UnoDumpTargetFrameworks \"-p:UnoDumpTargetFrameworksTargetFile={tmp}\" --verbosity quiet");
		var targetFrameworks = Read(tmp);

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
			tmp = Path.GetTempFileName();
			result = ProcessHelper.RunProcess("dotnet", $"build \"{solutionFile}\" --target:UnoDumpRemoteControlAddIns \"-p:UnoDumpRemoteControlAddInsTargetFile={tmp}\" --verbosity quiet --framework \"{targetFramework}\" -nowarn:MSB4057");
			if (!string.IsNullOrWhiteSpace(result.error))
			{
				if (_log.IsEnabled(LogLevel.Warning))
				{
					_log.Log(LogLevel.Warning, new Exception(result.error), $"Failed to get add-ins for solution '{solutionFile}' for tfm {targetFramework} (cf. inner exception for details).");
				}

				continue;
			}

			var addIns = Read(tmp);
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

	private static ImmutableList<string> Read(string file)
	{
		var values = ImmutableList<string>.Empty;
		try
		{
			values = File
				.ReadAllLines(file, Encoding.Unicode)
				.SelectMany(line => line.Split(['\r', '\n', ';', ','], StringSplitOptions.RemoveEmptyEntries))
				.Select(value => value.Trim())
				.Where(value => value is { Length: > 0 })
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToImmutableList();
		}
		catch { }

		try
		{
			File.Delete(file);
		}
		catch { }

		return values;
	}
}
