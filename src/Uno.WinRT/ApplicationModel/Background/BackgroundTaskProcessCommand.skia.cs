#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Windows.ApplicationModel.Background;

internal sealed record BackgroundTaskProcessCommand(
	string ExecutablePath,
	IReadOnlyList<string> Arguments,
	string WorkingDirectory)
{
	internal static BackgroundTaskProcessCommand Create(Guid taskId)
	{
		var executablePath = Environment.ProcessPath;
		if (string.IsNullOrWhiteSpace(executablePath))
		{
			throw new InvalidOperationException(
				"The current executable path could not be determined.");
		}

		var arguments = new List<string>();
		if (string.Equals(
			Path.GetFileNameWithoutExtension(executablePath),
			"dotnet",
			StringComparison.OrdinalIgnoreCase))
		{
			var executableAssemblyPath = Assembly.GetEntryAssembly()?.Location;
			if (string.IsNullOrWhiteSpace(executableAssemblyPath))
			{
				throw new InvalidOperationException(
					"The application assembly path could not be determined for the .NET host.");
			}

			arguments.Add(Path.GetFullPath(executableAssemblyPath));
		}

		var applicationAssembly = Package.EntryAssembly
			?? throw new InvalidOperationException(
				"The application package identity has not been initialized.");
		arguments.Add(BackgroundTaskActivation.ArgumentName);
		arguments.Add(taskId.ToString("D"));
		arguments.Add(BackgroundTaskActivation.ApplicationAssemblyNameArgument);
		arguments.Add(
			applicationAssembly.GetName().Name
				?? throw new InvalidOperationException(
					"The application assembly name could not be determined."));

		var applicationAssemblyPath = applicationAssembly.Location;
		if (!string.IsNullOrWhiteSpace(applicationAssemblyPath))
		{
			arguments.Add(BackgroundTaskActivation.ApplicationAssemblyPathArgument);
			arguments.Add(Path.GetFullPath(applicationAssemblyPath));
		}

		return new BackgroundTaskProcessCommand(
			executablePath,
			arguments,
			AppContext.BaseDirectory);
	}
}
