using System.Diagnostics;
using System.Text;
using AwesomeAssertions;

namespace Uno.UI.DevServer.Cli.Tests.Mcp;

[TestClass]
public class Given_CliCommandProcess
{
	[TestMethod]
	[Description("list does not auto-discover a nested Uno workspace when invoked from an unresolved parent directory")]
	public async Task List_WhenNestedWorkspaceExists_DoesNotResolveWorkspaceBeforeInvokingHost()
	{
		var root = CreateTempDirectory();
		var bogusVersion = "0.0.0-dev-ci-test";

		try
		{
			CreateNestedUnoWorkspace(root, bogusVersion);

			var result = await RunCliCommandAsync(root, "list");

			result.ExitCode.Should().Be(1);
			result.CombinedOutput.Should().Contain("Could not determine SDK version from global.json.");
			result.CombinedOutput.Should().NotContain(bogusVersion);
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	[TestMethod]
	[Description("cleanup does not auto-discover a nested Uno workspace when invoked from an unresolved parent directory")]
	public async Task Cleanup_WhenNestedWorkspaceExists_DoesNotResolveWorkspaceBeforeInvokingHost()
	{
		var root = CreateTempDirectory();
		var bogusVersion = "0.0.0-dev-ci-test";

		try
		{
			CreateNestedUnoWorkspace(root, bogusVersion);

			var result = await RunCliCommandAsync(root, "cleanup");

			result.ExitCode.Should().Be(1);
			result.CombinedOutput.Should().Contain("Could not determine SDK version from global.json.");
			result.CombinedOutput.Should().NotContain(bogusVersion);
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	private static void CreateNestedUnoWorkspace(string root, string sdkVersion)
	{
		var workspace = Path.Combine(root, "nested-uno-workspace");
		Directory.CreateDirectory(workspace);
		File.WriteAllText(
			Path.Combine(workspace, "global.json"),
			$$"""
			{
			  "msbuild-sdks": {
			    "Uno.Sdk": "{{sdkVersion}}"
			  }
			}
			""");
		File.WriteAllText(Path.Combine(workspace, "NestedApp.slnx"), string.Empty);
	}

	private static async Task<(int ExitCode, string CombinedOutput)> RunCliCommandAsync(string workingDirectory, string command)
	{
		var startInfo = new ProcessStartInfo("dotnet")
		{
			WorkingDirectory = workingDirectory,
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			StandardOutputEncoding = Encoding.UTF8,
			StandardErrorEncoding = Encoding.UTF8,
		};

		startInfo.ArgumentList.Add(GetCliDllPath());
		startInfo.ArgumentList.Add(command);

		using var process = Process.Start(startInfo);
		process.Should().NotBeNull();

		var stdoutTask = process!.StandardOutput.ReadToEndAsync();
		var stderrTask = process.StandardError.ReadToEndAsync();

		await process.WaitForExitAsync();

		var stdout = await stdoutTask;
		var stderr = await stderrTask;
		return (process.ExitCode, stdout + stderr);
	}

	private static string GetCliDllPath()
	{
		var path = Path.Combine(AppContext.BaseDirectory, "Uno.UI.DevServer.Cli.dll");
		if (!File.Exists(path))
		{
			throw new FileNotFoundException($"Unable to locate CLI test dependency at '{path}'.", path);
		}

		return path;
	}

	private static string CreateTempDirectory()
	{
		var path = Path.Combine(Path.GetTempPath(), $"uno-cli-process-tests-{Guid.NewGuid():N}");
		Directory.CreateDirectory(path);
		return path;
	}
}
