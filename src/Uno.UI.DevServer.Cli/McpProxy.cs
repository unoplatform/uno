using System.Collections.Concurrent;
using System.CommandLine;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;

namespace Uno.UI.DevServer.Cli;

internal class McpProxy
{
	private readonly Microsoft.Extensions.Logging.ILogger _logger; // Use MS ILogger with LogX methods

	public McpProxy(Microsoft.Extensions.Logging.ILogger logger) => _logger = logger;

	public async Task<int> RunAsync(string hostPath, int port, List<string> forwardedArgs, CancellationToken ct)
	{
		try
		{
			var (flowControl, value) = await StartProcess(hostPath, port, forwardedArgs, ct);
			if (!flowControl)
			{
				return value ?? 1;
			}

			_logger.LogInformation("DevServer started on port {Port}", port);
			var remoteEndpoint = $"http://localhost:{port}/mcp";
			_logger.LogInformation("Starting MCP stdio proxy to {Endpoint}", remoteEndpoint);

			return await StartMcpStdIoProxyAsync(remoteEndpoint, ct);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "MCP proxy error: {Message}", ex.Message);
			return 1;
		}
	}

	private async Task<(bool success, int? exitCode)> StartProcess(string hostPath, int port, List<string> forwardedArgs, CancellationToken ct)
	{
		var useDotnet = !RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || hostPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);

		var startInfo = new ProcessStartInfo
		{
			FileName = useDotnet ? "dotnet" : hostPath,
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			RedirectStandardInput = true, // important, otherwise stdin stays captured and the mcp server can't use it
			CreateNoWindow = true,
			WorkingDirectory = Directory.GetCurrentDirectory(),
		};

		// If invoking via dotnet and the package provides an .exe, switch to the .dll equivalent
		var hostArgPath = hostPath;
		if (useDotnet)
		{
			if (hostArgPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
			{
				hostArgPath = Path.ChangeExtension(hostArgPath, ".dll");
			}

			startInfo.ArgumentList.Add(hostArgPath);
		}

		startInfo.ArgumentList.Add("--command");
		startInfo.ArgumentList.Add("start");
		startInfo.ArgumentList.Add("--httpPort");
		startInfo.ArgumentList.Add(port.ToString(System.Globalization.CultureInfo.InvariantCulture));

		// Tie the lifetime of the devserver to this MCP server
		startInfo.ArgumentList.Add("--ppid");
		startInfo.ArgumentList.Add(Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture));

		foreach (var fa in forwardedArgs)
		{
			startInfo.ArgumentList.Add(fa);
		}

		_logger.LogInformation("Launching devserver controller: {File} {Args}", startInfo.FileName, string.Join(" ", startInfo.ArgumentList));

		var controller = Process.Start(startInfo);
		if (controller is null)
		{
			_logger.LogError("Failed to start devserver controller process");
			return (success: false, exitCode: 1);
		}

		var stdoutCapture = controller.StandardOutput.ReadToEndAsync();
		var stderrCapture = controller.StandardError.ReadToEndAsync();

		// We launch the devserver controller, which in turn launches the actual devserver.
		await controller.WaitForExitAsync(ct);

		if (controller.ExitCode != 0)
		{
			_logger.LogError("DevServer controller exited with code {ExitCode}", controller.ExitCode);
			var so = await stdoutCapture;
			var se = await stderrCapture;

			if (!string.IsNullOrWhiteSpace(so))
			{
				_logger.LogDebug("Controller stdout:\n{Stdout}", so);
			}

			if (!string.IsNullOrWhiteSpace(se))
			{
				_logger.LogError("Controller stderr:\n{Stderr}", se);
			}

			return (success: false, exitCode: controller.ExitCode);
		}

		controller.Dispose();

		return (success: true, exitCode: null);
	}

	private async Task<int> StartMcpStdIoProxyAsync(string remoteEndpoint, CancellationToken ct)
	{
		var builder = Host.CreateApplicationBuilder();

		IMcpClient? upstreamClient = null;

		builder.Services
			.AddMcpServer()
			.WithStdioServerTransport()
			.WithCallToolHandler(async (ctx, ct) =>
			{
				_logger.LogDebug("Invoking MCP tool {Tool}", ctx.Params!.Name);

				var name = ctx.Params!.Name;
				var args = ctx.Params.Arguments ?? new Dictionary<string, JsonElement>();
				var adjustedArguments = args.ToDictionary(v => v.Key, v => (object?)v.Value);

				var result = await upstreamClient!.CallToolAsync(
					name,
					adjustedArguments,
					cancellationToken: ct
				);

				return result;
			})
			.WithListToolsHandler(async (ctx, ct) =>
			{
				_logger.LogTrace("Client requested tools list update");

				var list = await upstreamClient!.ListToolsAsync(cancellationToken: ct);

				_logger.LogDebug("Reporting {Count} tools", list.Count);

				return new ListToolsResult
				{
					Tools = list.Select(t => t.ProtocolTool).ToList()
				};
			});

		builder.Logging.AddConsole(consoleLogOptions =>
		{
			// Configure all logs to go to stderr
			consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
		});

		var host = builder.Build();

		var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("ToolPoller");

		upstreamClient = await ConnectOrDieAsync(
			remoteEndpoint,
			async () =>
			{
				await host.Services.GetRequiredService<IMcpServer>().SendNotificationAsync(
					NotificationMethods.ToolListChangedNotification,
					new ResourceUpdatedNotificationParams()
				);
			},
			host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("UpstreamConnector")
		);

		try
		{
			await host.RunAsync();
		}
		finally
		{
			if (upstreamClient != null)
			{
				await upstreamClient.DisposeAsync();
			}
		}

		return 0;
	}

	static async Task<IMcpClient> ConnectOrDieAsync(string url, Action toolsListChanged, Microsoft.Extensions.Logging.ILogger log)
	{
		try
		{
			var clientTransport = new SseClientTransport(new()
			{
				Name = "Devserver",
				Endpoint = new Uri(url),
				TransportMode = HttpTransportMode.StreamableHttp,
			});

			var options = new McpClientOptions
			{
				ClientInfo = new Implementation
				{
					Name = "stdio-http-proxy",
					Version = "1.0.0",
				},
				Capabilities = new()
				{
					NotificationHandlers =
					[
						new(NotificationMethods.ToolListChangedNotification, (notification, cancellationToken) =>
						{
							var notificationParams = JsonSerializer.Deserialize<ResourceUpdatedNotificationParams>(notification.Params, McpJsonUtilities.DefaultOptions);

							toolsListChanged();

							return default;
						})
					],
				},
			};

			log.LogInformation("Connecting to upstream MCP at {Url}", url);
			var client = await McpClientFactory.CreateAsync(clientTransport, options);
			log.LogInformation("Connected to upstream: {Name} {Version}", client.ServerInfo.Name, client.ServerInfo.Version);
			return client;
		}
		catch (Exception ex)
		{
			log.LogWarning(ex, "Upstream connect failed");
			throw;
		}
	}
}
