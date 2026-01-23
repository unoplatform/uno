using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Uno.Extensions;
using Uno.UI.RemoteControl.Host;
using Uno.UI.RemoteControl.HotReload.Messages;
using Uno.UI.RemoteControl.Messages;
using Uno.UI.RemoteControl.Messaging;
using Uno.UI.RemoteControl.Messaging.IdeChannel;
using Uno.UI.RemoteControl.Server.AppLaunch;
using Uno.UI.RemoteControl.Server.Telemetry;
using Uno.UI.RemoteControl.Services;
using Uno.UI.RemoteControl.ServerCore;
using Uno.UI.RemoteControl.ServerCore.Configuration;

namespace Uno.UI.RemoteControl.Server;

/// <summary>
/// Connection-scoped devserver runtime that owns the transport, processors, and discovery state for a single IDE/runtime session.
/// A new instance is created per connection scope by <see cref="Uno.UI.RemoteControl.ServerCore.RemoteControlServerHost"/>.
/// </summary>
public sealed class RemoteControlServer : IRemoteControlServer, IRemoteControlServerConnection, IDisposable
{
	private readonly Dictionary<string, IServerProcessor> _processors = new();
	private readonly List<DiscoveredProcessor> _discoveredProcessors = [];
	private readonly List<IRemoteControlProcessorLease> _processorLeases = [];
	private readonly CancellationTokenSource _ct = new();

	private IFrameTransport? _transport;
	private readonly IRemoteControlConfiguration _configuration;
	private readonly IIdeChannel _ideChannel;
	// Connection-scoped provider used to resolve telemetry and instantiate processors (and their dependencies) discovered later on.
	private readonly IServiceProvider _serviceProvider;
	private readonly ITelemetry? _telemetry;
	private readonly IApplicationLaunchMonitor _launchMonitor;
	private readonly IRemoteControlProcessorFactory _processorFactory;

	/// <summary>
	/// Creates a per-connection server instance. The supplied <paramref name="serviceProvider"/> must be the scoped provider created by the host for that connection.
	/// It is reused to resolve optional connection services (telemetry, launch monitor) and to materialize processors via dependency injection.
	/// </summary>
	public RemoteControlServer(
		IRemoteControlConfiguration configuration,
		IIdeChannel ideChannel,
		IApplicationLaunchMonitor launchMonitor,
		IRemoteControlProcessorFactory processorFactory,
		IServiceProvider serviceProvider)
	{
		_configuration = configuration;
		_ideChannel = ideChannel;
		_launchMonitor = launchMonitor;
		_processorFactory = processorFactory;
		_serviceProvider = serviceProvider;

		// Use connection-specific telemetry for this RemoteControlServer instance
		// This telemetry is scoped to the current transport connection (Kestrel request)
		_telemetry = _serviceProvider.GetService<ITelemetry>();

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().LogDebug("Starting RemoteControlServer");
		}

		_ideChannel.MessageFromIde += ProcessIdeMessage;
	}

	string IRemoteControlServer.GetServerConfiguration(string key)
		=> _configuration.GetValue(key) ?? string.Empty;

	private void RegisterProcessor(IServerProcessor hotReloadProcessor)
		=> _processors[hotReloadProcessor.Scope] = hotReloadProcessor;

	private void RegisterDiscoveredProcessors(RemoteControlProcessorDiscoveryResult result)
	{
		foreach (var processor in result.Instances)
		{
			RegisterProcessor(processor);
		}

		if (result.Lease is not null)
		{
			_processorLeases.Add(result.Lease);
		}
	}

	async Task IRemoteControlServerConnection.HandleConnectionAsync(IFrameTransport transport, CancellationToken ct)
	{
		_transport = transport;

		await _ideChannel.WaitForReady(ct);

		while (await transport.ReceiveAsync(ct) is Frame frame)
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
			catch (Exception error) when (IsTransportClosure(error))
			{
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().LogTrace("Transport closed while processing frame: {Message}", error.Message);
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

	private static bool IsTransportClosure(Exception error)
		=> error is OperationCanceledException or TaskCanceledException or ObjectDisposedException or WebSocketException;

	private void ProcessIdeMessage(object? sender, IdeMessage message)
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().LogDebug("Received message from IDE: {MessageType}", message.GetType().Name);
		}
		if (message is AppLaunchRegisterIdeMessage appLaunchRegisterIdeMessage)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug("Received app launch register message from IDE: {Msg}", appLaunchRegisterIdeMessage);
			}
			_launchMonitor.RegisterLaunch(
				appLaunchRegisterIdeMessage.Mvid,
				appLaunchRegisterIdeMessage.Platform,
				appLaunchRegisterIdeMessage.IsDebug,
				appLaunchRegisterIdeMessage.Ide,
				appLaunchRegisterIdeMessage.Plugin);
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
				this.Log().LogDebug("App {Step}: {Msg}", appLaunch.Step, appLaunch);
			}

			switch (appLaunch.Step)
			{
				case AppLaunchStep.Launched:
					if (appLaunch.Ide is null)
					{
						if (this.Log().IsEnabled(LogLevel.Error))
						{
							this.Log().LogError("App Launched: MVID={Mvid} Platform={Platform} Debug={Debug} - No IDE provided.", appLaunch.Mvid, appLaunch.Platform, appLaunch.IsDebug);
						}
						break;
					}

					if (appLaunch.Plugin is null)
					{
						if (this.Log().IsEnabled(LogLevel.Error))
						{
							this.Log().LogError("App Launched: MVID={Mvid} Platform={Platform} Debug={Debug} - No Plugin provided.", appLaunch.Mvid, appLaunch.Platform, appLaunch.IsDebug);
						}
						break;
					}
					_launchMonitor.RegisterLaunch(appLaunch.Mvid, appLaunch.Platform, appLaunch.IsDebug, appLaunch.Ide, appLaunch.Plugin);
					break;

				case AppLaunchStep.Connected:
					var success = _launchMonitor.ReportConnection(appLaunch.Mvid, appLaunch.Platform, appLaunch.IsDebug);
					if (!success && this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().LogDebug("App Connected: MVID={Mvid} Platform={Platform} Debug={Debug} - No immediate match, pending handled by ApplicationLaunchMonitor.", appLaunch.Mvid, appLaunch.Platform, appLaunch.IsDebug);
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
		IImmutableList<string> discoveryAssemblies = ImmutableList<string>.Empty;
		var processorsLoaded = 0;
		var processorsFailed = 0;
		string[] failedProcessors = [];

		try
		{
			var msg = JsonConvert.DeserializeObject<ProcessorsDiscovery>(frame.Content)
				?? throw new InvalidOperationException("Unable to deserialize processor discovery payload.");

			var discoveryProperties = new Dictionary<string, string>
			{
				["AppInstanceId"] = msg.AppInstanceId,
			};

			_telemetry?.TrackEvent("processor-discovery-start", discoveryProperties, null);

			var result = await _processorFactory
				.DiscoverProcessorsAsync(msg, _ct.Token)
				.ConfigureAwait(false)
				?? throw new InvalidOperationException("Processor factory returned null.");

			discoveryAssemblies = result.Assemblies;
			RegisterDiscoveredProcessors(result);
			_discoveredProcessors.AddRange(result.Processors);

			processorsLoaded = result.Processors.Count(p => p.IsLoaded);
			processorsFailed = result.Processors.Count - processorsLoaded;
			failedProcessors = result.Processors
				.Where(p => !p.IsLoaded)
				.Select(p => p.Type)
				.ToArray();

			var completionProperties = new Dictionary<string, string>(discoveryProperties)
			{
				["DiscoveryResult"] = processorsFailed == 0 ? "Success" : "PartialFailure",
				["DiscoveryFailedProcessors"] = string.Join(",", failedProcessors),
			};

			var completionMeasurements = new Dictionary<string, double>
			{
				["DiscoveryDurationMs"] = Stopwatch.GetElapsedTime(startTime).TotalMilliseconds,
				["DiscoveryAssembliesProcessed"] = discoveryAssemblies.Count,
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

			var errorProperties = new Dictionary<string, string>
			{
				["DiscoveryErrorMessage"] = exc.Message,
				["DiscoveryErrorType"] = exc.GetType().Name,
			};

			var errorMeasurements = new Dictionary<string, double>
			{
				["DiscoveryDurationMs"] = Stopwatch.GetElapsedTime(startTime).TotalMilliseconds,
				["DiscoveryAssembliesCount"] = discoveryAssemblies.Count,
				["DiscoveryProcessorsLoadedCount"] = processorsLoaded,
				["DiscoveryProcessorsFailedCount"] = processorsFailed,
			};

			_telemetry?.TrackEvent("processor-discovery-error", errorProperties, errorMeasurements);
		}
		finally
		{
			await SendFrame(new ProcessorsDiscoveryResponse(
				discoveryAssemblies,
				_discoveredProcessors.ToImmutableList()));
		}
	}

	public async Task SendFrame(IMessage message)
	{
		if (_transport is not null)
		{
			await _transport.SendAsync(
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
				this.Log().LogWarning("Tried to send frame, but transport is null.");
			}
		}
	}

	async Task IRemoteControlServer.SendMessageToIDEAsync(IdeMessage message)
		=> await TrySendMessageToIDEAsync(message, CancellationToken.None);

	public Task<bool> TrySendMessageToIDEAsync(IdeMessage message, CancellationToken ct)
		=> _ideChannel.TrySendToIdeAsync(message, ct);

	public void Dispose()
	{
		_ct.Cancel(false);

		// Nothing to flush explicitly: pending is handled internally by ApplicationLaunchMonitor

		foreach (var processor in _processors)
		{
			processor.Value.Dispose();
		}

		foreach (var lease in _processorLeases)
		{
			try
			{
				lease.Dispose();
			}
			catch (Exception exc)
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().LogWarning(exc, "Failed to dispose processor lease.");
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
