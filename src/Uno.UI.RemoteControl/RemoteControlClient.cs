#nullable enable

using System;
using System.Collections.Generic;
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

namespace Uno.UI.RemoteControl
{
	public class RemoteControlClient : IRemoteControlClient
	{
		public static RemoteControlClient? Instance { get; private set; }

		public Type AppType { get; }

		private readonly (string endpoint, int port)[]? _serverAddresses;
		private WebSocket? _webSocket;
		private Dictionary<string, IRemoteControlProcessor> _processors = new Dictionary<string, IRemoteControlProcessor>();
		private Timer? _keepAliveTimer;

		private RemoteControlClient(Type appType)
		{
			AppType = appType;

			if(appType.Assembly.GetCustomAttributes(typeof(ServerEndpointAttribute), false) is ServerEndpointAttribute[] endpoints)
			{
				IEnumerable<(string endpoint, int port)> GetAddresses()
				{
					foreach (var endpoint in endpoints)
					{
						if (endpoint.Port == 0 && !Uri.TryCreate(endpoint.Endpoint, UriKind.Absolute, out _))
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

			if ((_serverAddresses?.Length ?? 0) == 0)
			{
				this.Log().LogError("Failed to get any remote control server endpoint from the IDE.");

				return;
			}

			RegisterProcessor(new HotReload.ClientHotReloadProcessor(this));
			_ = StartConnection();
		}

		private void RegisterProcessor(IRemoteControlProcessor processor)
		{
			_processors[processor.Scope] = processor;
		}

		private async Task StartConnection()
		{
			try
			{
#if __WASM__
				var isHttps = WebAssemblyRuntime.InvokeJS("window.location.protocol == 'https:'").Equals("true", StringComparison.OrdinalIgnoreCase);
#else
				const bool isHttps = false;
#endif
				async Task<(Uri endPoint, WebSocket socket)> Connect(string endpoint, int port, CancellationToken ct)
				{
					var s = new ClientWebSocket();

					Uri BuildServerUri()
					{
						if (Uri.TryCreate(endpoint, UriKind.Absolute, out var fullUri))
						{
							var wsScheme = fullUri.Scheme switch
							{
								"http" => "ws",
								"https" => "wss",
								_ => throw new InvalidOperationException($"Unsupported remote host scheme ({fullUri})"),
							};

							return new Uri($"{wsScheme}://{fullUri.Authority}/rc");
						}
						else if (port == 443)
						{
#if __WASM__
							if (endpoint.EndsWith("gitpod.io", StringComparison.Ordinal))
							{
								var originParts = endpoint.Split('-');

								var currentHost = Foundation.WebAssemblyRuntime.InvokeJS("window.location.hostname");
								var targetParts = currentHost.Split('-');

								endpoint = originParts[0] + '-' + currentHost.Substring(targetParts[0].Length + 1);
							}
#endif

							return new Uri($"wss://{endpoint}/rc");
						}
						else
						{
							var scheme = isHttps ? "wss" : "ws";
							return new Uri($"{scheme}://{endpoint}:{port}/rc");
						}
					}

					var serverUri = BuildServerUri();

					if (this.Log().IsEnabled(LogLevel.Trace))
					{
						this.Log().Trace($"Connecting to [{serverUri}]");
					}

					try
					{
						await s.ConnectAsync(serverUri, ct);
					}
					catch(Exception e)
					{
						if (this.Log().IsEnabled(LogLevel.Trace))
						{
							this.Log().Trace($"Connecting to [{serverUri}] failed: {e.Message}");
						}

						throw;
					}

					return (serverUri, s);
				}

				if (_serverAddresses != null)
				{
					var connections = _serverAddresses
						.Where(adr => adr.port != 0 || Uri.TryCreate(adr.endpoint, UriKind.Absolute, out _))
						.Select(s =>
						{
							var cts = new CancellationTokenSource();
							var task = Connect(s.endpoint, s.port, cts.Token);

							return (task, cts);
						})
						.ToArray();

					var timeout = Task.Delay(30000);
					var completed = await Task.WhenAny(connections.Select(c => c.task).Concat(timeout));

					// Wait for any non-faulted task
					while (completed.IsFaulted)
					{
						var tasks = connections.Select(c => c.task).Where(t => t.Status != TaskStatus.Faulted).ToArray();

						if (tasks.Length > 0)
						{
							completed = await Task.WhenAny(tasks.Concat(timeout));
						}
						else
						{
							break;
						}
					}

					foreach (var connection in connections)
					{
						if (connection.task == completed)
						{
							continue;
						}

						connection.cts.Cancel();
						if (connection.task.Status == TaskStatus.RanToCompletion)
						{
							_ = connection.task.Result.socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
						}
					}

					if (completed == timeout)
					{
						if (this.Log().IsEnabled(LogLevel.Error))
						{
							this.Log().LogError("Failed to connect to the server (timeout).");
						}

						return;
					}

					var connected = ((Task<(Uri endPoint, WebSocket socket)>)completed).Result;

					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().LogDebug($"Connected to {connected.endPoint}");
					}

					_webSocket = connected.socket;
					await ProcessMessages();
				}
				else
				{
					if (this.Log().IsEnabled(LogLevel.Warning))
					{
						this.Log().LogWarning($"No server addresses provided, skipping.");
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
			}
		}

		private async Task ProcessMessages()
		{
			_ = InitializeServerProcessors();

			foreach(var processor in _processors)
			{
				await processor.Value.Initialize();
			}

			StartKeepAliveTimer();

			while (_webSocket != null
				&& await WebSocketHelper.ReadFrame(_webSocket, CancellationToken.None) is HotReload.Messages.Frame frame)
			{
				if (frame.Scope == "RemoteControlServer")
				{
					if (frame.Name == KeepAliveMessage.Name)
					{
						if (this.Log().IsEnabled(LogLevel.Trace))
						{
							this.Log().Trace($"Server Keepalive frame");
						}
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

						await processor.ProcessFrame(frame);
					}
					else
					{
						if (this.Log().IsEnabled(LogLevel.Error))
						{
							this.Log().LogError($"Unknown Frame scope {frame.Scope}");
						}
					}
				}
			}
		}

		private void StartKeepAliveTimer()
		{
			KeepAliveMessage keepAlive = new();

			_keepAliveTimer = new Timer(_ => {

				try
				{
					if (this.Log().IsEnabled(LogLevel.Trace))
					{
						this.Log().Trace($"Sending Keepalive frame");
					}

					_ = SendMessage(keepAlive);
				}
				catch(Exception)
				{
					if (this.Log().IsEnabled(LogLevel.Trace))
					{
						this.Log().Trace($"Keepalive failed");
					}

					_keepAliveTimer?.Dispose();
				}
			});

			_keepAliveTimer.Change(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
		}

		private async Task InitializeServerProcessors()
		{
			if (AppType.Assembly.GetCustomAttributes(typeof(ServerProcessorsConfigurationAttribute), false) is ServerProcessorsConfigurationAttribute[] configs)
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

		public static RemoteControlClient Initialize(Type appType)
			=> Instance = new RemoteControlClient(appType);

		public async Task SendMessage(IMessage message)
		{
			if (_webSocket != null)
			{
				await WebSocketHelper.SendFrame(
					_webSocket,
					HotReload.Messages.Frame.Create(
						1,
						message.Scope,
						message.Name,
						message
					),
					CancellationToken.None);
			}
			else
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().LogError("Unable send message, no connection available");
				}
			}
		}
	}
}
