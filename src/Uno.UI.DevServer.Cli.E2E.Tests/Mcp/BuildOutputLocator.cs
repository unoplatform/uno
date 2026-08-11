namespace Uno.UI.DevServer.Cli.E2E.Tests.Mcp;

internal static class BuildOutputLocator
{
	public static string ResolveCliProjectPath()
		=> Path.Combine(FindRepositoryRoot(), "src", "Uno.UI.DevServer.Cli", "Uno.UI.DevServer.Cli.csproj");

	public static string FindRepositoryRoot()
	{
		var current = new DirectoryInfo(AppContext.BaseDirectory);
		while (current is not null)
		{
			if (Directory.Exists(Path.Combine(current.FullName, "src", "Uno.UI.DevServer.Cli"))
				&& Directory.Exists(Path.Combine(current.FullName, "src", "Uno.UI.RemoteControl.Host")))
			{
				return current.FullName;
			}

			current = current.Parent;
		}

		throw new DirectoryNotFoundException("Unable to locate the repository root for DevServer CLI E2E tests.");
	}

	public static string ResolveCliDllPath()
	{
		var repoRoot = FindRepositoryRoot();
		var cliBinDirectory = Path.Combine(repoRoot, "src", "Uno.UI.DevServer.Cli", "bin");

		foreach (var configuration in new[] { "Debug", "Release" })
		{
			foreach (var targetFramework in new[] { "net10.0", "net11.0" })
			{
				var candidate = Path.Combine(cliBinDirectory, configuration, targetFramework, "Uno.UI.DevServer.Cli.dll");
				if (File.Exists(candidate))
				{
					return candidate;
				}
			}
		}

		throw new FileNotFoundException($"Unable to locate built Uno.UI.DevServer.Cli output under '{cliBinDirectory}'. Build the CLI project before running DevServer CLI E2E tests.");
	}
}
