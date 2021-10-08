using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Uno.Extensions;
using Uno.UI.RemoteControl.Helpers;
using Uno.UI.RemoteControl.HotReload;
using Uno.UI.RemoteControl.HotReload.Messages;
using Uno.UI.RemoteControl.Messages;

namespace Uno.UI.RemoteControl
{
	public class RemoteControlClient : IRemoteControlClient
	{
		public static RemoteControlClient Instance { get; private set; }

		public Type AppType { get; }

		private readonly (string endpoint, int port)[] _serverAddresses;
		private WebSocket _webSocket;
		private Dictionary<string, IRemoteControlProcessor> _processors = new Dictionary<string, IRemoteControlProcessor>();

		private RemoteControlClient(Type appType)
		{
			AppType = appType;

			if(appType.Assembly.GetCustomAttributes(typeof(ServerEndpointAttribute), false) is ServerEndpointAttribute[] endpoints)
			{
				IEnumerable<(string endpoint, int port)> GetAddresses()
				{
					foreach (var endpoint in endpoints)
					{
						if (endpoint.Port == 0)
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
			StartConnection();
		}

		private void RegisterProcessor(IRemoteControlProcessor processor)
		{
			_processors[processor.Scope] = processor;
		}

		private async Task StartConnection()
		{
			try
			{
				async Task<(string endPoint, int port, WebSocket socket)> Connect(string endpoint, int port, CancellationToken ct)
				{
#if __WASM__
					var s = new Uno.Wasm.WebSockets.WasmWebSocket();
#else
					var s = new ClientWebSocket();
#endif

					if(port == 443)
					{
#if __WASM__
						if (endpoint.EndsWith("gitpod.io"))
						{
							var originParts = endpoint.Split('-');

							var currentHost = Foundation.WebAssemblyRuntime.InvokeJS("window.location.hostname");
							var targetParts = currentHost.Split('-');

							endpoint = originParts[0] + '-' + currentHost.Substring(targetParts[0].Length + 1);
						}
#endif

						await s.ConnectAsync(new Uri($"wss://{endpoint}/rc"), ct);
					}
					else
					{
						await s.ConnectAsync(new Uri($"ws://{endpoint}:{port}/rc"), ct);
					}

					return (endpoint, port, s);
				}

				var connections = _serverAddresses
					.Where(adr => adr.port != 0)
					.Select(s =>
					{
						if (this.Log().IsEnabled(LogLevel.Debug))
						{
							this.Log().LogDebug($"Connecting to {s}...");
						}

						var cts = new CancellationTokenSource();
						var task = Connect(s.endpoint, s.port, cts.Token);

						return (task, cts);
					})
					.ToArray();

				var timeout = Task.Delay(30000);
				var completed = await Task.WhenAny(connections.Select(c => c.task).Concat(timeout));

				foreach (var connection in connections)
				{
					if (connection.task == completed)
					{
						continue;
					}

					connection.cts.Cancel();
					if (connection.task.Status == TaskStatus.RanToCompletion)
					{
						connection.task.Result.socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
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

				var connected = ((Task<(string endPoint, int port, WebSocket socket)>)completed).Result;

				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogDebug($"Connected to {connected.endPoint}:{connected.port}");
				}

				_webSocket = connected.socket;
				await ProcessMessages();
			}
			catch (Exception ex)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().LogError($"Failed to connect to the server ({ex})", ex);
				}
			}
		}

		private async Task ProcessMessages()
		{
			InitializeServerProcessors();

			foreach(var processor in _processors)
			{
				await processor.Value.Initialize();
			}

			while (await WebSocketHelper.ReadFrame(_webSocket, CancellationToken.None) is HotReload.Messages.Frame frame)
			{
				if (_processors.TryGetValue(frame.Scope, out var processor))
				{
					if (this.Log().IsEnabled(LogLevel.Trace))
					{
						this.Log().LogTrace($"Received frame [{frame.Scope}/{frame.Name}]");
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
	}
}
