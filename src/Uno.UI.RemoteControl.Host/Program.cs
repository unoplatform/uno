using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mono.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Versioning;
using Uno.Extensions;
using Uno.UI.RemoteControl.Host.Extensibility;
using Uno.UI.RemoteControl.Host.IdeChannel;
using Uno.UI.RemoteControl.Server.Helpers;
using Uno.UI.RemoteControl.Server.Telemetry;
using Uno.UI.RemoteControl.Services;
using Uno.UI.RemoteControl.Helpers;
using Uno.UI.RemoteControl.Server.AppLaunch;

namespace Uno.UI.RemoteControl.Host
{
	partial class Program
	{
		static async Task Main(string[] args)
		{
			var startTime = Stopwatch.GetTimestamp();

			ITelemetry? telemetry = null;

			using var ct = ConsoleHelper.CreateCancellationToken();
			AmbientRegistry? ambientRegistry = null;

			try
			{
				var httpPort = 0;
				var parentPID = 0;
				var solution = default(string);
				var ideChannel = Guid.Empty;
				var command = default(string);
				var workingDir = default(string);
				var timeoutMs = 30000;

				var p = new OptionSet
				{
					{
						"httpPort=", s => {
							if(!int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out httpPort))
							{
								throw new ArgumentException($"The httpPort parameter is invalid {s}");
							}
						}
					},
					{
						"ppid=", s => {
							if(!int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out parentPID))
							{
								throw new ArgumentException($"The parent process id parameter is invalid {s}");
							}
						}
					},
					{
						"solution=", s =>
						{
							if (string.IsNullOrWhiteSpace(s) || !File.Exists(s))
							{
								throw new ArgumentException($"The provided solution path '{s}' does not exists");
							}

							solution = s;
						}
					},
					{
						"ideChannel=", s => {
							if(!Guid.TryParse(s, out ideChannel))
							{
								throw new ArgumentException($"The ide channel parameter is invalid {s}");
							}
						}
					},
					{
						"c|command=", s => command = s
					},
					{
						"workingDir=", s => workingDir = s
					},
					{
						"timeoutMs=", s => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out timeoutMs)
					}
				};

				p.Parse(args);

				// Controller mode
				if (!string.IsNullOrWhiteSpace(command))
				{
					var verb = command.ToLowerInvariant();
					switch (verb)
					{
						case "start":
							await StartCommandAsync(httpPort, parentPID, solution, workingDir, timeoutMs);
							return;
						case "stop":
							await StopCommandAsync();
							return;
						case "list":
							await ListCommandAsync();
							return;
						case "cleanup":
							await CleanupCommandAsync();
							return;
						default:
							await Console.Error.WriteLineAsync($"Unknown command '{command}'. Supported: start, stop, list, cleanup");
							Environment.ExitCode = 1;
							return;
					}
				}

				if (httpPort == 0)
				{
					throw new ArgumentException($"The httpPort parameter is required.");
				}

				const LogLevel logLevel = LogLevel.Debug;

				// During init, we dump the logs to the console, until the logger is set up
				Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(logLevel).AddConsole());

				// STEP 1: Create the global service provider BEFORE WebApplication
				// This contains services that live for the entire duration of the server process
				var globalServices = new ServiceCollection();

				// Add logging services to the global container
				// This is necessary for services like IdeChannelServer that require ILogger<T>
				globalServices.AddLogging(logging => logging
					.AddConsole()
					.SetMinimumLevel(LogLevel.Debug));

				globalServices.AddGlobalTelemetry(); // Global telemetry services (Singleton)
				globalServices.AddOptions<IdeChannelServerOptions>().Configure(opts => opts.ChannelId = ideChannel);
				globalServices.AddSingleton<IIdeChannel, IdeChannelServer>();

#pragma warning disable ASP0000 // Do not call ConfigureServices after calling UseKestrel.
				var globalServiceProvider = globalServices.BuildServiceProvider();
#pragma warning restore ASP0000

				telemetry = globalServiceProvider.GetRequiredService<ITelemetry>();

				// Force resolution of the IDEChannel to enable connection (Note: We should use a BackgroundService instead)
				// Note: The IDE channel is expected to inform IDE that we are up as soon as possible.
				//		 This is required for UDEI to **not** log invalid timeout message.
				globalServiceProvider.GetService<IIdeChannel>();

#pragma warning disable ASPDEPR004
				// STEP 2: Create the WebApplication builder with reference to the global service provider
				var builder = WebApplication.CreateBuilder(new WebApplicationOptions
				{
					Args = args,
					ContentRootPath = Directory.GetCurrentDirectory(),
				});

				// Configure Kestrel and URLs
				builder.WebHost
					.UseKestrel()
					.UseUrls($"http://*:{httpPort}/")
					.UseContentRoot(Directory.GetCurrentDirectory())
					.ConfigureLogging(logging =>
					{
						logging.ClearProviders();
						logging.AddConsole();
						logging.SetMinimumLevel(LogLevel.Debug);
						logging.AddFilter("Microsoft.", LogLevel.Information);
						logging.AddFilter("Microsoft.AspNetCore.Routing", LogLevel.Warning);
					})
					.ConfigureAppConfiguration((hostingContext, config) =>
					{
						config.AddCommandLine(args);
						config.AddEnvironmentVariables("UNO_DEVSERVER_");
					})
					.ConfigureServices(services =>
					{
						services.AddRouting();
						services.Configure<RemoteControlOptions>(builder.Configuration);
					});

				builder.Services.AddSingleton<IIdeChannel>(_ => globalServiceProvider.GetRequiredService<IIdeChannel>());
				builder.Services.AddSingleton<UnoDevEnvironmentService>();

				builder.Services.AddSingleton<ApplicationLaunchMonitor>(
					_ => globalServiceProvider.GetRequiredService<ApplicationLaunchMonitor>());

				// Add the global service provider to the DI container
				builder.Services.AddKeyedSingleton<IServiceProvider>("global", globalServiceProvider);

				// Add connection-specific telemetry services (Scoped)
				builder.Services.AddConnectionTelemetry(solution);

				// Apply Startup.ConfigureServices for compatibility with existing Startup class
				new Startup(builder.Configuration).ConfigureServices(builder.Services);

				if (solution is not null)
				{
					// For backward compatibility, we allow to not have a solution file specified.
					builder.ConfigureAddIns(solution, telemetry);
				}
				else
				{
					typeof(Program).Log().Log(LogLevel.Warning, "No solution file specified, add-ins will not be loaded which means that you won't be able to use any of the uno-studio features. Usually this indicates that your version of uno's IDE extension is too old.");
					builder.Services.AddSingleton(AddInsStatus.Empty);
				}
#pragma warning restore ASPDEPR004

#pragma warning disable ASPDEPR008
				// Ditto: https://github.com/aspnet/Announcements/issues/526
				var host = builder.Build();
#pragma warning restore ASPDEPR008

				// Apply Startup.Configure using Minimal APIs app instance
				new Startup(host.Configuration).Configure(host);

				// Once the app has started, we use the logger from the host
				LogExtensionPoint.AmbientLoggerFactory = host.Services.GetRequiredService<ILoggerFactory>();

				_ = host.Services.GetRequiredService<UnoDevEnvironmentService>().StartAsync(ct.Token); // Background services are not supported by WebHostBuilder

				// Display DevServer version banner
				var config = host.Services.GetRequiredService<IConfiguration>();
				var ideChannelId = config["ideChannel"]; // GUID used as the named pipe name when IDE channel is enabled
				DisplayVersionBanner(httpPort, ideChannelId);

				// STEP 3: Use global telemetry for server-wide events
				// Track devserver startup using global telemetry service
				var startupProperties = new Dictionary<string, string>
				{
					["StartupHasSolution"] = (solution != null).ToString(),
				};

				telemetry?.TrackEvent("startup", startupProperties, null);

				_ = ParentProcessObserver.ObserveAsync(parentPID, ct.Cancel, telemetry, ct.Token);

				ambientRegistry = new AmbientRegistry(host.Services.GetRequiredService<ILogger<AmbientRegistry>>());
				ambientRegistry.Register(solution, parentPID, httpPort);

				await host.StartAsync(ct.Token);
				try
				{
					await host.WaitForShutdownAsync(ct.Token);
				}
				finally
				{
					if (telemetry is not null)
					{
						// Track devserver shutdown with timing measurements
						var uptime = TimeSpan.FromTicks(Stopwatch.GetElapsedTime(startTime).Ticks);
						var shutdownProperties = new Dictionary<string, string>
						{
							["ShutdownType"] = ct.IsCancellationRequested ? "Graceful" : "Crash",
						};
						var shutdownMeasurements = new Dictionary<string, double>
						{
							["UptimeSeconds"] = uptime.TotalSeconds,
						};

						telemetry.TrackEvent("shutdown", shutdownProperties, shutdownMeasurements);
						await telemetry.FlushAsync(CancellationToken.None);
					}
				}
			}
			catch (Exception ex)
			{
				if (telemetry is not null)
				{
					// Track devserver startup failure
					var uptime = TimeSpan.FromTicks(Stopwatch.GetElapsedTime(startTime).Ticks);
					var errorProperties = new Dictionary<string, string>
					{
						["StartupErrorMessage"] = ex.Message,
						["StartupErrorType"] = ex.GetType().Name,
						["StartupStackTrace"] = ex.StackTrace ?? "",
					};
					var errorMeasurements = new Dictionary<string, double>
					{
						["UptimeSeconds"] = uptime.TotalSeconds,
					};

					telemetry.TrackEvent("startup-failure", errorProperties, errorMeasurements);
					await telemetry.FlushAsync(CancellationToken.None);
					throw;
				}
			}
			finally
			{
				ambientRegistry?.Unregister();
			}
		}

		/// <summary>
		/// Displays a banner with the DevServer version information when it starts up.
		/// </summary>
		private static void DisplayVersionBanner(int httpPort, string? ideChannelId)
		{
			try
			{
				var assembly = typeof(Program).Assembly;
				var version = VersionHelper.GetVersion(assembly);
				var assemblyName = assembly.GetName().Name ?? "Uno.UI.RemoteControl.Host";
				var location = assembly.Location;

				var targetFrameworkAttr = assembly.GetCustomAttribute<TargetFrameworkAttribute>();
				var runtimeText = targetFrameworkAttr is not null
					? $"dotnet v{Environment.Version} (Assembly target: {targetFrameworkAttr.FrameworkDisplayName})"
					: $"dotnet v{Environment.Version}";
#if DEBUG
				var lastWriteTime = File.GetLastWriteTime(location);
#endif

				var entries = new List<Host.Helpers.BannerHelper.BannerEntry>()
				{
#if DEBUG
					("Build", "DEBUG"),
					("Build Date/Time", $"{lastWriteTime:yyyy-MM-dd/HH:mm:ss} ({DateTime.Now - lastWriteTime:g} ago)"),
#endif
					("Version", version),
					("Runtime", runtimeText),
					("Assembly", assemblyName),
					("Location", Path.GetDirectoryName(location) ?? location, Helpers.BannerHelper.ClipMode.Start),
					("HTTP Port", httpPort.ToString(DateTimeFormatInfo.InvariantInfo)),
					("IDE Channel", string.IsNullOrWhiteSpace(ideChannelId) ? "Disabled" : $@"\\.\pipe\{ideChannelId}"),
				};

				Helpers.BannerHelper.Write("Uno Platform DevServer", entries);
			}
			catch (Exception ex)
			{
				// Log the error for debugging purposes
				Console.WriteLine($"Warning: Could not extract version information: {ex.Message}");
			}
		}
	}
}
