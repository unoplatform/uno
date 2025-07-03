using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mono.Options;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.UI.RemoteControl.Helpers;
using Uno.UI.RemoteControl.Host.Extensibility;
using Uno.UI.RemoteControl.Host.IdeChannel;
using Uno.UI.RemoteControl.Server.Telemetry;
using Uno.UI.RemoteControl.Services;

namespace Uno.UI.RemoteControl.Host
{
	class Program
	{
		static async Task Main(string[] args)
		{
			ITelemetry? telemetry = null;
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
					services.AddTelemetry();
				});

			if (solution is not null)
			{
				// For backward compatibility, we allow to not have a solution file specified.
				builder.ConfigureAddIns(solution);
			}
			else
			{
				typeof(Program).Log().Log(LogLevel.Warning, "No solution file specified, add-ins will not be loaded which means that you won't be able to use any of the uno-studio features. Usually this indicates that your version of uno's IDE extension is too old.");
			}

			var host = builder.Build();

			// Once the app has started, we use the logger from the host
			Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = host.Services.GetRequiredService<ILoggerFactory>();

			host.Services.GetService<IIdeChannel>();

				// Track devserver startup
				telemetry = host.Services.GetService<ITelemetry>();
				var startupProperties = new Dictionary<string, string>
				{
					["HasSolution"] = (solution != null).ToString(),
					["MachineName"] = Environment.MachineName,
					["OSVersion"] = Environment.OSVersion.ToString(),
					["ProcessorCount"] = Environment.ProcessorCount.ToString(CultureInfo.InvariantCulture)
				};

				telemetry?.TrackEvent("DevServer.Startup", startupProperties, null);

				using var parentObserver = ParentProcessObserver.Observe(host, parentPID);

				try
				{
					await host.RunAsync();
				}
				finally
				{
					// WARNING: THIS WILL WON'T WORK UNTIL THE SERVER IMPLEMENTS A KIND OF GRACEFUL SHUTDOWN
					// (currently the server is just getting killed without any due process)

					// Track devserver shutdown
					var shutdownProperties = new Dictionary<string, string>
					{
						["UpTime"] = (DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime()).ToString("c")
					};

					telemetry?.TrackEvent("DevServer.Shutdown", shutdownProperties, null);
					telemetry?.Flush();
				}
			}
			catch (Exception ex)
			{
				// Track devserver startup failure
				var errorProperties = new Dictionary<string, string>
				{
					["ErrorMessage"] = ex.Message,
					["ErrorType"] = ex.GetType().Name,
					["StackTrace"] = ex.StackTrace ?? "",
				};

				telemetry?.TrackEvent("DevServer.StartupFailure", errorProperties, null);
				telemetry?.Flush();
				throw;
			}
		}
	}
}
