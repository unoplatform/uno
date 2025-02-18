using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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
		// Note: We include the targets "on the fly" so if a project uses Microsoft.NET.Sdk instead of Uno.Sdk, we will still have the targets defined.
		var targetsFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "DevServer.Custom.Targets");

		var tmp = Path.GetTempFileName();
		var wd = Path.GetDirectoryName(solutionFile);
		string DumpTFM(string v) => $"build \"{solutionFile}\" -t:UnoDumpTargetFrameworks \"-p:UnoDumpTargetFrameworksTargetFile={tmp}\" \"-p:CustomBeforeMicrosoftCSharpTargets={targetsFile}\" --verbosity {v}";
		var command = DumpTFM("quiet");
		var result = ProcessHelper.RunProcess("dotnet", command, wd);
		var targetFrameworks = Read(tmp);

		if (targetFrameworks.IsEmpty)
		{
			if (_log.IsEnabled(LogLevel.Warning))
			{
				var msg = $"Failed to get target frameworks of solution '{solutionFile}'. "
					+ "This usually indicates that the solution is in an invalid state (e.g. a referenced project is missing on disk). "
					+ $"Please fix and restart your IDE (command used: `dotnet {command}`).";
				if (result.error is { Length: > 0 })
				{
					_log.Log(LogLevel.Warning, new Exception(result.error), msg + " (cf. inner exception for more details.)");
				}
				else
				{
					result = ProcessHelper.RunProcess("dotnet", DumpTFM("diagnostic"), wd);

					_log.Log(LogLevel.Warning, msg);
					_log.Log(LogLevel.Debug, result.output);
				}
			}

			return ImmutableArray<string>.Empty;
		}

		if (_log.IsEnabled(LogLevel.Debug))
		{
			_log.Log(LogLevel.Debug, $"Found target frameworks for solution '{solutionFile}': {string.Join(", ", targetFrameworks)}.");
		}


		foreach (var targetFramework in targetFrameworks)
		{
			tmp = Path.GetTempFileName();
			command = $"build \"{solutionFile}\" -t:UnoDumpRemoteControlAddIns \"-p:UnoDumpRemoteControlAddInsTargetFile={tmp}\" \"-p:CustomBeforeMicrosoftCSharpTargets={targetsFile}\" --verbosity quiet --framework \"{targetFramework}\" -nowarn:MSB4057";
			result = ProcessHelper.RunProcess("dotnet", command, wd);
			if (!string.IsNullOrWhiteSpace(result.error))
			{
				if (_log.IsEnabled(LogLevel.Warning))
				{
					var msg = $"Failed to get add-ins for solution '{solutionFile}' for tfm {targetFramework} (command used: `dotnet {command}`).";
					if (result.error is { Length: > 0 })
					{
						_log.Log(LogLevel.Warning, new Exception(result.error), msg + " (cf. inner exception for more details.)");
					}
					else
					{
						_log.Log(LogLevel.Warning, msg);
					}
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
