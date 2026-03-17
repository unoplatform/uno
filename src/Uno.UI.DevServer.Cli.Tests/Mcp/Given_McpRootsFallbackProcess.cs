using System.Diagnostics;
using System.Linq;
using System.Text;
using AwesomeAssertions;

namespace Uno.UI.DevServer.Cli.Tests.Mcp;

[TestClass]
public class Given_McpRootsFallbackProcess
{
	[TestMethod]
	[Description("The MCP proxy starts the deferred DevServer after uno_app_set_roots is called in force-roots-fallback mode")]
	public async Task WhenRootsAreProvidedViaToolCall_DeferredDevServerStarts()
	{
		var cliDllPath = GetCliDllPath();
		var workspaceDirectory = GetTemplateWorkspaceDirectory();
		var port = GetFreeTcpPort();
		var stdout = new StringBuilder();
		var stderr = new StringBuilder();
		Process? process = null;

		try
		{
			await StopDevServerAsync(cliDllPath, workspaceDirectory);

			process = StartMcpProxy(cliDllPath, workspaceDirectory, port, stdout, stderr);

			await Task.Delay(TimeSpan.FromSeconds(3));

			stderr.ToString().Should().NotContain("Starting DevServer monitor using solution directory", "roots fallback should defer monitor startup until roots are provided");

			await SendInitializeAsync(process);
			await WaitForInitializeResponseAsync(stdout);
			await SendSetRootsAsync(process, workspaceDirectory);

			var started = await WaitUntilAsync(
				() => stderr.ToString().Contains("Starting DevServer monitor using solution directory", StringComparison.Ordinal),
				timeout: TimeSpan.FromSeconds(60));

			if (!started)
			{
				Assert.Fail($"Expected deferred DevServer to start after roots were provided.{Environment.NewLine}STDOUT:{Environment.NewLine}{stdout}{Environment.NewLine}STDERR:{Environment.NewLine}{stderr}");
			}
		}
		finally
		{
			if (process is not null && !process.HasExited)
			{
				try
				{
					process.Kill(entireProcessTree: true);
				}
				catch
				{
					// Best-effort cleanup for integration test teardown.
				}

				try
				{
					await process.WaitForExitAsync();
				}
				catch
				{
					// Ignore teardown wait failures.
				}
			}

			await StopDevServerAsync(cliDllPath, workspaceDirectory);
		}
	}

	[TestMethod]
	[Description("The MCP proxy starts the selected DevServer after uno_app_select_solution is called in force-roots-fallback mode")]
	public async Task WhenSolutionIsSelectedViaToolCall_DeferredDevServerStarts()
	{
		var cliDllPath = GetCliDllPath();
		var workspaceDirectory = GetTemplateWorkspaceDirectory();
		var solutionPath = Directory.EnumerateFiles(workspaceDirectory, "*.slnx").FirstOrDefault()
			?? Directory.EnumerateFiles(workspaceDirectory, "*.sln").First();
		var port = GetFreeTcpPort();
		var stdout = new StringBuilder();
		var stderr = new StringBuilder();
		Process? process = null;

		try
		{
			await StopDevServerAsync(cliDllPath, workspaceDirectory);

			process = StartMcpProxy(cliDllPath, workspaceDirectory, port, stdout, stderr);

			await Task.Delay(TimeSpan.FromSeconds(3));

			stderr.ToString().Should().NotContain("Starting DevServer monitor using solution directory", "force-roots-fallback should still defer monitor startup until a selection command is provided");

			await SendInitializeAsync(process);
			await WaitForInitializeResponseAsync(stdout);
			await SendSelectSolutionAsync(process, solutionPath);

			var started = await WaitUntilAsync(
				() => stderr.ToString().Contains("Starting DevServer monitor using solution directory", StringComparison.Ordinal),
				timeout: TimeSpan.FromSeconds(60));

			if (!started)
			{
				Assert.Fail($"Expected deferred DevServer to start after solution selection was provided.{Environment.NewLine}STDOUT:{Environment.NewLine}{stdout}{Environment.NewLine}STDERR:{Environment.NewLine}{stderr}");
			}
		}
		finally
		{
			if (process is not null && !process.HasExited)
			{
				try
				{
					process.Kill(entireProcessTree: true);
				}
				catch
				{
					// Best-effort cleanup for integration test teardown.
				}

				try
				{
					await process.WaitForExitAsync();
				}
				catch
				{
					// Ignore teardown wait failures.
				}
			}

			await StopDevServerAsync(cliDllPath, workspaceDirectory);
		}
	}

	private static Process StartMcpProxy(string cliDllPath, string workingDirectory, int port, StringBuilder stdout, StringBuilder stderr)
	{
		var startInfo = new ProcessStartInfo("dotnet")
		{
			WorkingDirectory = workingDirectory,
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardInput = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			StandardOutputEncoding = Encoding.UTF8,
			StandardErrorEncoding = Encoding.UTF8,
		};

		foreach (var argument in new[] { cliDllPath, "--mcp-app", "--force-roots-fallback", "--port", port.ToString(), "-l", "trace" })
		{
			startInfo.ArgumentList.Add(argument);
		}

		var process = new Process
		{
			StartInfo = startInfo,
			EnableRaisingEvents = true,
		};

		process.Start().Should().BeTrue("the MCP proxy process must start for the integration test");
		process.OutputDataReceived += (_, args) =>
		{
			if (args.Data is not null)
			{
				stdout.AppendLine(args.Data);
			}
		};
		process.ErrorDataReceived += (_, args) =>
		{
			if (args.Data is not null)
			{
				stderr.AppendLine(args.Data);
			}
		};
		process.BeginOutputReadLine();
		process.BeginErrorReadLine();
		return process;
	}

	private static async Task SendInitializeAsync(Process process)
	{
		var request = """
			{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test-client","version":"1.0.0"}}}
			""";
		await process.StandardInput.WriteLineAsync(request);
		await process.StandardInput.FlushAsync();

		var initializedNotification = """
			{"jsonrpc":"2.0","method":"notifications/initialized"}
			""";
		await process.StandardInput.WriteLineAsync(initializedNotification);
		await process.StandardInput.FlushAsync();
	}

	private static async Task SendSetRootsAsync(Process process, string workspaceDirectory)
	{
		var request = System.Text.Json.JsonSerializer.Serialize(new
		{
			jsonrpc = "2.0",
			id = 2,
			method = "tools/call",
			@params = new
			{
				name = "uno_app_set_roots",
				arguments = new
				{
					roots = new[] { workspaceDirectory }
				}
			}
		});
		await process.StandardInput.WriteLineAsync(request);
		await process.StandardInput.FlushAsync();
	}

	private static async Task SendSelectSolutionAsync(Process process, string solutionPath)
	{
		var request = System.Text.Json.JsonSerializer.Serialize(new
		{
			jsonrpc = "2.0",
			id = 3,
			method = "tools/call",
			@params = new
			{
				name = "uno_app_select_solution",
				arguments = new
				{
					solutionPath
				}
			}
		});
		await process.StandardInput.WriteLineAsync(request);
		await process.StandardInput.FlushAsync();
	}

	private static async Task<bool> WaitUntilAsync(Func<bool> condition, TimeSpan timeout, TimeSpan? pollInterval = null)
	{
		var delay = pollInterval ?? TimeSpan.FromSeconds(2);
		var deadline = DateTime.UtcNow + timeout;
		while (DateTime.UtcNow < deadline)
		{
			if (condition())
			{
				return true;
			}

			await Task.Delay(delay);
		}

		return condition();
	}

	private static async Task WaitForInitializeResponseAsync(StringBuilder stdout)
	{
		var initialized = await WaitUntilAsync(
			() => stdout.ToString().Contains("\"id\":1", StringComparison.Ordinal),
			timeout: TimeSpan.FromSeconds(10),
			pollInterval: TimeSpan.FromMilliseconds(100));

		initialized.Should().BeTrue("the MCP initialize response should be observed before sending follow-up tool calls");
	}

	private static async Task StopDevServerAsync(string cliDllPath, string workspaceDirectory)
	{
		var startInfo = new ProcessStartInfo("dotnet")
		{
			WorkingDirectory = workspaceDirectory,
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
		};

		foreach (var argument in new[] { cliDllPath, "stop", "--solution-dir", workspaceDirectory })
		{
			startInfo.ArgumentList.Add(argument);
		}

		using var process = Process.Start(startInfo);
		if (process is null)
		{
			return;
		}

		try
		{
			await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30));
		}
		catch (TimeoutException)
		{
			if (!process.HasExited)
			{
				try
				{
					process.Kill(entireProcessTree: true);
				}
				catch
				{
					// Ignore cleanup failures in test helper.
				}
			}
		}
	}

	private static int GetFreeTcpPort()
	{
		var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
		listener.Start();
		try
		{
			return ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
		}
		finally
		{
			listener.Stop();
		}
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

	private static string GetTemplateWorkspaceDirectory()
	{
		var current = new DirectoryInfo(AppContext.BaseDirectory);
		while (current is not null)
		{
			var candidate = Path.Combine(current.FullName, "src", "SolutionTemplate", "5.6", "uno56netcurrent");
			if (Directory.Exists(candidate))
			{
				return candidate;
			}

			current = current.Parent;
		}

		throw new DirectoryNotFoundException("Unable to locate the SolutionTemplate workspace used by the DevServer CLI integration tests.");
	}
}
