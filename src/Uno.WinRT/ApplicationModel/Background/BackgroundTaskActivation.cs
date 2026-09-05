#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Windows.ApplicationModel.Background;

internal static class BackgroundTaskActivation
{
	internal const string ArgumentName = "--uno-background-task";
	internal const string ApplicationAssemblyNameArgument =
		"--uno-background-task-application-assembly";
	internal const string ApplicationAssemblyPathArgument =
		"--uno-background-task-application-path";

	internal static bool TryGetActivation(
		IReadOnlyList<string> arguments,
		[NotNullWhen(true)]
		out BackgroundTaskActivationInfo? activation)
	{
		var taskIdValue = FindValue(arguments, ArgumentName);
		var applicationAssemblyName = FindValue(
			arguments,
			ApplicationAssemblyNameArgument);
		if (Guid.TryParse(taskIdValue, out var taskId) &&
			!string.IsNullOrWhiteSpace(applicationAssemblyName))
		{
			activation = new BackgroundTaskActivationInfo(
				taskId,
				applicationAssemblyName,
				FindValue(arguments, ApplicationAssemblyPathArgument));
			return true;
		}

		activation = null;
		return false;
	}

	internal static int Run(Guid taskId)
	{
#if __SKIA__
		return BackgroundTaskRunner.Run(taskId);
#else
		throw new PlatformNotSupportedException(
			"Out-of-process background task activation is only available on Skia targets.");
#endif
	}

	private static string? FindValue(
		IReadOnlyList<string> arguments,
		string name)
	{
		for (var index = 0; index < arguments.Count - 1; index++)
		{
			if (string.Equals(arguments[index], name, StringComparison.Ordinal))
			{
				return arguments[index + 1];
			}
		}

		return null;
	}
}

internal sealed record BackgroundTaskActivationInfo(
	Guid TaskId,
	string ApplicationAssemblyName,
	string? ApplicationAssemblyPath);
