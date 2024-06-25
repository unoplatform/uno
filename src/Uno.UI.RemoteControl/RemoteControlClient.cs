#nullable enable

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
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
using static Uno.UI.RemoteControl.RemoteControlStatus;

namespace Uno.UI.RemoteControl;

public partial class RemoteControlClient : IRemoteControlClient
{
	public delegate void RemoteControlFrameReceivedEventHandler(object sender, ReceivedFrameEventArgs args);
	public delegate void RemoteControlClientEventEventHandler(object sender, ClientEventEventArgs args);
	public delegate void SendMessageFailedEventHandler(object sender, SendMessageFailedEventArgs args);

	public static RemoteControlClient? Instance { get; private set; }

	public static RemoteControlClient Initialize(Type appType)
		=> Instance = new RemoteControlClient(appType);

	internal static RemoteControlClient Initialize(Type appType, ServerEndpointAttribute[]? endpoints)
		=> Instance = new RemoteControlClient(appType, endpoints);

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
	private readonly (string endpoint, int port)[]? _serverAddresses;
	private readonly Dictionary<string, IClientProcessor> _processors = new();
	private readonly List<IRemoteControlPreProcessor> _preprocessors = new();
	private readonly object _connectionGate = new();
	private Task<Connection?> _connection; // null if no server, socket only null if connection was established once but lost since then
	private Timer? _keepAliveTimer;
	private KeepAliveMessage _ping = new();

	private record Connection(Uri EndPoint, Stopwatch Since, WebSocket? Socket);

	private RemoteControlClient(Type appType, ServerEndpointAttribute[]? endpoints = null)
	{
		AppType = appType;
		_status = new StatusSink(this);

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
			&& appType.Assembly.GetCustomAttributes(typeof(ServerEndpointAttribute), false) is ServerEndpointAttribute[] embeddedEndpoints)
		{
			IEnumerable<(string endpoint, int port)> GetAddresses()
			{
				foreach (var endpoint in embeddedEndpoints)
				{
					if (endpoint.Port is 0 && !Uri.TryCreate(endpoint.Endpoint, UriKind.Absolute, out _))
					{
						this.Log().LogError($"Failed to get remote control server port from the IDE for endpoint {endpoint.Endpoint}.");
					}
					else
					{
						yield return (endpoint.Endpoint, endpoint.Port);
					}
				}
			}

			_serverAddresses = GetAddresses().ToArray();
		}

		if (_serverAddresses is null or { Length: 0 })
		{
			_serverAddresses = endpoints
				?.Select(ep => (ep.Endpoint, ep.Port))
				.ToArray();
		}

		if (_serverAddresses is null or { Length: 0 })
		{
			this.Log().LogError("Failed to get any remote control server endpoint from the IDE.");

			_connection = Task.FromResult<Connection?>(null);
			_status.Report(ConnectionState.NoServer);
			return;
		}

		// Enable hot-reload
		RegisterProcessor(new HotReload.ClientHotReloadProcessor(this));
		_status.RegisterRequiredServerProcessor("Uno.UI.RemoteControl.Host.HotReload.ServerHotReloadProcessor", VersionHelper.GetVersion(typeof(ClientHotReloadProcessor)));

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
		if (connection is { Socket.State: not WebSocketState.Open } and { Since.ElapsedMilliseconds: >= _connectionRetryInterval })
		{
			// We have a socket (and uri) but we lost the connection, try to reconnect (only if more than 5 sec since last attempt)
			lock (_connectionGate)
			{
				_status.Report(ConnectionState.Reconnecting);

				if (connectionTask == _connection)
				{
					_connection = Connect(connection.EndPoint, CancellationToken.None);
				}
			}

			connection = await _connection;
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
					this.Log().LogWarning($"No server addresses provided, skipping.");
				}

				_status.Report(ConnectionState.NoServer);
				return default;
			}

#if __WASM__
			var isHttps = WebAssemblyRuntime.InvokeJS("window.location.protocol == 'https:'").Equals("true", StringComparison.OrdinalIgnoreCase);
#else
			const bool isHttps = false;
#endif

			_status.Report(ConnectionState.Connecting);

			var pending = _serverAddresses
				.Where(adr => adr.port != 0 || Uri.TryCreate(adr.endpoint, UriKind.Absolute, out _))
				.Select(s =>
				{
					var cts = new CancellationTokenSource();
					var task = Connect(s.endpoint, s.port, isHttps, cts.Token);

					return (task: task, cts: cts);
				})
				.ToDictionary(c => c.task as Task);
			var timeout = Task.Delay(30000);

			// Ensure to await all connection tasks to avoid UnobservedTaskException
			CleanupConnections(pending.Keys);

			// Wait for the first connection to succeed
			Connection? connected = default;
			while (connected is null && pending is { Count: > 0 })
			{
				var completed = await Task.WhenAny(pending.Keys.Concat(timeout));
				if (completed == timeout)
				{
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().LogError("Failed to connect to the server (timeout).");
					}

					_status.Report(ConnectionState.ConnectionTimeout);
					AbortPending();

					return null;
				}

				// Remove the completed task from the pending list, no matter its completion status
				pending.Remove(completed);

				// If the connection is successful, break the loop
				if (completed.IsCompleted
					&& ((Task<Connection>)completed).Result is { Socket: not null } successfulConnection)
				{
					connected = successfulConnection;
					break;
				}
			}

			// Abort all other pending connections
			AbortPending();

			_status.ReportActiveConnection(connected);

			if (connected is null)
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
					this.Log().LogDebug($"Connected to {connected!.EndPoint}");
				}

				DevServerDiagnostics.Current = _status;
				_ = ProcessMessages(connected!.Socket!);

				return connected;
			}

			void AbortPending()
			{
				foreach (var connection in pending.Values)
				{
					connection.cts.Cancel();
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

	private async Task<Connection?> Connect(string endpoint, int port, bool isHttps, CancellationToken ct)
	{
		try
		{
			Uri serverUri;
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
#if __WASM__
				if (endpoint.EndsWith("gitpod.io", StringComparison.Ordinal))
				{
					var originParts = endpoint.Split('-');

					var currentHost = Foundation.WebAssemblyRuntime.InvokeJS("window.location.hostname");
					var targetParts = currentHost.Split('-');

					endpoint = string.Concat(originParts[0].AsSpan(), "-", currentHost.AsSpan().Slice(targetParts[0].Length + 1));
				}
#endif

				serverUri = new Uri($"wss://{endpoint}/rc");
			}
			else
			{
				var scheme = isHttps ? "wss" : "ws";
				serverUri = new Uri($"{scheme}://{endpoint}:{port}/rc");
			}

			return await Connect(serverUri, ct);
		}
		catch (Exception e)
		{
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"Connecting to [{endpoint}:{port}] failed: {e.Message}");
			}

			return null;
		}
	}

	private async Task<Connection?> Connect(Uri serverUri, CancellationToken ct)
	{
		// Note: This method **MUST NOT** throw any exception as it being used for re-connection

		var watch = Stopwatch.StartNew();
		try
		{
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"Connecting to [{serverUri}]");
			}

			var client = new ClientWebSocket();

			await client.ConnectAsync(serverUri, ct);

			return new(serverUri, watch, client);
		}
		catch (Exception e)
		{
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"Connecting to [{serverUri}] failed: {e.Message}");
			}

			return new(serverUri, watch, null);
		}
	}

	/// <summary>
	/// Cleanup connections to avoid tasks raising UnobservedTaskException.
	/// </summary>
	private static void CleanupConnections(IEnumerable<Task> connections)
		=> _ = Task.Run(async () =>
		{
			foreach (var connection in connections)
			{
				try
				{
					await connection;
				}
				catch
				{
					// Exceptions are not used here.
				}
			}
		});

	private async Task ProcessMessages(WebSocket socket)
	{
		_ = InitializeServerProcessors();

		foreach (var processor in _processors)
		{
			await processor.Value.Initialize();
		}

		StartKeepAliveTimer();

		while (await WebSocketHelper.ReadFrame(socket, CancellationToken.None) is HotReload.Messages.Frame frame)
		{
			if (frame.Scope == WellKnownScopes.DevServerChannel)
			{
				if (frame.Name == KeepAliveMessage.Name)
				{
					ProcessPong(frame);
				}
				else if (frame.Name == ProcessorsDiscoveryResponse.Name)
				{
					ProcessServerProcessorsDiscovered(frame);
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

					bool skipProcessing = false;

					foreach (var preProcessor in _preprocessors)
					{
						if (await preProcessor.SkipProcessingFrame(frame))
						{
							skipProcessing = true;
							break;
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
								this.Log().LogError($"Error while processing frame [{frame.Scope}/{frame.Name}]", e);
							}
						}
					}
				}
				else
				{
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().LogError($"Unknown Frame scope {frame.Scope}");
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

	private void ProcessServerProcessorsDiscovered(Frame frame)
	{
		if (frame.TryGetContent(out ProcessorsDiscoveryResponse? response))
		{
			_status.ReportServerProcessors(response);

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Server loaded processors: \r\n{response.Processors.Select(p => $"\t- {p.Type} v {p.Version} (from {p.AssemblyPath})").JoinBy("\r\n")}.");
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
			timer.Change(_keepAliveInterval, _keepAliveInterval);
		}
	}

	private async Task InitializeServerProcessors()
	{
		if (AppType.Assembly.GetCustomAttributes(typeof(ServerProcessorsConfigurationAttribute), false) is ServerProcessorsConfigurationAttribute[] { Length: > 0 } configs)
		{
			var config = configs.First();

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"ServerProcessorsConfigurationAttribute ProcessorsPath={config.ProcessorsPath}");
			}

			await SendMessage(new ProcessorsDiscovery(config.ProcessorsPath));
		}
		else
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().LogError("Unable to find ProjectConfigurationAttribute");
			}
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
}
