using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Uno.Extensions;
using Uno.UI.RemoteControl.Helpers;
using Uno.UI.RemoteControl.Host.Extensibility;
using Uno.UI.RemoteControl.Host.Helpers;
using Uno.UI.RemoteControl.Host.IdeChannel;
using Uno.UI.RemoteControl.Host.Mcp;
using Uno.UI.RemoteControl.Server.AppLaunch;
using Uno.UI.RemoteControl.Server.Helpers;
using Uno.UI.RemoteControl.Server.Telemetry;
using Uno.UI.RemoteControl.Services;

namespace Uno.UI.RemoteControl.Host
{
	partial class Program
	{
		/// <summary>
		/// Shared-framework OOB assemblies that the host eager-loads from the
		/// apphost directory during static initialization. Anything in this list
		/// must satisfy two properties: (a) the bundled DLL exists in the host's
		/// output directory (i.e. the SDK doesn't strip it via PackageOverride),
		/// and (b) the host's own dependency graph or the add-ins it loads can
		/// realistically embed a cross-major-version AssemblyRef to it. Extend
		/// here as additional offenders are identified (e.g. another OOB package
		/// referenced by ModelContextProtocol or by future add-in payloads).
		/// </summary>
		private static readonly string[] s_eagerLoadedSharedAssemblyDllNames =
		{
			"System.Text.Json.dll",
			"System.Text.Encodings.Web.dll",
		};

		static Program()
		{
			// The host process targets a specific .NET runtime, but its NuGet
			// dependency graph (e.g. ModelContextProtocol 1.1.0) and the add-ins
			// it loads (e.g. Microsoft.Kiota.* net8.0 binaries) can reference
			// versions of shared framework OOB assemblies (System.Text.*,
			// Microsoft.Extensions.*) that don't match what the runtime
			// actually has loaded — typically a cross-major-version gap such as
			// System.Text.Encodings.Web v8.0.0.0 (Kiota net8.0) or v10.0.0.0
			// (compiled against net10 reference assemblies) against the host's
			// v9.0.0.0 from Microsoft.NETCore.App 9.x.
			//
			// .NET Core/5+ removed app.config binding redirects, so the runtime
			// fails such requests with FileNotFoundException by default. We
			// reinstate the equivalent behaviour by handling the default ALC's
			// Resolving event: when a request can't be satisfied by normal
			// probing, fall back to any already-loaded assembly with the same
			// simple name. This is invisible for well-behaved deps (Resolving
			// only fires on FAILURE) and matches the lax-binding semantics
			// developers used to get from binding redirects.
			AssemblyLoadContext.Default.Resolving += static (context, requested) =>
			{
				if (requested.Name is not { } simpleName)
				{
					return null;
				}

				var requestedToken = requested.GetPublicKeyToken();

				foreach (var loaded in context.Assemblies)
				{
					// Skip reflection-emitted / source-generator-emitted dynamic
					// assemblies: their auto-generated names could coincidentally
					// collide with a real AssemblyRef and we'd silently substitute
					// them, producing confusing downstream TypeLoadException /
					// MissingMethodException failures.
					if (loaded.IsDynamic)
					{
						continue;
					}

					var loadedName = loaded.GetName();
					if (!string.Equals(loadedName.Name, simpleName, StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}

					// Only bridge between assemblies with the same strong-name
					// identity (or both unsigned). Returning a differently-signed
					// assembly would be a security-relevant identity swap, not a
					// version-mismatch papered over.
					if (requestedToken is { Length: > 0 })
					{
						var loadedToken = loadedName.GetPublicKeyToken();
						if (loadedToken is null || !loadedToken.AsSpan().SequenceEqual(requestedToken))
						{
							continue;
						}
					}

					return loaded;
				}

				return null;
			};

			// Eager-load shared-framework OOB assemblies most prone to
			// cross-major-version requests *by file path* from the apphost
			// directory. The Resolving handler above can only return assemblies
			// already in the load context, so we have to populate it before any
			// versioned request fires.
			//
			// Loading by AssemblyName instead would hit .NET 9's "platform
			// assembly" override for System.Text.Encodings.Web — the framework's
			// v9 instance is forced into TPA even when the host's deps.json (and
			// bundled DLL) is v10 — and a simple-name load then fails outright.
			// `typeof(...).Assembly` is also unsafe here: with this project
			// built by the .NET 10 SDK, typeof emits a v10 AssemblyRef which
			// would itself trigger the very FileNotFoundException we want to
			// prevent. Loading the apphost-dir file directly avoids both traps
			// and gives the Resolving handler a candidate to satisfy later
			// versioned requests from add-ins (Kiota net8.0 → v8.0.0.0) and
			// from the host's own deps (ModelContextProtocol 1.1.0 → v10.0.0.0)
			// without forcing every add-in to ship its own copy.
			var hostDir = Path.GetDirectoryName(typeof(Program).Assembly.Location);
			if (!string.IsNullOrEmpty(hostDir))
			{
				foreach (var dllName in s_eagerLoadedSharedAssemblyDllNames)
				{
					var path = Path.Combine(hostDir, dllName);
					if (!File.Exists(path))
					{
						continue;
					}

					try
					{
						AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
					}
					catch (Exception ex)
					{
						// Best effort: if the bundled file can't be loaded the
						// Resolving handler simply won't have a candidate later
						// and the original failure will surface as normal. Log a
						// breadcrumb so a regression in this preload step doesn't
						// silently disable the safety net — the host hasn't built
						// its ILoggerFactory yet at this point, so write directly
						// to stderr.
						Console.Error.WriteLine(
							$"Uno.UI.RemoteControl.Host: eager-load of '{dllName}' failed ({ex.GetType().Name}: {ex.Message}). " +
							"Cross-major-version AssemblyRefs for this assembly may still throw FileNotFoundException at runtime.");
					}
				}
			}
		}

		static async Task Main(string[] args)
		{
			var startTime = Stopwatch.GetTimestamp();

			ITelemetry? telemetry = null;

			using var ct = ConsoleHelper.CreateCancellationToken();
			AmbientRegistry? ambientRegistry = null;

			try
			{
				var switchMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
				{
					["-c"] = "command",
				};

				var globalConfiguration = new ConfigurationBuilder()
					.AddEnvironmentVariables("UNO_DEVSERVER_")
					.AddCommandLine(args, switchMappings)
					.Build();

				var httpPort = globalConfiguration.ParseOptionalInt("httpPort");
				var parentPID = globalConfiguration.ParseOptionalInt("ppid");
				var command = globalConfiguration.GetOptionalString("command");
				var workingDir = globalConfiguration.GetOptionalString("workingDir");
				var timeoutMs = globalConfiguration.ParseIntOrDefault("timeoutMs", 30000);

				var solution = globalConfiguration.GetOptionalString("solution");
				if (!string.IsNullOrWhiteSpace(solution) && !File.Exists(solution))
				{
					throw new ArgumentException($"The provided solution path '{solution}' does not exist");
				}

				// Read --addins: pre-resolved add-in DLL paths (semicolon-separated).
				// When present, MSBuild-based discovery is skipped entirely.
				var addins = globalConfiguration.GetAddinsValue("addins");

				var ideChannel = globalConfiguration.GetOptionalString("ideChannel");

				// Controller mode
				if (!string.IsNullOrWhiteSpace(command))
				{
					var verb = command.ToLowerInvariant();
					switch (verb)
					{
						case "start":
							await StartCommandAsync(httpPort, parentPID, solution, workingDir, timeoutMs, addins, ideChannel);
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
							await Console.Error.WriteLineAsync(
								$"Unknown command '{command}'. Supported: start, stop, list, cleanup");
							Environment.ExitCode = 1;
							return;
					}
				}

				if (httpPort == 0)
				{
					throw new ArgumentException("The httpPort parameter is required.");
				}

				const LogLevel logLevel = LogLevel.Debug;

				// During init, we dump the logs to the console, until the logger is set up
				Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory =
					LoggerFactory.Create(builder => builder.SetMinimumLevel(logLevel).AddConsole());

				// STEP 1: Create the global service provider BEFORE WebApplication
				// This contains services that live for the entire duration of the server process
				var globalServices = new ServiceCollection();
				globalServices.AddSingleton<IConfiguration>(globalConfiguration);

				// Add logging services to the global container
				// This is necessary for services like IdeChannelServer that require ILogger<T>
				globalServices.AddLogging(logging => logging
					.AddConsole()
					.SetMinimumLevel(LogLevel.Debug));

				globalServices.AddGlobalTelemetry(); // Global telemetry services (Singleton)
				globalServices.AddOptions<IdeChannelServerOptions>()
					.Configure<IConfiguration>((opts, configuration) =>
					{
						opts.ChannelId = configuration.GetOptionalString("ideChannel");
					});
				globalServices.AddSingleton<IdeChannelServer>();
				globalServices.AddSingleton<IIdeChannel>(sp => sp.GetRequiredService<IdeChannelServer>());
				globalServices.AddSingleton<IIdeChannelManager>(sp => sp.GetRequiredService<IdeChannelServer>());
				globalServices.AddSingleton(sp =>
					new AmbientRegistry(sp.GetRequiredService<ILoggerFactory>().CreateLogger<AmbientRegistry>()));

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
						config.AddConfiguration(globalConfiguration);
					})
					.ConfigureServices(services =>
					{
						services.AddRouting();
						services.Configure<RemoteControlOptions>(builder.Configuration);
					});

				builder.Services.AddSingleton<IIdeChannel>(_ =>
					globalServiceProvider.GetRequiredService<IIdeChannel>());
				builder.Services.AddSingleton<IIdeChannelManager>(_ =>
					globalServiceProvider.GetRequiredService<IIdeChannelManager>());
				builder.Services.AddSingleton<UnoDevEnvironmentService>();
				builder.Services.AddSingleton(_ =>
					globalServiceProvider.GetRequiredService<AmbientRegistry>());

				builder.Services.AddSingleton<ApplicationLaunchMonitor>(_ =>
					globalServiceProvider.GetRequiredService<ApplicationLaunchMonitor>());

				// Add the global service provider to the DI container
				builder.Services.AddKeyedSingleton<IServiceProvider>("global", globalServiceProvider);

				// Add connection-specific telemetry services (Scoped)
				builder.Services.AddConnectionTelemetry(solution);

				// Apply Startup.ConfigureServices for compatibility with existing Startup class
				new Startup(builder.Configuration).ConfigureServices(builder.Services);

				if (addins is not null)
				{
					// Pre-resolved add-in paths from CLI (convention-based discovery).
					// Skip MSBuild-based discovery entirely.
					builder.ConfigureAddInsFromPaths(addins, telemetry);
				}
				else if (solution is not null)
				{
					// For backward compatibility, we allow to not have a solution file specified.
					builder.ConfigureAddIns(solution, telemetry);
				}
				else
				{
					typeof(Program).Log().Log(LogLevel.Warning,
						"No solution file specified, add-ins will not be loaded which means that you won't be able to use any of the uno-studio features. Usually this indicates that your version of uno's IDE extension is too old.");
					builder.Services.AddSingleton(AddInsStatus.Empty);
				}
#pragma warning restore ASPDEPR004

				// Register Host-level MCP health tool and resource.
				// AddMcpServer() uses TryAdd* internally, safe even if add-ins already called it.
				HostHealthTool.Configure(builder.Services);

#pragma warning disable ASPDEPR008
				// Ditto: https://github.com/aspnet/Announcements/issues/526
				var host = builder.Build();
#pragma warning restore ASPDEPR008

				// Apply Startup.Configure using Minimal APIs app instance
				new Startup(host.Configuration).Configure(host);

				// Once the app has started, we use the logger from the host
				LogExtensionPoint.AmbientLoggerFactory = host.Services.GetRequiredService<ILoggerFactory>();

				_ = host.Services.GetRequiredService<UnoDevEnvironmentService>()
					.StartAsync(ct.Token); // Background services are not supported by WebHostBuilder

				// Display DevServer version banner
				var ideChannelManager = host.Services.GetRequiredService<IIdeChannelManager>();
				DisplayVersionBanner(httpPort, ideChannelManager.ChannelId, ideChannelManager.IsConnected);

				// STEP 3: Use global telemetry for server-wide events
				// Track devserver startup using global telemetry service
				var startupProperties = new Dictionary<string, string>
				{
					["StartupHasSolution"] = (solution != null).ToString(),
				};

				telemetry?.TrackEvent("startup", startupProperties, null);

				_ = ParentProcessObserver.ObserveAsync(parentPID, ct.Cancel, telemetry, ct.Token);
				IdeChannelObserver.Observe(parentPID, ideChannel, ideChannelManager, ct.Cancel, telemetry, ct.Token);

				ambientRegistry = host.Services.GetRequiredService<AmbientRegistry>();
				ambientRegistry.Register(solution, parentPID, httpPort, ideChannelManager.ChannelId);

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
					var errorMeasurements = new Dictionary<string, double> { ["UptimeSeconds"] = uptime.TotalSeconds, };

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
		private static void DisplayVersionBanner(int httpPort, string? ideChannelId, bool ideChannelConnected)
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
					("IDE Channel", FormatIdeChannelStatus(ideChannelId, ideChannelConnected)),
				};

				Helpers.BannerHelper.Write("Uno Platform DevServer", entries);
			}
			catch (Exception ex)
			{
				// Log the error for debugging purposes
				Console.WriteLine($"Warning: Could not extract version information: {ex.Message}");
			}
		}

		internal static string FormatIdeChannelStatus(string? ideChannelId, bool ideChannelConnected)
		{
			if (string.IsNullOrWhiteSpace(ideChannelId))
			{
				return "Not configured";
			}

			var pipePath = $@"\\.\pipe\{ideChannelId}";
			return ideChannelConnected
				? $"Connected: {pipePath}"
				: $"Configured: {pipePath}";
		}
	}
}
