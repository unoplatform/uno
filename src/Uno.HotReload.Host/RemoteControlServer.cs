using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Uno.HotReload.Host.HotReload;
using Uno.UI.HotReload;
using Uno.UI.HotReload.Helpers;
using Uno.UI.HotReload.HotReload.Messages;

namespace Uno.HotReload.Host
{
	internal class RemoteControlServer : IRemoteControlServer
	{
		private WebSocket _socket;
		private readonly Dictionary<string, IServerProcessor> _processors = new Dictionary<string, IServerProcessor>();

		public RemoteControlServer()
		{
			RegisterProcessor(new HotReload.ServerHotReloadProcessor(this));
		}

		private void RegisterProcessor(IServerProcessor hotReloadProcessor)
		{
			_processors[hotReloadProcessor.Scope] = hotReloadProcessor;
		}

		public async Task Run(WebSocket socket, CancellationToken ct)
		{
			_socket = socket;

			while (await WebSocketHelper.ReadFrame(socket, ct) is Frame frame)
			{
				if(_processors.TryGetValue(frame.Scope, out var processor))
				{
					Console.WriteLine($"Received Frame [{frame.Scope} / {frame.Name}]");
					await processor.ProcessFrame(frame);
				}
				else
				{
					Console.WriteLine($"Unknown Frame [{frame.Scope} / {frame.Name}]");
				}
			}
		}

		public async Task SendFrame(IMessage message)
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
	}
}
