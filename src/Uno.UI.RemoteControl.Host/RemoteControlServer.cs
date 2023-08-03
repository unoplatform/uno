using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Uno.UI.RemoteControl.Helpers;
using Uno.UI.RemoteControl.HotReload.Messages;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.UI.RemoteControl.Messages;
using System.Runtime.Loader;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace Uno.UI.RemoteControl.Host
{
	internal class RemoteControlServer : IRemoteControlServer, IDisposable
	{
		private readonly static Dictionary<string, AssemblyLoadContext> _loadContexts = new();
		private readonly Dictionary<string, IServerProcessor> _processors = new();

		private System.Reflection.Assembly? _loopAssembly;
		private WebSocket? _socket;
		private string? _appInstanceId;
		private readonly IConfiguration _configuration;

		public RemoteControlServer(IConfiguration configuration)
		{
			_configuration = configuration;

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug("Starting RemoteControlServer");
			}
		}

		string IRemoteControlServer.GetServerConfiguration(string key)
			=> _configuration[key] ?? "";

		private AssemblyLoadContext GetAssemblyLoadContext(string applicationId)
		{
			if (_loadContexts.TryGetValue(applicationId, out var context))
			{
				return context;
			}

			var loadContext = new AssemblyLoadContext(applicationId, isCollectible: true);
			loadContext.Unloading += (e) =>
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogDebug("Unloading assembly context");
				}
			};

			// Add custom resolving so we can find dependencies even when the processor assembly
			// is built for a different .net version than the host process.
			loadContext.Resolving += (context, assemblyName) =>
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
									if (this.Log().IsEnabled(LogLevel.Trace))
									{
										this.Log().LogTrace("Loading assembly from resolved path: {relPath}", relPath);
									}

									return context.LoadFromAssemblyPath(relPath);
								}
							}
						}
						else
						{
							if (this.Log().IsEnabled(LogLevel.Debug))
							{
								this.Log().LogDebug("Failed for identify location of dependency: {assemblyName}", assemblyName);
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

			_loadContexts.Add(applicationId, loadContext);

			return loadContext;
		}

		private void RegisterProcessor(IServerProcessor hotReloadProcessor)
			=> _processors[hotReloadProcessor.Scope] = hotReloadProcessor;

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
					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().LogDebug("Received Frame [{Scope} / {Name}]", frame.Scope, frame.Name);
					}

					await processor.ProcessFrame(frame);
				}
				else
				{
					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().LogDebug("Unknown Frame [{Scope} / {Name}]", frame.Scope, frame.Name);
					}
				}
			}
		}

		private void ProcessDiscoveryFrame(Frame frame)
		{
			var msg = JsonConvert.DeserializeObject<ProcessorsDiscovery>(frame.Content)!;
			var serverAssemblyName = typeof(IServerProcessor).Assembly.GetName().Name;

			var assemblies = new List<System.Reflection.Assembly>();

			_appInstanceId = msg.AppInstanceId;
			var asmblyLoadContext = GetAssemblyLoadContext(msg.AppInstanceId);

			// If BasePath is a specific file, try and load that
			if (File.Exists(msg.BasePath))
			{
				try
				{
					assemblies.Add(asmblyLoadContext.LoadFromAssemblyPath(msg.BasePath));
				}
				catch (Exception exc)
				{
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().LogError("Failed to load assembly {BasePath} : {Exc}", msg.BasePath, exc);
					}
				}
			}
			else
			{
				// As BasePath is a directory, try and load processors from assemblies within that dir
				var basePath = msg.BasePath.Replace('/', Path.DirectorySeparatorChar);

#if NET8_0_OR_GREATER
				basePath = Path.Combine(basePath, "net8.0");
#elif NET7_0_OR_GREATER
				basePath = Path.Combine(basePath, "net7.0");
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
						this.Log().LogDebug("Discovery: Loading {File}", file);
					}

					try
					{
						assemblies.Add(asmblyLoadContext.LoadFromAssemblyPath(file));
					}
					catch (Exception exc)
					{
						// With additional processors there may be duplicates of assemblies already loaded
						if (this.Log().IsEnabled(LogLevel.Debug))
						{
							this.Log().LogDebug("Failed to load assembly {File} : {Exc}", file, exc);
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
								this.Log().LogDebug("Discovery: Registering {ProcessorType}", processor.ProcessorType);
							}

							if (asm.CreateInstance(processor.ProcessorType.FullName!, ignoreCase: false, bindingAttr: BindingFlags.Instance | BindingFlags.Public, binder: null, args: new[] { this }, culture: null, activationAttributes: null) is IServerProcessor serverProcessor)
							{
								RegisterProcessor(serverProcessor);
							}
							else
							{
								if (this.Log().IsEnabled(LogLevel.Debug))
								{
									this.Log().LogDebug("Failed to create server processor {ProcessorType}", processor.ProcessorType);
								}
							}
						}
					}
				}
				catch (Exception exc)
				{
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().LogError("Failed to create instace of server processor in  {Asm} : {Exc}", asm, exc);
					}
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
			foreach (var processor in _processors)
			{
				processor.Value.Dispose();
			}
		}
	}
}
