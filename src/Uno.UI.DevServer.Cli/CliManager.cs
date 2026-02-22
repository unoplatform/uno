using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uno.UI.DevServer.Cli.Helpers;
using Uno.UI.DevServer.Cli.Mcp;

namespace Uno.UI.DevServer.Cli;

internal class CliManager
{
	private readonly IServiceProvider _services;
	private readonly UnoToolsLocator _unoToolsLocator;
	private readonly ILogger<CliManager> _logger;

	public CliManager(IServiceProvider services, UnoToolsLocator unoToolsLocator)
	{
		_services = services;
		_unoToolsLocator = unoToolsLocator;
		_logger = _services.GetRequiredService<ILogger<CliManager>>();
	}

	public async Task<int> RunAsync(string[] originalArgs)
	{
		try
		{
			var solutionDirParseResult = ExtractSolutionDirectory(originalArgs);
			// --solution-dir is applied uniformly so automation and CI environments can run any
			// command (start, stop, list, login, MCP) against a target solution even when the
			// current working directory differs from the solution root.
			if (!solutionDirParseResult.Success)
			{
				return 1;
			}

			originalArgs = solutionDirParseResult.FilteredArgs;
			var workingDirectory = solutionDirParseResult.SolutionDirectory ?? Environment.CurrentDirectory;

			if (originalArgs.Contains("--mcp-app"))
			{
				return await RunMcpProxyAsync(
					originalArgs.Where(a => a != "--mcp-app").ToArray(),
					workingDirectory,
					solutionDirParseResult.SolutionDirectory);
			}

			ShowBanner();

			if (originalArgs is { Length: > 0 } && string.Equals(originalArgs[0], "login", StringComparison.OrdinalIgnoreCase))
			{
				return await OpenSettings(originalArgs, workingDirectory);
			}

			var hostPath = await _unoToolsLocator.ResolveHostExecutableAsync(workingDirectory);

			if (hostPath is null)
			{
				return 1; // errors already logged
			}

			var requiresHostOutputPassthrough = originalArgs.Length > 0 && (
				string.Equals(originalArgs[0], "list", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(originalArgs[0], "cleanup", StringComparison.OrdinalIgnoreCase)
			);

			var startInfo = BuildHostArgs(hostPath, originalArgs, workingDirectory, redirectOutput: true);

			var result = await DevServerProcessHelper.RunConsoleProcessAsync(startInfo, _logger, forwardOutputToConsole: requiresHostOutputPassthrough);
			return result.ExitCode;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error running command: {ErrorMessage}", ex.Message);
			return 1;
		}
	}

	private void ShowBanner()
	{
		// get the assembly informational version
		var attrs = typeof(CliManager).Assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false);

		if (attrs.Length > 0 && attrs[0] is System.Reflection.AssemblyInformationalVersionAttribute versionAttr)
		{
			// Take only what's before a `+`, we don't want the commit hash here
			var items = versionAttr.InformationalVersion.Split('+', StringSplitOptions.RemoveEmptyEntries);

			_logger.LogInformation("Uno Platform DevServer CLI - Version {Version}", items[0]);
		}
		else
		{
			_logger.LogInformation("Uno Platform DevServer CLI - Dev Version");
		}
	}

	private async Task<int> OpenSettings(string[] originalArgs, string workingDirectory)
	{
		var studioExecutable = await _unoToolsLocator.ResolveSettingsExecutableAsync(workingDirectory);

		if (studioExecutable is null)
		{
			return 1; // errors already logged
		}

		var startInfo = DevServerProcessHelper.CreateDotnetProcessStartInfo(studioExecutable, originalArgs, workingDirectory, redirectOutput: true);

		var (exitCode, stdOut, stdErr) = await DevServerProcessHelper.RunGuiProcessAsync(startInfo, _logger, TimeSpan.FromSeconds(3));

		if (exitCode is not null)
		{
			// Display output for debugging purposes
			if (!string.IsNullOrWhiteSpace(stdOut))
			{
				_logger.LogDebug("Settings application stdout:\n{Stdout}", stdOut);
			}
			if (!string.IsNullOrWhiteSpace(stdErr))
			{
				_logger.LogError("Settings application stderr:\n{Stderr}", stdErr);
			}

			_logger.LogError("Settings application exited with code {ExitCode}", exitCode);

			return 1;
		}
		else
		{
			_logger.LogInformation("Settings application started successfully");
			return 0;
		}
	}

	private async Task<int> RunMcpProxyAsync(string[] args, string workingDirectory, string? solutionDirectory)
	{
		try
		{
			_logger.LogInformation("Starting MCP Mode");

			int requestedPort = 0;
			bool mcpWaitToolsList = false;
			bool forceRootsFallback = false;
			bool forceGenerateToolCache = false;
			var forwardedArgs = new List<string>();

			for (int i = 0; i < args.Length; i++)
			{
				var a = args[i];
				if (a == "--port" || a == "-p")
				{
					if (i + 1 >= args.Length)
					{
						_logger.LogError($"Missing value for {a}");
						return 1;
					}
					if (!int.TryParse(args[i + 1], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out requestedPort) || requestedPort <= 0 || requestedPort > 65535)
					{
						_logger.LogError($"Invalid port value '{args[i + 1]}'");
						return 1;
					}
					i++; // skip value
					continue; // do not forward port arguments to controller
				}
				else if (a == "--mcp-wait-tools-list")
				{
					mcpWaitToolsList = true;
					continue; // do not forward mcp-specific arguments to controller
				}
				else if (a == "--force-roots-fallback")
				{
					forceRootsFallback = true;
					continue; // do not forward mcp-specific arguments to controller
				}
				else if (a == "--force-generate-tool-cache")
				{
					forceGenerateToolCache = true;
					continue; // do not forward mcp-specific arguments to controller
				}
				forwardedArgs.Add(a);
			}

			var normalizedSolutionDirectory = solutionDirectory ?? (forceGenerateToolCache ? workingDirectory : null);

			var waitForTools = mcpWaitToolsList;
			return await _services.GetRequiredService<McpProxy>().RunAsync(
				workingDirectory,
				requestedPort,
				forwardedArgs,
				waitForTools,
				forceRootsFallback,
				forceGenerateToolCache,
				normalizedSolutionDirectory,
				CancellationToken.None);
		}
		catch (Exception ex)
		{
			_logger.LogError($"MCP proxy error: {ex.Message}");
			return 1;
		}
	}

	private (bool Success, string[] FilteredArgs, string? SolutionDirectory) ExtractSolutionDirectory(string[] args)
	{
		string? rawSolutionDirectory = null;
		var filteredArgs = new List<string>(args.Length);

		for (int i = 0; i < args.Length; i++)
		{
			var arg = args[i];
			if (arg == "--solution-dir")
			{
				if (i + 1 >= args.Length)
				{
					_logger.LogError("Missing value for --solution-dir");
					return (false, Array.Empty<string>(), null);
				}

				rawSolutionDirectory = args[i + 1];
				i++; // skip value
				continue;
			}

			filteredArgs.Add(arg);
		}

		string? normalizedSolutionDirectory = null;
		if (!string.IsNullOrWhiteSpace(rawSolutionDirectory))
		{
			try
			{
				normalizedSolutionDirectory = Path.GetFullPath(rawSolutionDirectory);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Invalid solution directory '{Directory}'", rawSolutionDirectory);
				return (false, Array.Empty<string>(), null);
			}
		}

		return (true, filteredArgs.ToArray(), normalizedSolutionDirectory);
	}

	private ProcessStartInfo BuildHostArgs(string hostPath, string[] originalArgs, string workingDirectory, bool redirectOutput = true)
	{
		var args = new List<string> { "--command" };
		if (originalArgs.Length > 0)
		{
			args.Add(originalArgs[0]);
			for (int i = 1; i < originalArgs.Length; i++)
			{
				args.Add(originalArgs[i]);
			}
		}
		else
		{
			args.Add("start");
		}

		return DevServerProcessHelper.CreateDotnetProcessStartInfo(hostPath, args, workingDirectory, redirectOutput);
	}
}
