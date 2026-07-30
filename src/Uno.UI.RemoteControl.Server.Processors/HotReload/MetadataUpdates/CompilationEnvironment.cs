using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.Roslyn.MSBuild;
using Uno.UI.RemoteControl.Helpers;

namespace Uno.UI.RemoteControl.Host.HotReload.MetadataUpdates;

internal static class CompilationEnvironment
{
	private static string _mSBuildBasePath = "";

	public static void Initialize(string? workDir)
	{
		// Assembly registrations must be done before the workspace is initialized
		// Not doing so will cause the roslyn msbuild workspace to fail to load because of a missing path on assemblies loaded from a memory stream.
		RegisterAssemblyLoader();

		if (Assembly.Load("Microsoft.CodeAnalysis.Workspaces") is { } wsAsm)
		{
			// If this assembly was loaded from a stream, it will not have a location.
			// This will indicate that the assembly loader from CompilationWorkspaceProvider
			// has been registered too late.
			if (string.IsNullOrEmpty(wsAsm.Location))
			{
				throw new InvalidOperationException("Microsoft.CodeAnalysis.Workspaces was loaded from a stream and must be loaded from a known path");
			}
		}

		_mSBuildBasePath = BuildMSBuildPath(workDir);

		var expectedVersion = GetDotnetVersion(workDir);
		var currentVersion = typeof(object).Assembly.GetName().Version;
		if (expectedVersion.Major != currentVersion?.Major)
		{
			if (typeof(CompilationEnvironment).Log().IsEnabled(LogLevel.Error))
			{
				typeof(CompilationEnvironment).Log().LogError($"Unable to start the Remote Control server because the application's TargetFramework version does not match the default runtime. Expected: net{expectedVersion.Major}, Current: net{currentVersion?.Major}. Change the TargetFramework version in your project file to match the expected version.");
			}

			throw new InvalidOperationException($"Project TargetFramework version mismatch. Expected: net{expectedVersion.Major}, Current: net{currentVersion?.Major}");
		}

		Environment.SetEnvironmentVariable("MSBuildSDKsPath", Path.Combine(_mSBuildBasePath, "Sdks"));

		var MSBuildExists = File.Exists(Path.Combine(_mSBuildBasePath, "Microsoft.Build.dll"));

		if (!MSBuildExists)
		{
			throw new InvalidOperationException($"Invalid dotnet installation installation (Cannot find Microsoft.Build.dll in [{_mSBuildBasePath}])");
		}
	}

	private static Version GetDotnetVersion(string? workDir)
	{
		var result = ProcessHelper.RunProcess("dotnet.exe", "--version", workDir);

		if (result.exitCode == 0)
		{
			var reader = new StringReader(result.output);

			if (Version.TryParse(reader.ReadLine()?.Split('-').FirstOrDefault(), out var version))
			{
				return version;
			}
		}

		throw new InvalidOperationException("Failed to read dotnet version");
	}

	private static string BuildMSBuildPath(string? workDir)
	{
		var result = ProcessHelper.RunProcess("dotnet.exe", "--info", workDir);

		if (result.exitCode == 0)
		{
			var reader = new StringReader(result.output);

			while (reader.ReadLine() is string line)
			{
				if (line.Contains("Base Path:"))
				{
					return line.Substring(line.IndexOf(':') + 1).Trim();
				}
			}

			throw new InvalidOperationException($"Unable to find dotnet SDK base path in:\n {result.output}");
		}

		throw new InvalidOperationException("Unable to find dotnet SDK base path");
	}

	private static void RegisterAssemblyLoader()
	{
		// Force assembly loader to consider siblings, when running in a separate appdomain / ALC.
		Assembly? Load(string name)
		{
			if (name == "Mono.Runtime")
			{
				// Roslyn 2.0 and later checks for the presence of the Mono runtime
				// through this check.
				return null;
			}

			var assembly = new AssemblyName(name);
			var basePath = Path.GetDirectoryName(new Uri(typeof(CompilationWorkspaceProvider).Assembly.Location).LocalPath) ?? "";

			Console.WriteLine($"Searching for [{assembly}] from [{basePath}]");

			// Ignore resource assemblies for now, we'll have to adjust this
			// when adding globalization.
			if (assembly.Name is not null && assembly.Name.EndsWith(".resources", StringComparison.Ordinal))
			{
				return null;
			}

			Assembly? LoadAssembly(string filePath)
			{
				if (File.Exists(filePath))
				{
					try
					{
						var output = Assembly.LoadFrom(filePath);

						Console.WriteLine($"Loaded [{output.GetName()}] from [{output.Location}]");

						return output;
					}
					catch (Exception ex)
					{
						Console.WriteLine($"Failed to load [{assembly}] from [{filePath}]", ex);
						return null;
					}
				}
				else
				{
					return null;
				}
			}

			var paths = new[] {
				Path.Combine(basePath, assembly.Name + ".dll"),
				Path.Combine(_mSBuildBasePath, assembly.Name + ".dll"),
			};

			return paths
				.Select(LoadAssembly)
				.Where(p => p != null)
				.FirstOrDefault();
		}

		AppDomain.CurrentDomain.AssemblyResolve += (snd, e) => Load(e.Name);
		AppDomain.CurrentDomain.TypeResolve += (snd, e) => Load(e.Name);

		// Processors are loaded in a separate ALC in `RemoteControlServer`, which requires
		// to load the files in same ALC to avoid invalid cross-ALC references.
		if (AssemblyLoadContext.GetLoadContext(typeof(CompilationWorkspaceProvider).Assembly) is { } alc)
		{
			alc.Resolving += (snd, e) => Load(e.FullName);
		}
		else
		{
			throw new InvalidOperationException($"Unable to determine the ALC for {nameof(CompilationWorkspaceProvider)}");
		}
	}
}
