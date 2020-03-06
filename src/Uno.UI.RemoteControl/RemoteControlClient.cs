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
using Uno.Wasm.WebSockets;

namespace Uno.UI.RemoteControl
{
	public class RemoteControlClient : IRemoteControlClient
	{
		public static RemoteControlClient Instance { get; private set; }

		public Type AppType { get; }

		private readonly (string endpoint, int port)[] _serverAdresses;
		private WebSocket _webSocket;
		private Dictionary<string, IRemoteControlProcessor> _processors = new Dictionary<string, IRemoteControlProcessor>();

		private RemoteControlClient(Type appType)
		{
			AppType = appType;

			if(appType.Assembly.GetCustomAttributes(typeof(ServerEndpointAttribute), false) is ServerEndpointAttribute[] endpoints)
			{
				_serverAdresses = endpoints
					.Select(e => (endpoint: e.Endpoint, port: e.Port))
					.ToArray();
			}

			StartConnection();

			RegisterProcessor(new HotReload.ClientHotReloadProcessor(this));
		}

		private void RegisterProcessor(IRemoteControlProcessor processor)
		{
			_processors[processor.Scope] = processor;
		}

		private async Task StartConnection()
		{
			try
			{
				async Task<WebSocket> Connect(string endpoint, int port, CancellationToken ct)
				{
#if __WASM__
					var s = new WasmWebSocket();
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

					return s;
				}

				var connections = _serverAdresses.Select(s =>
				{
					var cts = new CancellationTokenSource();

					if (s.port == 0)
					{
						return (
							task: Task.FromException<WebSocket>(new InvalidOperationException($"Failed to get remote control server port from the IDE")),
							cts: cts
						);
					}
					else
					{
						if (this.Log().IsEnabled(LogLevel.Debug))
						{
							this.Log().LogDebug($"Connecting to {s}...");
						}

						var task = Connect(s.endpoint, s.port, cts.Token);
						return (task, cts);
					}
				}).ToArray();

				var allCts = new TaskCompletionSource<int>();

				for (int i = 0; i < connections.Length; i++)
				{
					var connectionIndex = i;
					connections[i]
						.task
						.ContinueWith(a => {
							if(a.Status == TaskStatus.RanToCompletion)
							{
								allCts.SetResult(connectionIndex);
							}
						});
				}

				Task.Delay(30000)
					.ContinueWith(a => allCts.SetException(new TimeoutException()));

				var index = await allCts.Task;

				for (int i = 0; i < connections.Length; i++)
				{
					if (i != index)
					{
						var connection = connections[i];
						connection.cts.Cancel();

						if (connection.task.Status == TaskStatus.RanToCompletion)
						{
							connections[i].task.Result.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
						}
					}
				}

				_webSocket = connections[index].task.Result;

				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogDebug($"Connected to {_serverAdresses[index]}");
				}

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

		public static RemoteControlClient Initialize(Type appType)
			=> Instance = new RemoteControlClient(appType);

		async Task IRemoteControlClient.SendMessage(IMessage message)
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
