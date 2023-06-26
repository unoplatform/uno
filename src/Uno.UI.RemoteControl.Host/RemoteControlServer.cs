using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Uno.Extensions;
using Uno.UI.RemoteControl.Helpers;
using Uno.UI.RemoteControl.HotReload.Messages;
using Uno.UI.RemoteControl.Messages;

namespace Uno.UI.RemoteControl.Host
{
	internal class RemoteControlServer : IRemoteControlServer, IDisposable
	{
		private System.Reflection.Assembly? _loopAssembly;
		private WebSocket? _socket;
		private readonly IConfiguration _configuration;
		private readonly AssemblyLoadContext _loadContext;

		public RemoteControlServer(IConfiguration configuration)
		{
			_configuration = configuration;
			_loadContext = new AssemblyLoadContext(null, isCollectible: true);
			_loadContext.Unloading += (e) =>
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogDebug("Unloading assembly context");
				}
			};

			// Add custom resolving so we can find dependencies even when the processor assembly
			// is built for a different .net version than the host process.
			_loadContext.Resolving += (context, assemblyName) =>
			{
				if (_loopAssembly is not null)
				{
					try
					{
						var loc = _loopAssembly.Location;

						if (!string.IsNullOrWhiteSpace(loc))
						{
							var dir = Path.GetDirectoryName(loc);

							if (!string.IsNullOrEmpty(dir))
							{
								var relPath = Path.Combine(dir, assemblyName.Name + ".dll");

								if (File.Exists(relPath))
								{
									return context.LoadFromAssemblyPath(relPath);
								}
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

				return context.LoadFromAssemblyName(assemblyName);
			};

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug("Starting RemoteControlServer");
			}
		}

		string IRemoteControlServer.GetServerConfiguration(string key)
			=> _configuration[key] ?? "";

		private void RegisterProcessor(IServerProcessor processor)
		{
			if (SharedProcessorPool.Instance.Processors.ContainsKey(processor.Scope))
			{
				// Remove any exising processor instances from the same source
				foreach (var registeredProcessor in SharedProcessorPool.Instance.Processors[processor.Scope])
				{
					if (registeredProcessor.GetType().FullName == processor.GetType().FullName)
					{
						SharedProcessorPool.Instance.Processors[processor.Scope].Remove(registeredProcessor);
						break;
					}
				}

				SharedProcessorPool.Instance.Processors[processor.Scope].Add(processor);
			}
			else
			{
				SharedProcessorPool.Instance.Processors.Add(processor.Scope, new List<IServerProcessor>() { processor });
			}
		}

		public async Task Run(WebSocket socket, CancellationToken ct)
		{
			_socket = socket;

			while (await WebSocketHelper.ReadFrame(socket, ct) is Frame frame)
			{
				if (frame.Scope == "RemoteControlServer")
				{
					switch (frame.Name)
					{
						case ProcessorsDiscovery.Name:
							ProcessDiscoveryFrame(frame);
							break;

						case ProcessorRemoval.Name:
							ProcessRemovalFrame(frame);
							break;

						case KeepAliveMessage.Name:
							if (this.Log().IsEnabled(LogLevel.Trace))
							{
								this.Log().LogTrace($"Client Keepalive frame");
							}

							await SendFrame(new KeepAliveMessage());
							break;
					}
				}

				if (SharedProcessorPool.Instance.Processors.TryGetValue(frame.Scope, out var processors))
				{
					if (this.Log().IsEnabled(LogLevel.Trace))
					{
						this.Log().LogTrace($"Received Frame [{frame.Scope} / {frame.Name}]");
					}

					foreach (var processor in processors)
					{
						await processor.ProcessFrame(frame);
					}
				}
				else
				{
					if (this.Log().IsEnabled(LogLevel.Trace) && frame.Scope != "RemoteControlServer")
					{
						this.Log().LogTrace($"Unknown Frame [{frame.Scope} / {frame.Name}]");
					}
				}
			}
		}

		private void ProcessDiscoveryFrame(Frame frame)
		{
			var msg = JsonConvert.DeserializeObject<ProcessorsDiscovery>(frame.Content)!;
			var serverAssemblyName = typeof(IServerProcessor).Assembly.GetName().Name;

			var assemblies = new List<System.Reflection.Assembly>();

			// If BasePath is a specific file, try and load that
			if (File.Exists(msg.BasePath))
			{
				try
				{
					assemblies.Add(_loadContext.LoadFromAssemblyPath(msg.BasePath));
				}
				catch (Exception exc)
				{
					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().LogDebug($"Failed to load assembly {msg.BasePath} : {exc}");
					}
				}
			}
			else
			{
				// As BasePath is a directory, try and load processors from assemblies within that dir
				var basePath = msg.BasePath.Replace('/', Path.DirectorySeparatorChar);

#if NET7_0_OR_GREATER
				basePath = Path.Combine(basePath, "net7.0");
#elif NET6_0_OR_GREATER
				basePath = Path.Combine(basePath, "net6.0");
#else
				basePath = Path.Combine(basePath, "netcoreapp3.1");
#endif

				// Additional processors may not need the directory added immmediately above.
				if (!Directory.Exists(basePath))
				{
					basePath = msg.BasePath;
				}

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

					try
					{
						assemblies.Add(_loadContext.LoadFromAssemblyPath(file));
					}
					catch (Exception exc)
					{
						// With additional processors there may be duplicates of assemblies already loaded
						if (this.Log().IsEnabled(LogLevel.Debug))
						{
							this.Log().LogDebug($"Failed to load assembly {file} : {exc}");
						}
					}
				}
			}

			foreach (var asm in assemblies)
			{
				try
				{
					_loopAssembly = asm;

					var attributes = asm.GetCustomAttributes(typeof(ServerProcessorAttribute), false);

					foreach (var processorAttribute in attributes)
					{
						if (processorAttribute is ServerProcessorAttribute processor)
						{
							if (this.Log().IsEnabled(LogLevel.Debug))
							{
								this.Log().LogDebug($"Discovery: Registering {processor.ProcessorType}");
							}

							if (Activator.CreateInstance(processor.ProcessorType, this) is IServerProcessor serverProcessor)
							{
								RegisterProcessor(serverProcessor);
							}
							else
							{
								if (this.Log().IsEnabled(LogLevel.Debug))
								{
									this.Log().LogDebug($"Failed to create server processor {processor.ProcessorType}");
								}
							}
						}
					}
				}
				catch (Exception exc)
				{
					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().LogDebug($"Failed to create instance of server processor in  {asm} : {exc}");
					}
				}
			}
		}

		private void ProcessRemovalFrame(Frame frame)
		{
			var msg = JsonConvert.DeserializeObject<ProcessorRemoval>(frame.Content)!;

			foreach (var processor in SharedProcessorPool.Instance.Processors[msg.ProcessorScope])
			{
				if (processor.GetType().Assembly.Location == msg.Path)
				{
					SharedProcessorPool.Instance.Processors[msg.ProcessorScope].Remove(processor);
					break;
				}
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
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogDebug($"Failed to send, no connection available");
				}
			}
		}

		public void Dispose()
		{
			foreach (var processors in SharedProcessorPool.Instance.Processors.Values)
			{
				foreach (var processor in processors)
				{
					processor.Dispose();
				}
			}
		}

		private class SharedProcessorPool
		{
			private readonly Dictionary<string, List<IServerProcessor>> _processors = new();

			internal static SharedProcessorPool Instance { get; } = new SharedProcessorPool();

			public Dictionary<string, List<IServerProcessor>> Processors => _processors;
		}
	}
}
