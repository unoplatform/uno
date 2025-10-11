using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Uno.Extensions;
using Uno.UI.RemoteControl.Helpers;
using Uno.UI.RemoteControl.Host.IdeChannel;
using Uno.UI.RemoteControl.HotReload.Messages;
using Uno.UI.RemoteControl.Messages;
using Uno.UI.RemoteControl.Messaging.IdeChannel;
using Uno.UI.RemoteControl.Server.AppLaunch;
using Uno.UI.RemoteControl.Server.Helpers;
using Uno.UI.RemoteControl.Server.Telemetry;
using Uno.UI.RemoteControl.Services;

namespace Uno.UI.RemoteControl.Host;

internal class RemoteControlServer : IRemoteControlServer, IDisposable
{
	private readonly object _loadContextGate = new();
	private static readonly Dictionary<string, (AssemblyLoadContext Context, int Count)> _loadContexts = new();
	private static readonly Dictionary<string, string> _resolveAssemblyLocations = new();
	private readonly Dictionary<string, IServerProcessor> _processors = new();
	private readonly List<DiscoveredProcessor> _discoveredProcessors = new();
	private readonly CancellationTokenSource _ct = new();

	private WebSocket? _socket;
	private readonly List<string> _appInstanceIds = new();
	private readonly IConfiguration _configuration;
	private readonly IIdeChannel _ideChannel;
	private readonly IServiceProvider _serviceProvider;
	private readonly IServiceProvider _globalServiceProvider;
	private readonly ITelemetry? _telemetry;

	public RemoteControlServer(IConfiguration configuration, IIdeChannel ideChannel, IServiceProvider serviceProvider)
	{
		_configuration = configuration;
		_ideChannel = ideChannel;
		_serviceProvider = serviceProvider;

		// Get the global service provider from the connection services
		// This allows access to both global and connection-specific services
		_globalServiceProvider = _serviceProvider.GetKeyedService<IServiceProvider>("global") ?? _serviceProvider;

		// Use connection-specific telemetry for this RemoteControlServer instance
		// This telemetry is scoped to the current WebSocket connection (Kestrel request)
		_telemetry = _serviceProvider.GetService<ITelemetry>();

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().LogDebug("Starting RemoteControlServer");
		}

		_ideChannel.MessageFromIde += ProcessIdeMessage;
	}

	string IRemoteControlServer.GetServerConfiguration(string key)
		=> _configuration[key] ?? "";

	private AssemblyLoadContext GetAssemblyLoadContext(string applicationId)
	{
		lock (_loadContextGate)
		{
			if (_loadContexts.TryGetValue(applicationId, out var lc))
			{
				_loadContexts[applicationId] = (lc.Context, lc.Count + 1);

				return lc.Context;
			}

			var loadContext = new AssemblyLoadContext(applicationId, isCollectible: true);
			loadContext.Unloading += (e) =>
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogDebug("Unloading assembly context {name}", e.Name);
				}
			};

			// Add custom resolving so we can find dependencies even when the processor assembly
			// is built for a different .net version than the host process.
			loadContext.Resolving += (context, assemblyName) =>
			{
				if (_resolveAssemblyLocations.TryGetValue(applicationId, out var _resolveAssemblyLocation) &&
					!string.IsNullOrWhiteSpace(_resolveAssemblyLocation))
				{
					try
					{
						var dir = Path.GetDirectoryName(_resolveAssemblyLocation);
						if (!string.IsNullOrEmpty(dir))
						{
							var relPath = Path.Combine(dir, assemblyName.Name + ".dll");
							if (File.Exists(relPath))
							{
								if (this.Log().IsEnabled(LogLevel.Trace))
								{
									this.Log().LogTrace("Loading assembly from resolved path: {relPath}", relPath);
								}

								return TryLoadAssemblyFromPath(context, relPath);
							}
						}
					}
					catch (Exception exc)
					{
						if (this.Log().IsEnabled(LogLevel.Error))
						{
							this.Log().LogError(exc, "Failed for load dependency: {assemblyName}", assemblyName);
						}
					}
				}
				else
				{
					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().LogDebug("Failed for identify location of dependency: {assemblyName}", assemblyName);
					}
				}

				// We haven't found the assembly in our context, let the runtime
				// find it using standard resolution mechanisms.
				return null;
			};

			if (!_loadContexts.TryAdd(applicationId, (loadContext, 1)))
			{
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().LogTrace("Failed to add a LoadContext for : {appId}", applicationId);
				}
			}

			return loadContext;
		}
	}

	private static Assembly TryLoadAssemblyFromPath(AssemblyLoadContext context, string asmPath)
	{
		// Load the assembly using the full path to avoid duplicates related
		// relative paths pointing to the same file.
		asmPath = Path.GetFullPath(asmPath);

		// Try loading the assembly multiple times, using a try catch and a loop with a sleep
		// to avoid issues with the assembly being locked by another process.
		int tries = 10;
		do
		{
			try
			{
				return context.LoadFromAssemblyPath(asmPath);
			}
			catch (Exception exc)
			{
				if (context.Log().IsEnabled(LogLevel.Trace))
				{
					context.Log().LogTrace("Failed to load assembly {asmPath} : {exc}", asmPath, exc);
				}
			}

			Thread.Sleep(100);
		}
		while (tries-- > 0);

		// Try without exception handling to report the original exception
		return context.LoadFromAssemblyPath(asmPath);
	}

	private void RegisterProcessor(IServerProcessor hotReloadProcessor)
		=> _processors[hotReloadProcessor.Scope] = hotReloadProcessor;

	public async Task RunAsync(WebSocket socket, CancellationToken ct)
	{
		_socket = socket;

		if (_ideChannel is IdeChannelServer srv)
		{
			await srv.WaitForReady(ct);
		}

		while (await WebSocketHelper.ReadFrame(socket, ct) is Frame frame)
		{
			try
			{
				if (frame.Scope == WellKnownScopes.DevServerChannel)
				{
					switch (frame.Name)
					{
						case ProcessorsDiscovery.Name:
							await ProcessDiscoveryFrame(frame);
							continue;
						case KeepAliveMessage.Name:
							await ProcessPingFrame(frame);
							continue;
						case AppLaunchMessage.Name:
							await ProcessAppLaunchFrame(frame);
							continue;
					}
				}

				if (_processors.TryGetValue(frame.Scope, out var processor))
				{
					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().LogDebug("Received Frame [{Scope} / {Name}] to be processed by {processor}", frame.Scope, frame.Name, processor);
					}

					try
					{
						DevServerDiagnostics.Current = DiagnosticsSink.Instance;
						await processor.ProcessFrame(frame);
					}
					catch (Exception e)
					{
						if (this.Log().IsEnabled(LogLevel.Error))
						{
							this.Log().LogError(e, "Failed to process frame [{Scope} / {Name}]", frame.Scope, frame.Name);
						}
					}
				}
				else
				{
					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						// Log no processors found to process this frame (give a registered processors list)
						var processorsList = string.Join(", ", _processors.Keys);
						this.Log().LogDebug("Unknown Frame [{Scope} / {Name}] - No processors registered. Registered processors: {processorsList}", frame.Scope, frame.Name, processorsList);
					}
				}
			}
			catch (Exception error)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().LogError(error, "Failed to process frame [{Scope} / {Name}]", frame.Scope, frame.Name);
				}
			}
		}
	}

	private void ProcessIdeMessage(object? sender, IdeMessage message)
	{
		Console.WriteLine($"Received message from IDE: {message.GetType().Name}");
		if (message is AppLaunchRegisterIdeMessage appLaunchRegisterIdeMessage)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug("Received app launch register message from IDE, mvid={Mvid}, platform={Platform}, isDebug={IsDebug}", appLaunchRegisterIdeMessage.Mvid, appLaunchRegisterIdeMessage.Platform, appLaunchRegisterIdeMessage.IsDebug);
			}
			var monitor = _serviceProvider.GetRequiredService<ApplicationLaunchMonitor>();

			monitor.RegisterLaunch(appLaunchRegisterIdeMessage.Mvid, appLaunchRegisterIdeMessage.Platform, appLaunchRegisterIdeMessage.IsDebug);
		}
		else if (_processors.TryGetValue(message.Scope, out var processor))
		{
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().LogTrace("Received message [{Scope} / {Name}] to be processed by {processor}", message.Scope, message.GetType().Name, processor);
			}

			var process = processor.ProcessIdeMessage(message, _ct.Token);

			if (this.Log().IsEnabled(LogLevel.Error))
			{
				process = process.ContinueWith(
					t => this.Log().LogError($"Failed to process message {message}: {t.Exception?.Flatten()}"),
					_ct.Token,
					TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.AttachedToParent,
					TaskScheduler.Default);
			}
		}
		else
		{
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().LogTrace("Unknown Frame [{Scope} / {Name}]", message.Scope, message.GetType().Name);
			}
		}
	}

	private async Task ProcessAppLaunchFrame(Frame frame)
	{
		if (frame.TryGetContent(out AppLaunchMessage? appLaunch))
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug("App {Step}: MVID={Mvid} Platform={Platform} Debug={Debug}", appLaunch.Step, appLaunch.Mvid, appLaunch.Platform, appLaunch.IsDebug);
			}

			switch (appLaunch.Step)
			{
				case AppLaunchStep.Launched:
					if (_serviceProvider.GetService<ApplicationLaunchMonitor>() is { } monitor)
					{
						monitor.RegisterLaunch(appLaunch.Mvid, appLaunch.Platform, appLaunch.IsDebug);
					}
					break;
				case AppLaunchStep.Connected:
					if (_serviceProvider.GetService<ApplicationLaunchMonitor>() is { } monitor2)
					{
						var success = monitor2.ReportConnection(appLaunch.Mvid, appLaunch.Platform, appLaunch.IsDebug);
						if (!success)
						{
							if (this.Log().IsEnabled(LogLevel.Error))
							{
								this.Log().LogError("App Connected: MVID={Mvid} Platform={Platform} Debug={Debug} - Failed to report connected: APP LAUNCH NOT REGISTERED.", appLaunch.Mvid, appLaunch.Platform, appLaunch.IsDebug);
							}
						}
					}
					break;
			}
		}
		else
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"Got an invalid app launch frame ({frame.Content})");
			}
		}

		await Task.CompletedTask;
	}

	private async Task ProcessPingFrame(Frame frame)
	{
		KeepAliveMessage pong;
		if (frame.TryGetContent(out KeepAliveMessage? ping))
		{
			pong = new() { SequenceId = ping.SequenceId };

			if (ping.AssemblyVersion != pong.AssemblyVersion && this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning(
					$"Client ping frame (a.k.a. KeepAlive), but version differs from server (server: {pong.AssemblyVersion} | client: {ping.AssemblyVersion})."
					+ $"This usually indicates that an old instance of the dev-server is being re-used or a partial deployment of the application."
					+ "Some feature like hot-reload are most likely to fail. To fix this, you might have to restart Visual Studio.");
			}
			else if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().LogTrace($"Client ping frame (a.k.a. KeepAlive) with valid version ({ping.AssemblyVersion}).");
			}
		}
		else
		{
			pong = new();

			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning(
					"Client ping frame (a.k.a. KeepAlive), but failed to deserialize it's content. "
					+ $"This usually indicates a version mismatch between client and server (server: {pong.AssemblyVersion})."
					+ "Some feature like hot-reload are most likely to fail. To fix this, you might have to restart Visual Studio.");
			}
		}

		await SendFrame(pong);
	}

	private async Task ProcessDiscoveryFrame(Frame frame)
	{
		var startTime = Stopwatch.GetTimestamp();
		var assemblies = new List<(string path, System.Reflection.Assembly assembly)>();
		var processorsLoaded = 0;
		var processorsFailed = 0;

		try
		{
			var msg = JsonConvert.DeserializeObject<ProcessorsDiscovery>(frame.Content)!;
			var serverAssemblyName = typeof(IServerProcessor).Assembly.GetName().Name;

			// Track processor discovery start
			var discoveryProperties = new Dictionary<string, string>
			{
				["AppInstanceId"] = msg.AppInstanceId,
				["DiscoveryIsFile"] = File.Exists(msg.BasePath).ToString()
			};

			_telemetry?.TrackEvent("processor-discovery-start", discoveryProperties, null);

			if (!_appInstanceIds.Contains(msg.AppInstanceId))
			{
				_appInstanceIds.Add(msg.AppInstanceId);
			}

			var assemblyLoadContext = GetAssemblyLoadContext(msg.AppInstanceId);

			// If BasePath is a specific file, try and load that
			if (File.Exists(msg.BasePath))
			{
				try
				{
					_resolveAssemblyLocations[msg.AppInstanceId] = msg.BasePath;

					assemblies.Add((msg.BasePath, TryLoadAssemblyFromPath(assemblyLoadContext, msg.BasePath)));
				}
				catch (Exception exc)
				{
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().LogError("Failed to load assembly {BasePath} : {Exc}", msg.BasePath, exc);
					}
				}
			}
			else
			{
				// As BasePath is a directory, try and load processors from assemblies within that dir
				var basePath = msg.BasePath.Replace('/', Path.DirectorySeparatorChar);

#if NET10_0_OR_GREATER
				basePath = Path.Combine(basePath, "net10.0");
#elif NET9_0_OR_GREATER
				basePath = Path.Combine(basePath, "net9.0");
#endif

				// Additional processors may not need the directory added immediately above.
				if (!Directory.Exists(basePath))
				{
					basePath = msg.BasePath;
				}

				// Local default Uno Processors (matched by assembly name)
				foreach (var file in Directory.GetFiles(basePath, "Uno.*.Processor*.dll"))
				{
					if (Path.GetFileNameWithoutExtension(file).Equals(serverAssemblyName, StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}

					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().LogDebug("Discovery: Loading {File}", file);
					}

					try
					{
						assemblies.Add((file, assemblyLoadContext.LoadFromAssemblyPath(file)));
					}
					catch (Exception exc)
					{
						// With additional processors there may be duplicates of assemblies already loaded
						if (this.Log().IsEnabled(LogLevel.Debug))
						{
							this.Log().LogDebug("Failed to load assembly {File} : {Exc}", file, exc);
						}
					}
				}
			}

			var failedProcessors = new List<string>();

			foreach (var asm in assemblies)
			{
				try
				{
					if (assemblies.Count > 1 ||
						!_resolveAssemblyLocations.TryGetValue(msg.AppInstanceId, out var _resolveAssemblyLocation) ||
						string.IsNullOrEmpty(_resolveAssemblyLocation))
					{
						_resolveAssemblyLocations[msg.AppInstanceId] = asm.path;
					}

					var attributes = asm.assembly.GetCustomAttributes(typeof(ServerProcessorAttribute), false);

					foreach (var processorAttribute in attributes)
					{
						if (processorAttribute is ServerProcessorAttribute processor)
						{
							if (this.Log().IsEnabled(LogLevel.Debug))
							{
								this.Log().LogDebug("Discovery: Registering {ProcessorType}", processor.ProcessorType);
							}

							try
							{
								// Log detailed processor instantiation attempt
								if (this.Log().IsEnabled(LogLevel.Information))
								{
									this.Log().LogInformation(
										"Attempting to instantiate processor {ProcessorType} from assembly {AssemblyPath}",
										processor.ProcessorType.FullName,
										asm.path);
									this.Log().LogInformation(
										"Processor assembly location: {AssemblyLocation}",
										processor.ProcessorType.Assembly.Location);
								}

								// Use the connection-scoped service provider directly
								// It should have all necessary dependencies registered via AddConnectionTelemetry()
								if (ActivatorUtilities.CreateInstance(_serviceProvider, processor.ProcessorType, parameters: [this]) is IServerProcessor serverProcessor)
								{
									_discoveredProcessors.Add(new(asm.path, processor.ProcessorType.FullName!, VersionHelper.GetVersion(processor.ProcessorType), IsLoaded: true));
									RegisterProcessor(serverProcessor);
									processorsLoaded++;
									if (this.Log().IsEnabled(LogLevel.Debug))
									{
										this.Log().LogDebug("Successfully registered server processor {ProcessorType}", processor.ProcessorType);
									}
								}
								else
								{
									_discoveredProcessors.Add(new(asm.path, processor.ProcessorType.FullName!, VersionHelper.GetVersion(processor.ProcessorType), IsLoaded: false));
									processorsFailed++;
									failedProcessors.Add(processor.ProcessorType.Name);
									if (this.Log().IsEnabled(LogLevel.Warning))
									{
										this.Log().LogWarning("Failed to create server processor {ProcessorType} - ActivatorUtilities returned null", processor.ProcessorType);
									}
								}
							}
							catch (Exception error)
							{
								_discoveredProcessors.Add(new(asm.path, processor.ProcessorType.FullName!, VersionHelper.GetVersion(processor.ProcessorType), IsLoaded: false, LoadError: error.ToString()));
								processorsFailed++;
								if (this.Log().IsEnabled(LogLevel.Error))
								{
									this.Log().LogError(error, "Failed to create server processor {ProcessorType} from assembly {AssemblyPath}", processor.ProcessorType.FullName, asm.path);
									this.Log().LogError("Processor assembly location: {AssemblyLocation}", processor.ProcessorType.Assembly.Location);
								}
							}
						}
					}
				}
				catch (Exception exc)
				{
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().LogError("Failed to create instance of server processor in  {Asm} : {Exc}", asm, exc);
					}
				}
			}

			// Being thorough about trying to ensure everything is unloaded
			assemblies.Clear();

			// Track processor discovery completion
			var completionProperties = new Dictionary<string, string>(discoveryProperties)
			{
				["DiscoveryResult"] = processorsFailed == 0 ? "Success" : "PartialFailure",
				["DiscoveryFailedProcessors"] = string.Join(",", failedProcessors),
			};

			var completionMeasurements = new Dictionary<string, double>
			{
				["DiscoveryDurationMs"] = Stopwatch.GetElapsedTime(startTime).TotalMilliseconds,
				["DiscoveryAssembliesProcessed"] = assemblies.Count,
				["DiscoveryProcessorsLoadedCount"] = processorsLoaded,
				["DiscoveryProcessorsFailedCount"] = processorsFailed,
			};

			_telemetry?.TrackEvent("processor-discovery-complete", completionProperties, completionMeasurements);
		}
		catch (Exception exc)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().LogError("Failed to process discovery frame: {Exc}", exc);
			}

			// Track processor discovery error
			var errorProperties = new Dictionary<string, string>
			{
				["DiscoveryErrorMessage"] = exc.Message,
				["DiscoveryErrorType"] = exc.GetType().Name,
			};

			var errorMeasurements = new Dictionary<string, double>
			{
				["DiscoveryDurationMs"] = Stopwatch.GetElapsedTime(startTime).TotalMilliseconds,
				["DiscoveryAssembliesCount"] = assemblies.Count,
				["DiscoveryProcessorsLoadedCount"] = processorsLoaded,
				["DiscoveryProcessorsFailedCount"] = processorsFailed,
			};

			_telemetry?.TrackEvent("processor-discovery-error", errorProperties, errorMeasurements);
		}
		finally
		{
			await SendFrame(new ProcessorsDiscoveryResponse(
				assemblies.Select(asm => asm.path).ToImmutableList(),
				_discoveredProcessors.ToImmutableList()));
		}
	}

	public async Task SendFrame(IMessage message)
	{
		if (_socket is not null)
		{
			await WebSocketHelper.SendFrame(
				_socket,
				Frame.Create(
					1,
					message.Scope,
					message.Name,
					message
					),
				CancellationToken.None);
		}
		else
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning("Tried to send frame, but WebSocket is null.");
			}
		}
	}

	public Task SendMessageToIDEAsync(IdeMessage message)
		=> _ideChannel.SendToIdeAsync(message, default);

	public void Dispose()
	{
		_ct.Cancel(false);

		foreach (var processor in _processors)
		{
			processor.Value.Dispose();
		}

		// Unload any AssemblyLoadContexts not being used by any current connection
		foreach (var appId in _appInstanceIds)
		{
			lock (_loadContextGate)
			{
				if (_loadContexts.TryGetValue(appId, out var lc))
				{
					if (lc.Count > 1)
					{
						_loadContexts[appId] = (lc.Context, lc.Count - 1);
					}
					else
					{
						try
						{
							_loadContexts[appId].Context.Unload();

							_loadContexts.Remove(appId);
						}
						catch (Exception exc)
						{
							if (this.Log().IsEnabled(LogLevel.Error))
							{
								this.Log().LogError("Failed to unload AssemblyLoadContext for '{appId}' : {Exc}", appId, exc);
							}
						}
					}
				}
			}
		}
	}

	private class DiagnosticsSink : DevServerDiagnostics.ISink
	{
		public static DiagnosticsSink Instance { get; } = new();

		private DiagnosticsSink() { }

		/// <inheritdoc />
		public void ReportInvalidFrame<TContent>(Frame frame)
			=> typeof(RemoteControlServer).Log().LogError($"Got an invalid frame for type {typeof(TContent).Name} [{frame.Scope} / {frame.Name}]");
	}

}
