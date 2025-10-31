#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Uno.Extensions;
using Uno.Foundation;
using Uno.Foundation.Logging;
using Uno.UI.RemoteControl.Helpers;
using Uno.UI.RemoteControl.HotReload;
using Uno.UI.RemoteControl.HotReload.Messages;
using Uno.UI.RemoteControl.Messages;
using Windows.Networking.Sockets;
using Windows.Storage;
using static Uno.UI.RemoteControl.RemoteControlStatus;

namespace Uno.UI.RemoteControl;

public partial class RemoteControlClient : IRemoteControlClient, IAsyncDisposable
{
	private readonly string? _additionalServerProcessorsDiscoveryPath;
	private readonly bool _autoRegisterAppIdentity;

	public delegate void RemoteControlFrameReceivedEventHandler(object sender, ReceivedFrameEventArgs args);

	public delegate void RemoteControlClientEventEventHandler(object sender, ClientEventEventArgs args);

	public delegate void SendMessageFailedEventHandler(object sender, SendMessageFailedEventArgs args);

	public static RemoteControlClient? Instance
	{
		get => _instance;
		private set
		{
			_instance = value;

			if (value is { })
			{
				while (Interlocked.Exchange(ref _waitingList, null) is { } waitingList)
				{
					foreach (var action in waitingList)
					{
						action(value);
					}
				}
			}
		}
	}

	private static IReadOnlyCollection<Action<RemoteControlClient>>? _waitingList;

	/// <summary>
	/// Add a callback to be called when the Instance is available.
	/// </summary>
	/// <remarks>
	/// Will be called synchronously if the instance is already available, no need to check for it before.
	/// </remarks>
	public static void OnRemoteControlClientAvailable(Action<RemoteControlClient> action)
	{
		if (Instance is { })
		{
			action(Instance);
		}
		else
		{
			// Thread-safe way to add the action to a waiting list for the client to be available
			while (true)
			{
				var waitingList = _waitingList;
				IReadOnlyCollection<Action<RemoteControlClient>> newList = waitingList is null
					? [action]
					: [.. waitingList, action];

				if (Instance is { } i) // Last chance to avoid the waiting list
				{
					action(i);
					break;
				}

				if (ReferenceEquals(Interlocked.CompareExchange(ref _waitingList, newList, waitingList), waitingList))
				{
					break;
				}
			}
		}
	}

	/// <summary>
	/// Initializes the remote control client for the current application.
	/// </summary>
	/// <param name="appType">The type of the application entry point (usually your App type).</param>
	/// <returns>The initialized RemoteControlClient singleton instance.</returns>
	/// <remarks>
	/// This is the primary initialization entry point used by applications. It is invoked by generated code
	/// in debug builds (see Uno XAML source generator) and relies on discovery of the dev-server endpoint via
	/// environment variables or assembly attributes emitted at build time by the IDE integration.
	/// </remarks>
	public static RemoteControlClient Initialize(Type appType)
		=> Instance = new RemoteControlClient(appType);

	/// <summary>
	/// Initializes the remote control client with explicit server endpoints.
	/// </summary>
	/// <param name="appType">The type of the application entry point.</param>
	/// <param name="endpoints">Optional list of fallback endpoints to try when connecting to the dev-server.</param>
	/// <returns>The initialized RemoteControlClient singleton instance.</returns>
	/// <remarks>
	/// This overload is internal and mainly intended for tests and advanced scenarios. It allows providing explicit
	/// endpoints that will be used as a fallback in addition to values coming from environment variables or
	/// assembly-level attributes. Typical application code should call <see cref="Initialize(Type)"/>.
	/// </remarks>
	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static RemoteControlClient Initialize(Type appType, ServerEndpointAttribute[]? endpoints)
		=> Instance = new RemoteControlClient(appType, endpoints);

	/// <summary>
	/// Initializes the remote control client with explicit server endpoints and an additional processors discovery path.
	/// </summary>
	/// <param name="appType">The type of the application entry point.</param>
	/// <param name="endpoints">Optional list of fallback endpoints to try when connecting to the dev-server.</param>
	/// <param name="additionalServerProcessorsDiscoveryPath">An optional absolute or relative path used to discover additional server processors.</param>
	/// <param name="autoRegisterAppIdentity">Whether to automatically register the app identity (mvid - platform...) with the dev-server.</param>
	/// <returns>The initialized RemoteControlClient singleton instance.</returns>
	/// <remarks>
	/// This overload is internal and primarily used by tests to inject additional server processors or assemblies
	/// during discovery, and to control the endpoints to connect to. Application code should use <see cref="Initialize(Type)"/>.
	/// </remarks>
	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static RemoteControlClient Initialize(
		Type appType,
		ServerEndpointAttribute[]? endpoints,
		string? additionalServerProcessorsDiscoveryPath,
		bool autoRegisterAppIdentity = true)
		=> Instance = new RemoteControlClient(appType, endpoints, additionalServerProcessorsDiscoveryPath, autoRegisterAppIdentity);

	public event RemoteControlFrameReceivedEventHandler? FrameReceived;
	public event RemoteControlClientEventEventHandler? ClientEvent;
	public event SendMessageFailedEventHandler? SendMessageFailed;

	/// <summary>
	/// Application type used to initialize this client.
	/// </summary>
	public Type AppType { get; }

	/// <summary>
	/// Gets the minimum interval between re-connection attempts.
	/// </summary>
	/// <remarks>This applies only if a connection has been established once and has been lost by then.</remarks>
	public TimeSpan ConnectionRetryInterval { get; } = TimeSpan.FromMilliseconds(_connectionRetryInterval);

	private const int _connectionRetryInterval = 5_000;

	private readonly StatusSink _status;
	private static readonly TimeSpan _keepAliveInterval = TimeSpan.FromSeconds(30);
	private static RemoteControlClient? _instance;
	private readonly (string endpoint, int port)[]? _serverAddresses;
	private readonly Dictionary<string, IClientProcessor> _processors = new();
	private readonly List<IRemoteControlPreProcessor> _preprocessors = new();
	private readonly Lock _connectionGate = new();
	private Task<Connection?> _connection; // null if no server, socket only null if connection was established once but lost since then
	private Timer? _keepAliveTimer;
	private KeepAliveMessage _ping = new();

	private record Connection(RemoteControlClient Owner, Uri EndPoint, Stopwatch Since, WebSocket? Socket)
		: IAsyncDisposable
	{
		private static class States
		{
			public const int Ready = 0;
			public const int Active = 1;
			public const int Disposed = 255;
		}

		private readonly CancellationTokenSource _ct = new();
		private int _state = Socket is null ? States.Disposed : States.Ready;

		public void EnsureActive()
		{
			if (Interlocked.CompareExchange(ref _state, States.Active, States.Ready) is States.Ready)
			{
				DevServerDiagnostics.Current = Owner._status;
				_ = Owner.ProcessMessages(Socket!, _ct.Token);
			}
		}

		/// <inheritdoc />
		public async ValueTask DisposeAsync()
		{
			_state = States.Disposed;
			await _ct.CancelAsync();
			_ct.Dispose();

			if (Socket is not null)
			{
				try
				{
					await Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnected", CancellationToken.None);
				}
				catch { }

				Socket.Dispose();
			}
		}
	}

	private RemoteControlClient(Type appType,
		ServerEndpointAttribute[]? endpoints = null,
		string? additionalServerProcessorsDiscoveryPath = null,
		bool autoRegisterAppIdentity = true)
	{
		AppType = appType;
		_additionalServerProcessorsDiscoveryPath = additionalServerProcessorsDiscoveryPath;
		_autoRegisterAppIdentity = autoRegisterAppIdentity;

		_status = new StatusSink(this);
		var error = default(ConnectionError?);

		// Environment variables are the first priority as they are used by runtime tests engine to test hot-reload.
		// They should be considered as the default values and in any case they must take precedence over the assembly-provided values.
		if (Environment.GetEnvironmentVariable("UNO_DEV_SERVER_HOST") is { Length: > 0 } host
			&& Environment.GetEnvironmentVariable("UNO_DEV_SERVER_PORT") is { Length: > 0 } portRaw
			&& int.TryParse(portRaw, out var port))
		{
			_serverAddresses = new[] { (host, port) };
		}

		// Get the addresses from the assembly attributes set by the code-gen in debug (i.e. from the IDE)
		if (_serverAddresses is null or { Length: 0 }
			&& appType.Assembly.GetCustomAttributes(typeof(ServerEndpointAttribute), false) is ServerEndpointAttribute[] { Length: > 0 } embeddedEndpoints)
		{
			IEnumerable<(string endpoint, int port)> GetAddresses()
			{
				foreach (var endpoint in embeddedEndpoints)
				{
					if (endpoint.Port is 0 && !Uri.TryCreate(endpoint.Endpoint, UriKind.Absolute, out _))
					{
						this.Log().LogInfo($"Failed to get dev-server port from the IDE for endpoint {endpoint.Endpoint}.");
					}
					else
					{
						yield return (endpoint.Endpoint, endpoint.Port);
					}
				}
			}

			_serverAddresses = GetAddresses().ToArray();
			if (_serverAddresses is { Length: 0 })
			{
				error = ConnectionError.EndpointWithoutPort;
				this.Log().LogError(
					"Some endpoint for uno's dev-server has been configured in your application, but all are invalid (port is missing?). "
					+ "This can usually be fixed with a **rebuild** of your application. "
					+ "If not, make sure you have the latest version of the uno's extensions installed in your IDE and restart your IDE.");
			}
		}

		if (_serverAddresses is null or { Length: 0 })
		{
			_serverAddresses = endpoints
				?.Select(ep => (ep.Endpoint, ep.Port))
				.ToArray();
		}

		// Enable hot-reload
		// Note: We register the HR processor even if we _serverAddresses is empty. This is to make sure to create the HR indicator.
		RegisterProcessor(new HotReload.ClientHotReloadProcessor(this));
		_status.RegisterRequiredServerProcessor("Uno.UI.RemoteControl.Host.HotReload.ServerHotReloadProcessor", VersionHelper.GetVersion(typeof(ClientHotReloadProcessor)));

		if (_serverAddresses is null or { Length: 0 })
		{
			if (error is null)
			{
				error = ConnectionError.NoEndpoint;
				this.Log().LogError(
					"Failed to get any valid dev-server endpoint from the IDE."
					+ "Make sure you have the latest version of the uno's extensions installed in your IDE and restart your IDE.");
			}

			_connection = Task.FromResult<Connection?>(null);
			_status.Report(ConnectionState.NoServer, error);
			return;
		}

		_connection = StartConnection();
	}

	public IEnumerable<object> Processors
		=> _processors.Values;

	internal IClientProcessor[] RegisteredProcessors
		=> _processors.Values.ToArray();

	internal Task WaitForConnection()
		=> WaitForConnection(CancellationToken.None);

	public Task WaitForConnection(CancellationToken ct)
		=> _connection;

	private void RegisterProcessor(IClientProcessor processor)
		=> _processors[processor.Scope] = processor;

	public void RegisterPreProcessor(IRemoteControlPreProcessor preprocessor)
		=> _preprocessors.Add(preprocessor);

	private async ValueTask<WebSocket?> GetActiveSocket()
	{
		var connectionTask = _connection;
		var connection = await connectionTask;
		if (connection is ({ Socket: null } or { Socket.State: not WebSocketState.Open }) and { Since.ElapsedMilliseconds: >= _connectionRetryInterval })
		{
			// We have a socket (and uri) but we lost the connection, try to reconnect (only if more than 5 sec since last attempt)
			lock (_connectionGate)
			{
				_status.Report(ConnectionState.Reconnecting);

				if (connectionTask == _connection)
				{
					_connection = Connect(connection.EndPoint, CancellationToken.None)!;
				}
			}

			connection = await _connection;
			connection?.EnsureActive();
		}

		_status.ReportActiveConnection(connection);

		return connection?.Socket;
	}

	private async Task<Connection?> StartConnection()
	{
		try
		{
			if (_serverAddresses is null or { Length: 0 })
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().LogWarning("No server addresses provided, skipping.");
				}

				_status.Report(ConnectionState.NoServer);
				return default;
			}

			var isHttps = false;
			if (OperatingSystem.IsBrowser())
			{
				isHttps = string.Equals(WebAssemblyImports.EvalString("window.location.protocol"), "https:", StringComparison.OrdinalIgnoreCase);
			}

			_status.Report(ConnectionState.Connecting);

			const string lastEndpointKey = "__UNO__" + nameof(RemoteControlClient) + "__last_endpoint";
			string? preferred;
			try
			{
				preferred =
					ApplicationData.Current.LocalSettings.Values.TryGetValue(lastEndpointKey, out var lastValue) &&
					lastValue is string lastEp
						? _serverAddresses
							.FirstOrDefault(srv => srv.endpoint.Equals(lastEp, StringComparison.OrdinalIgnoreCase))
							.endpoint
						: default;
			}
			catch
			{
				preferred = default;
			}

			var pending = _serverAddresses
				.Select(srv =>
				{
					if (TryParse(srv.endpoint, srv.port, isHttps, out var serverUri))
					{
						// Note: If we have a preferred endpoint (last known to be successful), we delay a bit the connection to other endpoints.
						//		 This is to reduce the number of (task cancelled / socket) exceptions at startup by giving a chance to the preferred endpoint to succeed first.
						var cts = new CancellationTokenSource();
						var delay = preferred is null || preferred.Equals(srv.endpoint, StringComparison.OrdinalIgnoreCase) ? 0 : 3000;
						var task = Connect(serverUri, delay, cts.Token);

						return (task, srv.endpoint, cts);
					}

					return default;
				})
				.Where(c => c.task is not null)
				.ToDictionary(c => c.task as Task);
			var timeout = Task.Delay(30000);

			// Wait for the first connection to succeed
			Connection? connection = default;
			while (connection is null && pending is { Count: > 0 })
			{
				var task = await Task.WhenAny([.. pending.Keys, timeout]);
				if (task == timeout)
				{
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().LogError("Failed to connect to the server (timeout).");
					}

					_status.Report(ConnectionState.ConnectionTimeout);
					AbortPending();

					return null;
				}

				var (_, endpoint, _) = pending[task];

				// Remove the completed task from the pending list, no matter its completion status
				pending.Remove(task);

				// If the connection is successful, break the loop
				if (task is Task<Connection?> { IsCompleted: true, Result: { Socket: not null } successfulConnection })
				{
					try
					{
						ApplicationData.Current.LocalSettings.Values[lastEndpointKey] = endpoint;
					}
					catch
					{
						// best effort here
					}

					connection = successfulConnection;
					break;
				}
			}

			// Abort all other pending connections
			AbortPending();

			_status.ReportActiveConnection(connection);

			if (connection is null)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().LogError("Failed to connect to the server (all endpoints failed).");
				}

				return null;
			}
			else
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogDebug($"Connected to {connection!.EndPoint}");
				}

				// Ensure we're processing incoming messages for the connection
				connection.EnsureActive();

				return connection;
			}

			void AbortPending()
			{
				foreach (var connection in pending.Values)
				{
					connection.cts.Cancel(throwOnFirstException: false);
					if (connection is { task.Status: TaskStatus.RanToCompletion, task.Result.Socket: { } socket })
					{
						_ = socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
					}
				}
			}
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogDebug($"Failed to connect to the server ({ex})", ex);
				}
				else
				{
					this.Log().LogWarning($"The remote control client failed to initialize ({ex.Message}). This generally means that XAML Hot Reload will not be available for this session.");
				}
			}

			return null;
		}
	}

	private bool TryParse(string endpoint, int port, bool isHttps, [NotNullWhen(true)] out Uri? serverUri)
	{
		try
		{
			if (Uri.TryCreate(endpoint, UriKind.Absolute, out var fullUri))
			{
				var wsScheme = fullUri.Scheme switch
				{
					"http" => "ws",
					"https" => "wss",
					_ => throw new InvalidOperationException($"Unsupported remote host scheme ({fullUri})"),
				};

				serverUri = new Uri($"{wsScheme}://{fullUri.Authority}/rc");
			}
			else if (port == 443)
			{
				if (OperatingSystem.IsBrowser())
				{
					if (endpoint.EndsWith("gitpod.io", StringComparison.Ordinal))
					{
						var originParts = endpoint.Split('-');

						var currentHost = WebAssemblyImports.EvalString("window.location.hostname");
						var targetParts = currentHost.Split('-');

						endpoint = string.Concat(originParts[0].AsSpan(), "-", currentHost.AsSpan().Slice(targetParts[0].Length + 1));
					}
				}

				serverUri = new Uri($"wss://{endpoint}/rc");
			}
			else if (port is not 0)
			{
				var scheme = isHttps ? "wss" : "ws";
				serverUri = new Uri($"{scheme}://{endpoint}:{port}/rc");
			}
			else
			{
				serverUri = default;
				return false;
			}

			return true;
		}
		catch (Exception e)
		{
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"Connecting to [{endpoint}:{port}] failed: {e.Message}");
			}

			serverUri = default;
			return false;
		}
	}

	private Task<Connection> Connect(Uri serverUri, CancellationToken ct)
		=> Connect(serverUri, 0, ct);

	private async Task<Connection> Connect(Uri serverUri, int delay, CancellationToken ct)
	{
		// Note: This method **MUST NOT** throw any exception as it being used for re-connection

		var watch = Stopwatch.StartNew();
		try
		{
			if (delay > 0)
			{
				// We don't use the CT to make sure to NOT throw an exception here
				await Task.Delay(delay, CancellationToken.None);
			}

			if (ct.IsCancellationRequested)
			{
				return new(this, serverUri, watch, null);
			}

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"Connecting to [{serverUri}]");
			}

			var client = new ClientWebSocket();

			await client.ConnectAsync(serverUri, ct);

			return new(this, serverUri, watch, client);
		}
		catch (Exception e)
		{
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				var innerMessage = e.InnerException is { } ie ? $" ({ie.Message})" : "";
				this.Log().Trace($"Connecting to [{serverUri}] failed: {e.Message}{innerMessage}");
			}

			return new(this, serverUri, watch, null);
		}
	}

	private async Task ProcessMessages(WebSocket socket, CancellationToken ct)
	{
		_ = InitializeServerProcessors();

		foreach (var processor in _processors)
		{
			try
			{
				await processor.Value.Initialize();
			}
			catch (Exception error)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().LogError($"Failed to initialize processor '{processor}'.", error);
				}
			}
		}

		StartKeepAliveTimer();

		while (await WebSocketHelper.ReadFrame(socket, ct) is { } frame)
		{
			try
			{
				// Central handling for some cross-cutting frames
				if (frame is { Scope: WellKnownScopes.HotReload, Name: HotReloadStatusMessage.Name })
				{
					if (frame.TryGetContent(out HotReloadStatusMessage? hrStatus))
					{
						// Report any server initialization error to the global client status sink.
						// If ServerError is empty/null, this will clear any previously reported fatal server error
						// and allow the UI to transition back to a normal state when the server recovers.
						_status.ReportHotReloadServerError(string.IsNullOrEmpty(hrStatus.ServerError) ? null : hrStatus.ServerError);
					}
				}
				if (frame.Scope == WellKnownScopes.DevServerChannel)
				{
					if (frame.Name == KeepAliveMessage.Name)
					{
						ProcessPong(frame);
					}
					else if (frame.Name == ProcessorsDiscoveryResponse.Name)
					{
						await ProcessServerProcessorsDiscovered(frame);
					}
				}
				else
				{
					if (_processors.TryGetValue(frame.Scope, out var processor))
					{
						if (this.Log().IsEnabled(LogLevel.Trace))
						{
							this.Log().Trace($"Received frame [{frame.Scope}/{frame.Name}]");
						}

						var skipProcessing = false;
						foreach (var preProcessor in _preprocessors)
						{
							try
							{
								if (await preProcessor.SkipProcessingFrame(frame))
								{
									skipProcessing = true;
									break;
								}
							}
							catch (Exception error)
							{
								if (this.Log().IsEnabled(LogLevel.Error))
								{
									this.Log().LogError($"Error while **PRE**processing frame [{frame.Scope}/{frame.Name}] be pre-processor {preProcessor}", error);
								}
							}
						}

						if (!skipProcessing)
						{
							try
							{
								await processor.ProcessFrame(frame);
							}
							catch (Exception e)
							{
								if (this.Log().IsEnabled(LogLevel.Error))
								{
									this.Log().LogError($"Error while processing frame [{frame.Scope}/{frame.Name}] by processor {processor}", e);
								}
							}
						}
					}
					else
					{
						if (this.Log().IsEnabled(LogLevel.Trace))
						{
							this.Log().Trace($"Unknown Frame scope {frame.Scope}");
						}
					}
				}

				try
				{
					FrameReceived?.Invoke(this, new ReceivedFrameEventArgs(frame));
				}
				catch (Exception error)
				{
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().LogError($"Error while notifying frame received {frame.Scope}/{frame.Name}", error);
					}
				}
			}
			catch (Exception error)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().LogError($"Error while processing frame {frame.Scope}/{frame.Name}", error);
				}
			}
		}
	}

	private bool _appIdentitySent;

	public async Task SendAppIdentityAsync()
	{
		if (_appIdentitySent)
		{
			return;
		}

		try
		{
			var asm = AppType.Assembly;
			var mvid = ApplicationInfoHelper.GetMvid(asm);
			var platform = ApplicationInfoHelper.GetTargetPlatform(asm);
			var isDebug = Debugger.IsAttached;

			await SendMessage(new AppLaunchMessage { Mvid = mvid, Platform = platform, IsDebug = isDebug, Step = AppLaunchStep.Connected });

			_appIdentitySent = true;

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"AppIdentity sent to server (MVID={mvid}, Platform={platform}, Debug={isDebug}).");
			}
		}
		catch (Exception e)
		{
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"Failed to send AppIdentityMessage: {e.Message}");
			}
		}
	}

	private void ProcessPong(Frame frame)
	{
		if (frame.TryGetContent(out KeepAliveMessage? pong))
		{
			_status.ReportPong(pong);

			if (pong.AssemblyVersion != _ping.AssemblyVersion && this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Trace(
					$"Server pong frame (a.k.a. KeepAlive), but version differs from client (server: {pong.AssemblyVersion} | client: {_ping.AssemblyVersion})."
					+ $"This usually indicates that an old instance of the dev-server is being re-used or a partial deployment of the application."
					+ "Some feature like hot-reload are most likely to fail. To fix this, you might have to restart Visual Studio.");
			}
			else if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"Server pong frame (a.k.a. KeepAlive) with valid version ({pong.AssemblyVersion}).");
			}
		}
		else
		{
			_status.ReportPong(null);

			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Trace(
					"Server pong frame (a.k.a. KeepAlive), but failed to deserialize it's content. "
					+ $"This usually indicates a version mismatch between client and server (client: {_ping.AssemblyVersion})."
					+ "Some feature like hot-reload are most likely to fail. To fix this, you might have to restart Visual Studio.");
			}
		}
	}

	private async Task ProcessServerProcessorsDiscovered(Frame frame)
	{
		if (frame.TryGetContent(out ProcessorsDiscoveryResponse? response))
		{
			_status.ReportServerProcessors(response);

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Server loaded processors: \r\n{response.Processors.Select(p => $"\t- {p.Type} v {p.Version} (from {p.AssemblyPath})").JoinBy("\r\n")}.");
			}

			if (_autoRegisterAppIdentity)
			{
				await SendAppIdentityAsync();
			}
		}
	}

	private void StartKeepAliveTimer()
	{
		if (_keepAliveTimer is not null)
		{
			return;
		}

		Timer? timer = default;
		timer = new Timer(async _ =>
		{
			try
			{
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"Sending Keepalive frame from client");
				}

				_ping = _ping.Next();
				_status.ReportPing(_ping);
				await SendMessage(_ping);
			}
			catch (Exception error)
			{
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace("Keepalive failed");
				}

				_status.ReportKeepAliveAborted(error);
				Interlocked.CompareExchange(ref _keepAliveTimer, null, timer);
				timer?.Dispose();
			}
		});

		if (Interlocked.CompareExchange(ref _keepAliveTimer, timer, null) is null)
		{
			timer.Change(TimeSpan.Zero, _keepAliveInterval);
		}
	}

	private async Task InitializeServerProcessors()
	{
		var anyDiscoveryRequested = false;
		if (_additionalServerProcessorsDiscoveryPath is not null)
		{
			anyDiscoveryRequested = true;
			await SendMessage(new ProcessorsDiscovery(_additionalServerProcessorsDiscoveryPath));
		}

		if (AppType.Assembly.GetCustomAttributes(typeof(ServerProcessorsConfigurationAttribute), false) is ServerProcessorsConfigurationAttribute[] { Length: > 0 } configs)
		{
			var config = configs.First();

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"{nameof(ServerProcessorsConfigurationAttribute)} ProcessorsPath={config.ProcessorsPath}");
			}

			anyDiscoveryRequested = true;
			await SendMessage(new ProcessorsDiscovery(config.ProcessorsPath));
		}
		else
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"Unable to find any [{nameof(ServerProcessorsConfigurationAttribute)}]");
			}
		}

		// If there is nothing to discover, send the AppIdentity message now.
		if (!anyDiscoveryRequested && _autoRegisterAppIdentity)
		{
			await SendAppIdentityAsync();
		}
	}

	public async Task SendMessage(IMessage message)
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Sending message: {message} {message.Name}");
		}


		var socket = await GetActiveSocket();
		if (socket is null)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().LogError("Unable send message, no connection available");
			}

			SendMessageFailed?.Invoke(this, new SendMessageFailedEventArgs(message));

			return;
		}

		await WebSocketHelper.SendFrame(
			socket,
			HotReload.Messages.Frame.Create(
				1,
				message.Scope,
				message.Name,
				message
			),
			CancellationToken.None);
	}

	internal void NotifyOfEvent(string eventName, string eventDetails)
	{
		ClientEvent?.Invoke(this, new ClientEventEventArgs(eventName, eventDetails));
	}

	public async ValueTask DisposeAsync()
	{
		var connectionTask = _connection;
		_connection = Task.FromResult<Connection?>(null); // Prevent any re-connection

		if (await connectionTask is { } connection)
		{
			await connection.DisposeAsync();
		}

		foreach (var processor in _processors.Values)
		{
			try
			{
				if (processor is IDisposable disposable)
				{
					disposable.Dispose();
				}
				else if (processor is IAsyncDisposable asyncDisposable)
				{
					await asyncDisposable.DisposeAsync();
				}
			}
			catch (Exception error)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().LogError($"Failed to dispose processor '{processor}'.", error);
				}
			}
		}

		_processors.Clear();

		// Stop the keep alive timer
		Interlocked.Exchange(ref _keepAliveTimer, null)?.Dispose();

		// Remove the instance if it's the current one (should not happen in regular usage)
		if (ReferenceEquals(Instance, this))
		{
			Instance = null;
		}
	}
}
