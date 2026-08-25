#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace Uno.UI.Runtime.Skia;

internal static class NativeProcessRunner
{
	internal static NativeProcessResult Run(
		string executable,
		IEnumerable<string> arguments,
		IReadOnlyDictionary<string, string?>? environment = null)
	{
		var startInfo = new ProcessStartInfo
		{
			FileName = executable,
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			CreateNoWindow = true,
			StandardOutputEncoding = Encoding.UTF8,
			StandardErrorEncoding = Encoding.UTF8
		};

		foreach (var argument in arguments)
		{
			startInfo.ArgumentList.Add(argument);
		}

		if (environment is not null)
		{
			foreach (var variable in environment)
			{
				startInfo.Environment[variable.Key] = variable.Value;
			}
		}

		try
		{
			using var process = new Process { StartInfo = startInfo };
			if (!process.Start())
			{
				throw new InvalidOperationException(
					$"The process '{executable}' could not be started.");
			}

			var standardOutput = process.StandardOutput.ReadToEndAsync();
			var standardError = process.StandardError.ReadToEndAsync();
			process.WaitForExit();
			return new NativeProcessResult(
				process.ExitCode,
				standardOutput.GetAwaiter().GetResult(),
				standardError.GetAwaiter().GetResult());
		}
		catch (Win32Exception error)
		{
			throw new InvalidOperationException(
				$"The process '{executable}' could not be started.",
				error);
		}
	}
}

internal sealed record NativeProcessResult(
	int ExitCode,
	string StandardOutput,
	string StandardError)
{
	internal string GetError(string operation)
	{
		var details = string.IsNullOrWhiteSpace(StandardError)
			? StandardOutput.Trim()
			: StandardError.Trim();
		return string.IsNullOrWhiteSpace(details)
			? $"{operation} failed with exit code {ExitCode}."
			: $"{operation} failed: {details}";
	}
}
