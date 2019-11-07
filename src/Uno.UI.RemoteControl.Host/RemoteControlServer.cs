using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Uno.UI.RemoteControl.Host.HotReload;
using Uno.UI.RemoteControl;
using Uno.UI.RemoteControl.Helpers;
using Uno.UI.RemoteControl.HotReload.Messages;
using Microsoft.Extensions.Logging;
using Uno.Extensions;

namespace Uno.UI.RemoteControl.Host
{
	internal class RemoteControlServer : IRemoteControlServer, IDisposable
	{
		private WebSocket _socket;
		private readonly Dictionary<string, IServerProcessor> _processors = new Dictionary<string, IServerProcessor>();

		public RemoteControlServer()
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug("Starting RemoteControlServer");
			}

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
					if (this.Log().IsEnabled(LogLevel.Trace))
					{
						this.Log().LogTrace($"Received Frame [{frame.Scope} / {frame.Name}]");
					}

					await processor.ProcessFrame(frame);
				}
				else
				{
					if (this.Log().IsEnabled(LogLevel.Trace))
					{
						this.Log().LogTrace($"Unknown Frame [{frame.Scope} / {frame.Name}]");
					}
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

		public void Dispose()
		{
			foreach(var processor in _processors)
			{
				processor.Value.Dispose();
			}
		}
	}
}
