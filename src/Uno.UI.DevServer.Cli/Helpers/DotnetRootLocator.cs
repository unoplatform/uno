using System;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Uno.UI.DevServer.Cli.Helpers;

/// <summary>
/// A .NET install: its root folder and the architecture of its <c>dotnet</c> executable.
/// </summary>
internal readonly record struct DotnetInstall(string Root, Architecture Architecture);

/// <summary>
/// Finds the .NET install that <c>dotnet</c>, started by name from this process, runs from: the one whose
/// <c>dotnet --version</c> picks the host TFM. Windows only, the only OS where the host is started through its apphost.
/// </summary>
internal static class DotnetRootLocator
{
	private const string DotnetExecutable = "dotnet.exe";

	/// <param name="processPath">The current process, a <c>dotnet</c> muxer when the CLI runs through <c>dotnet dnx</c>.</param>
	/// <param name="pathVariable">The <c>PATH</c> value, searched when it isn't (e.g. the CLI runs through its global-tool shim).</param>
	public static DotnetInstall? TryLocate(string? processPath, string? pathVariable)
	{
		// Windows looks for an executable in the current process's folder first, then on PATH.
		var dotnet = IsDotnet(processPath) ? processPath : FindFirstOnPath(pathVariable);
		var root = Path.GetDirectoryName(dotnet);

		// The first match is what Windows runs: when it isn't a .NET install (e.g. a package manager shim), don't guess.
		if (dotnet is null || string.IsNullOrEmpty(root) || !Directory.Exists(Path.Combine(root, "host", "fxr")))
		{
			return null;
		}

		return TryGetArchitecture(dotnet) is { } architecture
			? new DotnetInstall(root, architecture)
			: null;
	}

	/// <summary>
	/// The install an apphost of <paramref name="architecture"/> uses when no DOTNET_ROOT variable is set:
	/// the registered one, else the default folder under Program Files.
	/// </summary>
	public static string? TryGetMachineWideRoot(Architecture architecture)
	{
		if (!OperatingSystem.IsWindows())
		{
			return null;
		}

		try
		{
			// The .NET host reads the 32-bit registry view, whatever its own architecture.
			using var localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
			using var key = localMachine.OpenSubKey($@"SOFTWARE\dotnet\Setup\InstalledVersions\{architecture.ToString().ToLowerInvariant()}");
			if (key?.GetValue("InstallLocation") is string { Length: > 0 } registered)
			{
				return registered;
			}
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
		{
		}

		var programFiles = Environment.GetEnvironmentVariable(architecture == Architecture.X86 ? "ProgramFiles(x86)" : "ProgramFiles");
		return string.IsNullOrEmpty(programFiles) ? null : Path.Combine(programFiles, "dotnet");
	}

	public static Architecture? TryGetArchitecture(string executablePath)
	{
		try
		{
			using var stream = File.OpenRead(executablePath);
			using var reader = new PEReader(stream);

			return reader.PEHeaders.CoffHeader.Machine switch
			{
				Machine.Amd64 => Architecture.X64,
				Machine.Arm64 => Architecture.Arm64,
				Machine.I386 => Architecture.X86,
				_ => null,
			};
		}
		catch (Exception)
		{
			// Not readable or not an executable image: the caller then sets nothing.
			return null;
		}
	}

	private static bool IsDotnet(string? path)
		=> path is not null && string.Equals(Path.GetFileName(path), DotnetExecutable, StringComparison.OrdinalIgnoreCase);

	private static string? FindFirstOnPath(string? pathVariable)
	{
		if (string.IsNullOrEmpty(pathVariable))
		{
			return null;
		}

		// Entries are taken as they are, like Windows does: a quoted or padded entry is not searched.
		foreach (var entry in pathVariable.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
		{
			try
			{
				var candidate = Path.Combine(Path.GetFullPath(entry), DotnetExecutable);
				if (File.Exists(candidate))
				{
					return candidate;
				}
			}
			catch (Exception ex) when (ex is ArgumentException or NotSupportedException or IOException or UnauthorizedAccessException)
			{
				// An unusable PATH entry is skipped.
			}
		}

		return null;
	}
}
