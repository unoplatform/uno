using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Uno.UI.DevServer.Cli.E2E.Tests.Mcp;

internal sealed class TemporaryUnoWorkspace : IDisposable
{
	public string RootDirectory { get; }
	public string RepositoryRoot { get; }
	public string PrimaryWorkspaceDirectory { get; internal set; } = null!;
	public string PrimarySolutionPath { get; internal set; } = null!;
	public string? SecondaryWorkspaceDirectory { get; internal set; }
	public string? SecondarySolutionPath { get; internal set; }
	public string? NonUnoSolutionPath { get; internal set; }

	internal TemporaryUnoWorkspace(string rootDirectory, string repositoryRoot)
	{
		RootDirectory = rootDirectory;
		RepositoryRoot = repositoryRoot;
	}

	public void Dispose()
	{
		try
		{
			if (Directory.Exists(RootDirectory))
			{
				Directory.Delete(RootDirectory, recursive: true);
			}
		}
		catch
		{
			// Best-effort cleanup for process-backed integration tests.
		}
	}
}

internal static partial class TemporaryUnoWorkspaceBuilder
{
	private static readonly string TemplateRelativeDirectory = Path.Combine("src", "SolutionTemplate", "5.6", "uno56netcurrent");
	private const string TemplateSolutionName = "uno56netcurrent.sln";
	private static readonly string CompatibleUnoSdkVersion = ResolveCompatibleUnoSdkVersion();

	[GeneratedRegex("\"Uno\\.Sdk\\.Private\"\\s*:\\s*\"[^\"]+\"", RegexOptions.CultureInvariant)]
	private static partial Regex UnoSdkPrivateVersionRegex();

	public static TemporaryUnoWorkspace CreateNestedWorkspace()
	{
		var workspace = CreateFixture();
		var templateTargetDirectory = Path.Combine(workspace.RepositoryRoot, "src", "App");
		CopyTemplateWorkspace(templateTargetDirectory);
		RestoreWorkspace(templateTargetDirectory);

		workspace.PrimaryWorkspaceDirectory = templateTargetDirectory;
		workspace.PrimarySolutionPath = Path.Combine(templateTargetDirectory, TemplateSolutionName);
		return workspace;
	}

	public static TemporaryUnoWorkspace CreateAmbiguousWorkspace()
	{
		var workspace = CreateFixture();
		var primaryDirectory = Path.Combine(workspace.RepositoryRoot, "src", "AppOne");
		var secondaryDirectory = Path.Combine(workspace.RepositoryRoot, "src", "AppTwo");

		CopyTemplateWorkspace(primaryDirectory);
		CopyTemplateWorkspace(secondaryDirectory);
		RestoreWorkspace(primaryDirectory);
		RestoreWorkspace(secondaryDirectory);

		workspace.PrimaryWorkspaceDirectory = primaryDirectory;
		workspace.PrimarySolutionPath = Path.Combine(primaryDirectory, TemplateSolutionName);
		workspace.SecondaryWorkspaceDirectory = secondaryDirectory;
		workspace.SecondarySolutionPath = Path.Combine(secondaryDirectory, TemplateSolutionName);
		return workspace;
	}

	public static TemporaryUnoWorkspace CreateNestedWorkspaceWithNonUnoSolution()
	{
		var workspace = CreateNestedWorkspace();
		var nonUnoDirectory = Path.Combine(workspace.RepositoryRoot, "tools", "NonUno");
		Directory.CreateDirectory(nonUnoDirectory);

		workspace.NonUnoSolutionPath = Path.Combine(nonUnoDirectory, "NonUno.slnx");
		File.WriteAllText(
			workspace.NonUnoSolutionPath,
			"""
			<Solution>
			  <Configurations>
			    <BuildType Name="Debug" />
			    <Platform Name="Any CPU" />
			  </Configurations>
			</Solution>
			""");

		return workspace;
	}

	private static TemporaryUnoWorkspace CreateFixture()
	{
		var rootDirectory = Path.Combine(Path.GetTempPath(), $"uno-devserver-e2e-{Guid.NewGuid():N}");
		var repositoryRoot = Path.Combine(rootDirectory, "repo");

		Directory.CreateDirectory(repositoryRoot);
		return new TemporaryUnoWorkspace(rootDirectory, repositoryRoot);
	}

	private static void CopyTemplateWorkspace(string targetDirectory)
	{
		var sourceDirectory = Path.Combine(BuildOutputLocator.FindRepositoryRoot(), TemplateRelativeDirectory);
		CopyDirectoryRecursive(sourceDirectory, targetDirectory);
		RewriteGlobalJson(targetDirectory);
	}

	private static void CopyDirectoryRecursive(string sourceDirectory, string targetDirectory)
	{
		Directory.CreateDirectory(targetDirectory);

		foreach (var directory in Directory.EnumerateDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
		{
			var relativeDirectory = Path.GetRelativePath(sourceDirectory, directory);
			Directory.CreateDirectory(Path.Combine(targetDirectory, relativeDirectory));
		}

		foreach (var file in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
		{
			var relativeFile = Path.GetRelativePath(sourceDirectory, file);
			var destination = Path.Combine(targetDirectory, relativeFile);
			Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
			File.Copy(file, destination, overwrite: true);
		}
	}

	private static void RestoreWorkspace(string workspaceDirectory)
	{
		var startInfo = new ProcessStartInfo("dotnet")
		{
			WorkingDirectory = workspaceDirectory,
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
		};
		startInfo.ArgumentList.Add("restore");

		using var process = Process.Start(startInfo)
			?? throw new InvalidOperationException($"Unable to start 'dotnet restore' in '{workspaceDirectory}'.");

		var stdout = process.StandardOutput.ReadToEnd();
		var stderr = process.StandardError.ReadToEnd();
		process.WaitForExit();

		if (process.ExitCode != 0)
		{
			throw new InvalidOperationException(
				$"dotnet restore failed in '{workspaceDirectory}' with exit code {process.ExitCode}.{Environment.NewLine}STDOUT:{Environment.NewLine}{stdout}{Environment.NewLine}STDERR:{Environment.NewLine}{stderr}");
		}
	}

	private static void RewriteGlobalJson(string workspaceDirectory)
	{
		var globalJsonPath = Path.Combine(workspaceDirectory, "global.json");
		var contents = File.ReadAllText(globalJsonPath);
		contents = UnoSdkPrivateVersionRegex().Replace(contents, $"\"Uno.Sdk.Private\": \"{CompatibleUnoSdkVersion}\"");
		File.WriteAllText(globalJsonPath, contents);
	}

	private static string ResolveCompatibleUnoSdkVersion()
	{
		var packagesRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
		var sdkRoot = Path.Combine(packagesRoot, "uno.sdk.private");
		var devServerRoot = Path.Combine(packagesRoot, "uno.winui.devserver");

		if (!Directory.Exists(sdkRoot) || !Directory.Exists(devServerRoot))
		{
			throw new DirectoryNotFoundException("Unable to locate local Uno SDK / DevServer packages under the user's NuGet cache.");
		}

		var compatibleVersions = Directory.EnumerateDirectories(sdkRoot)
			.Select(Path.GetFileName)
			.Where(version => !string.IsNullOrWhiteSpace(version))
			.Select(version => version!)
			.Where(version =>
			{
				var hostPath = Path.Combine(devServerRoot, version, "tools", "rc", "host", "net10.0", "Uno.UI.RemoteControl.Host.dll");
				return File.Exists(hostPath);
			})
			.OrderByDescending(ParseVersionKey)
			.ToArray();

		if (compatibleVersions.Length == 0)
		{
			throw new InvalidOperationException("Unable to find a locally cached Uno SDK version whose DevServer package exposes a net10.0 host.");
		}

		return compatibleVersions[0];
	}

	private static VersionKey ParseVersionKey(string version)
	{
		var parts = version.Split('-', 2, StringSplitOptions.TrimEntries);
		var numericSegments = parts[0].Split('.');
		var major = numericSegments.Length > 0 && int.TryParse(numericSegments[0], out var parsedMajor) ? parsedMajor : 0;
		var minor = numericSegments.Length > 1 && int.TryParse(numericSegments[1], out var parsedMinor) ? parsedMinor : 0;
		var patch = numericSegments.Length > 2 && int.TryParse(numericSegments[2], out var parsedPatch) ? parsedPatch : 0;
		var suffix = parts.Length > 1 ? parts[1] : string.Empty;
		return new VersionKey(major, minor, patch, suffix);
	}

	private readonly record struct VersionKey(int Major, int Minor, int Patch, string Suffix) : IComparable<VersionKey>
	{
		public int CompareTo(VersionKey other)
		{
			var major = Major.CompareTo(other.Major);
			if (major != 0)
			{
				return major;
			}

			var minor = Minor.CompareTo(other.Minor);
			if (minor != 0)
			{
				return minor;
			}

			var patch = Patch.CompareTo(other.Patch);
			if (patch != 0)
			{
				return patch;
			}

			if (string.IsNullOrEmpty(Suffix) && string.IsNullOrEmpty(other.Suffix))
			{
				return 0;
			}

			if (string.IsNullOrEmpty(Suffix))
			{
				return 1;
			}

			if (string.IsNullOrEmpty(other.Suffix))
			{
				return -1;
			}

			return StringComparer.OrdinalIgnoreCase.Compare(Suffix, other.Suffix);
		}
	}
}
