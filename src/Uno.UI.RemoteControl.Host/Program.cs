using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mono.Options;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.UI.RemoteControl.Host.Extensibility;
using Uno.UI.RemoteControl.Host.IdeChannel;
using Uno.UI.RemoteControl.Server.Helpers;
using Uno.UI.RemoteControl.Server.Telemetry;
using Uno.UI.RemoteControl.Services;

namespace Uno.UI.RemoteControl.Host
{
	class Program
	{
		static async Task Main(string[] args)
		{
			var startTime = Stopwatch.GetTimestamp();

			ITelemetry? telemetry = null;

			// Set up graceful shutdown handling
			using var cancellationTokenSource = new CancellationTokenSource();
			var shutdownRequested = false;

			Console.CancelKeyPress += (sender, e) =>
			{
				if (!shutdownRequested)
				{
					shutdownRequested = true;
					e.Cancel = true; // Prevent immediate termination
					Console.WriteLine("Graceful shutdown requested...");
					cancellationTokenSource.Cancel();
				}
			};

			// Monitor stdin for CTRL-C character (ASCII 3) for graceful shutdown
			_ = Task.Run(async () =>
			{
				try
				{
					if (!Console.IsInputRedirected)
						return;

					using var reader = new StreamReader(Console.OpenStandardInput());

					var buffer = new char[1];
					while (!cancellationTokenSource.Token.IsCancellationRequested)
					{
						var read = await reader.ReadAsync(buffer, 0, 1);
						if (read == 0)
						{
							break; // EOF
						}

						if (buffer[0] != '\x03') // CTRL-C (ASCII 3)
						{
							continue;
						}

						if (!shutdownRequested)
						{
							shutdownRequested = true;
							Console.WriteLine("Graceful shutdown requested via stdin...");
							await cancellationTokenSource.CancelAsync();
						}

						break;
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error monitoring stdin: {ex.Message}");
				}
			}, cancellationTokenSource.Token);

			try
			{
				var httpPort = 0;
				var parentPID = 0;
				var solution = default(string);

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
					}
				};

				p.Parse(args);

				if (httpPort == 0)
				{
					throw new ArgumentException($"The httpPort parameter is required.");
				}

				const LogLevel logLevel = LogLevel.Debug;

				// During init, we dump the logs to the console, until the logger is set up
				Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(logLevel).AddConsole());

				// STEP 1: Create the global service provider BEFORE WebHostBuilder
				// This contains services that live for the entire duration of the server process
				var globalServices = new ServiceCollection();

				// Add logging services to the global container
				// This is necessary for services like IdeChannelServer that require ILogger<T>
				globalServices.AddLogging(logging => logging
					.AddConsole()
					.SetMinimumLevel(LogLevel.Debug));

				globalServices.AddGlobalTelemetry(); // Global telemetry services (Singleton)

#pragma warning disable ASP0000 // Do not call ConfigureServices after calling UseKestrel.
				var globalServiceProvider = globalServices.BuildServiceProvider();
#pragma warning restore ASP0000

				telemetry = globalServiceProvider.GetRequiredService<ITelemetry>();

				// STEP 2: Create the WebHost with reference to the global service provider
				var builder = new WebHostBuilder()
					.UseSetting("UseIISIntegration", false.ToString())
					.UseKestrel()
					.UseUrls($"http://*:{httpPort}/")
					.UseContentRoot(Directory.GetCurrentDirectory())
					.UseStartup<Startup>()
					.ConfigureLogging(logging =>
						logging
							.ClearProviders()
							.AddConsole()
							.SetMinimumLevel(LogLevel.Debug))
					.ConfigureAppConfiguration((hostingContext, config) =>
					{
						config.AddCommandLine(args);
					})
					.ConfigureServices(services =>
					{
						services.AddSingleton<IIdeChannel, IdeChannelServer>();

						// Add the global service provider to the DI container
						services.AddKeyedSingleton<IServiceProvider>("global", globalServiceProvider);

						// Add connection-specific telemetry services (Scoped)
						services.AddConnectionTelemetry(solution);
					});

				if (solution is not null)
				{
					// For backward compatibility, we allow to not have a solution file specified.
					builder.ConfigureAddIns(solution, telemetry);
				}
				else
				{
					typeof(Program).Log().Log(LogLevel.Warning, "No solution file specified, add-ins will not be loaded which means that you won't be able to use any of the uno-studio features. Usually this indicates that your version of uno's IDE extension is too old.");
				}

				var host = builder.Build();

				// Once the app has started, we use the logger from the host
				Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = host.Services.GetRequiredService<ILoggerFactory>();

				host.Services.GetService<IIdeChannel>();

				// Display DevServer version banner
				DisplayVersionBanner();

				// STEP 3: Use global telemetry for server-wide events
				// Track devserver startup using global telemetry service
				var startupProperties = new Dictionary<string, string>
				{
					["devserver/Startup/HasSolution"] = (solution != null).ToString(),
				};

				telemetry?.TrackEvent("DevServer.Startup", startupProperties, null);

				_ = ParentProcessObserver.ObserveAsync(
					parentPID,
					() =>
					{
						shutdownRequested = true;
						cancellationTokenSource.Cancel();
					},
					telemetry,
					cancellationTokenSource.Token);

				try
				{
					await host.RunAsync(cancellationTokenSource.Token);
				}
				finally
				{
					if (telemetry is not null)
					{
						// Track devserver shutdown with timing measurements
						var uptime = TimeSpan.FromTicks(Stopwatch.GetElapsedTime(startTime).Ticks);
						var shutdownProperties = new Dictionary<string, string>
						{
							["devserver/ShutdownType"] = shutdownRequested ? "Graceful" : "Crash",
						};
						var shutdownMeasurements = new Dictionary<string, double>
						{
							["devserver/UptimeSeconds"] = uptime.TotalSeconds,
						};

						telemetry.TrackEvent("DevServer.Shutdown", shutdownProperties, shutdownMeasurements);
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
						["devserver/Startup/ErrorMessage"] = ex.Message,
						["devserver/Startup/ErrorType"] = ex.GetType().Name,
						["devserver/Startup/StackTrace"] = ex.StackTrace ?? "",
					};
					var errorMeasurements = new Dictionary<string, double>
					{
						["devserver/UptimeSeconds"] = uptime.TotalSeconds,
					};

					telemetry.TrackEvent("DevServer.StartupFailure", errorProperties, errorMeasurements);
					await telemetry.FlushAsync(CancellationToken.None);
					throw;
				}
			}
		}

		/// <summary>
		/// Displays a banner with the DevServer version information when it starts up.
		/// </summary>
		private static void DisplayVersionBanner()
		{
			try
			{
				var assembly = typeof(Program).Assembly;
				var version = assembly.GetName().Version?.ToString() ?? "Unknown";
				var assemblyName = assembly.GetName().Name ?? "Uno.UI.RemoteControl.Host";
				var location = assembly.Location;

				Console.WriteLine();
				Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
				Console.WriteLine("║                    Uno Platform DevServer                   ║");
				Console.WriteLine("╠══════════════════════════════════════════════════════════════╣");
				Console.WriteLine($"║ Version: {version,-47} ║");
				Console.WriteLine($"║ Assembly: {assemblyName,-46} ║");
				if (!string.IsNullOrEmpty(location))
				{
					var shortLocation = location.Length > 45 ? $"...{location.AsSpan(location.Length - 42)}" : location;
					Console.WriteLine($"║ Location: {shortLocation,-46} ║");
				}
				Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
				Console.WriteLine();
			}
			catch (Exception ex)
			{
				// Fallback in case of any issues with version extraction
				Console.WriteLine();
				Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
				Console.WriteLine("║                    Uno Platform DevServer                   ║");
				Console.WriteLine("║                         Started                              ║");
				Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
				Console.WriteLine();

				// Log the error for debugging purposes
				Console.WriteLine($"Warning: Could not extract version information: {ex.Message}");
			}
		}
	}
}
