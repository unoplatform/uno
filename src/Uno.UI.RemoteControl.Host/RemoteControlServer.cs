#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Uno.UI.RemoteControl;
using Uno.UI.RemoteControl.Helpers;
using Uno.UI.RemoteControl.HotReload.Messages;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.UI.RemoteControl.Messages;
using System.Runtime.Loader;
using Microsoft.Extensions.Configuration;

namespace Uno.UI.RemoteControl.Host
{
	internal class RemoteControlServer : IRemoteControlServer, IDisposable
	{
		private readonly Dictionary<string, IServerProcessor> _processors = new Dictionary<string, IServerProcessor>();

		private WebSocket _socket;
		private readonly IConfiguration _configuration;
		private readonly AssemblyLoadContext _loadContext;

		public RemoteControlServer(IConfiguration configuration)
		{
			_configuration = configuration;
			_loadContext = new AssemblyLoadContext(null, isCollectible: true);
			_loadContext.Unloading += (e) => {
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogDebug("Unloading assembly context");
				}
			};

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug("Starting RemoteControlServer");
			}
		}

		string IRemoteControlServer.GetServerConfiguration(string key)
			=> _configuration[key];

		private void RegisterProcessor(IServerProcessor hotReloadProcessor)
		{
			_processors[hotReloadProcessor.Scope] = hotReloadProcessor;
		}

		public async Task Run(WebSocket socket, CancellationToken ct)
		{
			_socket = socket;

			while (await WebSocketHelper.ReadFrame(socket, ct) is Frame frame)
			{
				if (frame.Scope == "RemoteControlServer")
				{
					if (frame.Name == ProcessorsDiscovery.Name)
					{
						ProcessDiscoveryFrame(frame);
					}

					if (frame.Name == KeepAliveMessage.Name)
					{
						if (this.Log().IsEnabled(LogLevel.Trace))
						{
							this.Log().LogTrace($"Client Keepalive frame");
						}

						await SendFrame(new KeepAliveMessage());
					}
				}

				if (_processors.TryGetValue(frame.Scope, out var processor))
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

		private void ProcessDiscoveryFrame(Frame frame)
		{
			var msg = JsonConvert.DeserializeObject<ProcessorsDiscovery>(frame.Content);
			var serverAssemblyName = typeof(IServerProcessor).Assembly.GetName().Name;

			var basePath = msg.BasePath.Replace('/', Path.DirectorySeparatorChar);

#if NET6_0_OR_GREATER
			basePath = Path.Combine(basePath, "net6.0");
#else
			basePath = Path.Combine(basePath, "netcoreapp3.1");
#endif

			var assemblies = new List<System.Reflection.Assembly>();

			foreach (var file in Directory.GetFiles(basePath, "Uno.*.dll"))
			{
				if (Path.GetFileNameWithoutExtension(file).Equals(serverAssemblyName, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogDebug($"Discovery: Loading {file}");
				}

				assemblies.Add(_loadContext.LoadFromAssemblyPath(file));
			}

			foreach(var asm in assemblies)
			{
				var attributes = asm.GetCustomAttributes(typeof(ServerProcessorAttribute), false);

				foreach (var processorAttribute in attributes)
				{
					if (processorAttribute is ServerProcessorAttribute processor)
					{
						if (this.Log().IsEnabled(LogLevel.Debug))
						{
							this.Log().LogDebug($"Discovery: Registering {processor.ProcessorType}");
						}

						RegisterProcessor((IServerProcessor)Activator.CreateInstance(processor.ProcessorType, this));
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
