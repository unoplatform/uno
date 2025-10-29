using System.Diagnostics;
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
			if (originalArgs.Contains("--mcp"))
			{
				return await RunMcpProxyAsync(originalArgs.Where(a => a != "--mcp").ToArray());
			}

			ShowBanner();

			if (originalArgs is { Length: > 0 } && string.Equals(originalArgs[0], "login", StringComparison.OrdinalIgnoreCase))
			{
				return await OpenSettings(originalArgs);
			}

			var hostPath = await _unoToolsLocator.ResolveHostExecutableAsync();

			if (hostPath is null)
			{
				return 1; // errors already logged
			}

			var isDirectOutputCommand = originalArgs.Length > 0 && (
				string.Equals(originalArgs[0], "list", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(originalArgs[0], "cleanup", StringComparison.OrdinalIgnoreCase)
			);

			var startInfo = BuildHostArgs(hostPath, originalArgs, redirectOutput: !isDirectOutputCommand);

			var result = await DevServerProcessHelper.RunConsoleProcessAsync(startInfo, _logger);
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

	private async Task<int> OpenSettings(string[] originalArgs)
	{
		var studioExecutable = await _unoToolsLocator.ResolveSettingsExecutableAsync();

		if (studioExecutable is null)
		{
			return 1; // errors already logged
		}

		var startInfo = DevServerProcessHelper.CreateDotnetProcessStartInfo(studioExecutable, originalArgs, redirectOutput: true);

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

	private async Task<int> RunMcpProxyAsync(string[] args)
	{
		try
		{
			_logger.LogInformation("Starting MCP Mode");

			int requestedPort = 0;
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
				forwardedArgs.Add(a);
			}

			return await _services.GetRequiredService<McpProxy>().RunAsync(Environment.CurrentDirectory, requestedPort, forwardedArgs, CancellationToken.None);
		}
		catch (Exception ex)
		{
			_logger.LogError($"MCP proxy error: {ex.Message}");
			return 1;
		}
	}

	private ProcessStartInfo BuildHostArgs(string hostPath, string[] originalArgs, bool redirectOutput = true)
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

		return DevServerProcessHelper.CreateDotnetProcessStartInfo(hostPath, args, redirectOutput);
	}
}
