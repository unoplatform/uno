using System;

namespace Uno.HotReload;

public static class HotReloadEnvironment
{
	/// <summary>
	/// Enables diagnostic logging for Roslyn Edit and Continue and MSBuild by setting environment variables to write logs
	/// to the specified directory.
	/// </summary>
	/// <remarks>This method sets environment variables that control logging for Roslyn Edit and Continue and
	/// MSBuild. Logging is enabled for the current process and any child processes that inherit these environment
	/// variables. Use this method before starting operations that require diagnostic logging. The specified directory
	/// should exist and be accessible to avoid logging failures.</remarks>
	/// <param name="logPath">The directory path where diagnostic logs will be written. Must be a valid, writable file system path.</param>
	public static void EnableLogging(string logPath)
	{
		if (logPath is not { Length: > 0 })
		{
			// Avoid logging at random places if no valid path is provided
			return;
		}

		// Sets Roslyn's environment variable for troubleshooting HR, see:
		// https://github.com/dotnet/roslyn/blob/fc6e0c25277ff440ca7ded842ac60278ee6c9695/src/Features/Core/Portable/EditAndContinue/EditAndContinueService.cs#L72
		Environment.SetEnvironmentVariable("Microsoft_CodeAnalysis_EditAndContinue_LogDir", logPath);

		// Unconditionally enable binlog generation in msbuild. See https://github.com/dotnet/project-system/blob/4210ce79cfd35154dbd858f056bfb9101f290e69/docs/design-time-builds.md?L61
		Environment.SetEnvironmentVariable("MSBUILDDEBUGENGINE", "1");
		Environment.SetEnvironmentVariable("MSBuildDebugEngine", "1"); // For case-sensitive environments like macOS
		Environment.SetEnvironmentVariable("MSBUILDDEBUGPATH", logPath);
	}
}
