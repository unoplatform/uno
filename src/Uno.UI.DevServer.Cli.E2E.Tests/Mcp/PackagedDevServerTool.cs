namespace Uno.UI.DevServer.Cli.E2E.Tests.Mcp;

internal sealed class PackagedDevServerTool : IDisposable
{
	private readonly string _toolDirectory;
	private readonly string _packageVersion;

	private PackagedDevServerTool(string rootDirectory, string toolDirectory, string packageVersion)
	{
		RootDirectory = rootDirectory;
		_toolDirectory = toolDirectory;
		_packageVersion = packageVersion;
	}

	public string RootDirectory { get; }

	public string PackageVersion => _packageVersion;

	public string CommandPath
	{
		get
		{
			var candidateNames = OperatingSystem.IsWindows()
				? new[] { "uno-devserver.exe", "uno-devserver.cmd", "uno-devserver" }
				: new[] { "uno-devserver" };

			foreach (var name in candidateNames)
			{
				var candidate = Path.Combine(_toolDirectory, name);
				if (File.Exists(candidate))
				{
					return candidate;
				}
			}

			throw new FileNotFoundException($"Unable to locate the packaged uno-devserver command under '{_toolDirectory}'.");
		}
	}

	public CliCommandSpec CreateCommand(params IEnumerable<string>[] argumentGroups)
		=> new(CommandPath, [.. argumentGroups.SelectMany(static group => group)]);

	public static PackagedDevServerTool Create()
	{
		var rootDirectory = Path.Combine(Path.GetTempPath(), $"uno-devserver-package-e2e-{Guid.NewGuid():N}");
		var packageDirectory = Path.Combine(rootDirectory, "packages");
		var toolDirectory = Path.Combine(rootDirectory, "tool");
		var packageVersion = $"99.0.0-local-dev.{DateTime.UtcNow:yyyyMMddHHmmss}";

		Directory.CreateDirectory(packageDirectory);
		Directory.CreateDirectory(toolDirectory);

		var cliProjectPath = BuildOutputLocator.ResolveCliProjectPath();
		RunDotNet(
			$"pack {cliProjectPath} --no-restore -c Debug -o {Quote(packageDirectory)} /p:Version={packageVersion} /p:PackageVersion={packageVersion}",
			BuildOutputLocator.FindRepositoryRoot(),
			"dotnet pack for Uno.DevServer");

		RunDotNet(
			$"tool install Uno.DevServer --tool-path {Quote(toolDirectory)} --version {packageVersion} --add-source {Quote(packageDirectory)} --ignore-failed-sources",
			rootDirectory,
			"dotnet tool install Uno.DevServer");

		return new PackagedDevServerTool(rootDirectory, toolDirectory, packageVersion);
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
			// Best-effort cleanup for package-backed integration tests.
		}
	}

	private static void RunDotNet(string arguments, string workingDirectory, string operation)
	{
		var command = new CliCommandSpec("dotnet", arguments.SplitArguments());
		CliCommandRunner.Run(command, workingDirectory).EnsureSuccess(operation);
	}

	private static string Quote(string path) => $"\"{path}\"";
}

internal static class CommandLineSplitter
{
	public static IReadOnlyList<string> SplitArguments(this string commandLine)
	{
		var arguments = new List<string>();
		var current = new System.Text.StringBuilder();
		var inQuotes = false;

		foreach (var character in commandLine)
		{
			if (character == '"')
			{
				inQuotes = !inQuotes;
				continue;
			}

			if (!inQuotes && char.IsWhiteSpace(character))
			{
				if (current.Length > 0)
				{
					arguments.Add(current.ToString());
					current.Clear();
				}

				continue;
			}

			current.Append(character);
		}

		if (current.Length > 0)
		{
			arguments.Add(current.ToString());
		}

		return arguments;
	}
}
