using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.IO.Pipes;
using System.Net.WebSockets;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StreamJsonRpc;
using Uno.Extensions;
using Uno.UI.RemoteControl.Helpers;
using Uno.UI.RemoteControl.Host.IdeChannel;
using Uno.UI.RemoteControl.HotReload.Messages;
using Uno.UI.RemoteControl.Messages;
using Uno.UI.RemoteControl.Messaging.IdeChannel;
using static Uno.UI.RemoteControl.Host.RemoteControlServer;

namespace Uno.UI.RemoteControl.Host;

internal class RemoteControlServer : IRemoteControlServer, IDisposable
{
    private readonly object _loadContextGate = new();
    private static readonly Dictionary<string, (AssemblyLoadContext Context, int Count)> _loadContexts = new();
    private readonly Dictionary<string, IServerProcessor> _processors = new();

    private string? _resolveAssemblyLocation;
    private WebSocket? _socket;
    private IdeChannelServer? _ideChannelServer;
    private readonly List<string> _appInstanceIds = new();
    private readonly IConfiguration _configuration;
    private readonly IIdeChannelServerProvider _ideChannelProvider;
    private readonly IServiceProvider _serviceProvider;

    public RemoteControlServer(IConfiguration configuration, IIdeChannelServerProvider ideChannelProvider, IServiceProvider serviceProvider)
    {
        _configuration = configuration;
        _ideChannelProvider = ideChannelProvider;
        _serviceProvider = serviceProvider;

        if (this.Log().IsEnabled(LogLevel.Debug))
        {
            this.Log().LogDebug("Starting RemoteControlServer");
        }
    }

    string IRemoteControlServer.GetServerConfiguration(string key)
        => _configuration[key] ?? "";

    private AssemblyLoadContext GetAssemblyLoadContext(string applicationId)
    {
        lock (_loadContextGate)
        {
            if (_loadContexts.TryGetValue(applicationId, out var lc))
            {
                _loadContexts[applicationId] = (lc.Context, lc.Count + 1);

                return lc.Context;
            }

            var loadContext = new AssemblyLoadContext(applicationId, isCollectible: true);
            loadContext.Unloading += (e) =>
            {
                if (this.Log().IsEnabled(LogLevel.Debug))
                {
                    this.Log().LogDebug("Unloading assembly context {name}", e.Name);
                }
            };

            // Add custom resolving so we can find dependencies even when the processor assembly
            // is built for a different .net version than the host process.
            loadContext.Resolving += (context, assemblyName) =>
            {
                if (!string.IsNullOrWhiteSpace(_resolveAssemblyLocation))
                {
                    try
                    {
                        var dir = Path.GetDirectoryName(_resolveAssemblyLocation);
                        if (!string.IsNullOrEmpty(dir))
                        {
                            var relPath = Path.Combine(dir, assemblyName.Name + ".dll");
                            if (File.Exists(relPath))
                            {
                                if (this.Log().IsEnabled(LogLevel.Trace))
                                {
                                    this.Log().LogTrace("Loading assembly from resolved path: {relPath}", relPath);
                                }

                                using var fs = File.Open(relPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                                return context.LoadFromStream(fs);
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
                else
                {
                    if (this.Log().IsEnabled(LogLevel.Debug))
                    {
                        this.Log().LogDebug("Failed for identify location of dependency: {assemblyName}", assemblyName);
                    }
                }

                // We haven't found the assembly in our context, let the runtime
                // find it using standard resolution mechanisms.
                return null;
            };

            if (!_loadContexts.TryAdd(applicationId, (loadContext, 1)))
            {
                if (this.Log().IsEnabled(LogLevel.Trace))
                {
                    this.Log().LogTrace("Failed to add a LoadContext for : {appId}", applicationId);
                }
            }

            return loadContext;
        }
    }

    private void RegisterProcessor(IServerProcessor hotReloadProcessor)
        => _processors[hotReloadProcessor.Scope] = hotReloadProcessor;

    public IdeChannelServer? IDEChannelServer => _ideChannelServer;

    public async Task RunAsync(WebSocket socket, CancellationToken ct)
    {
        _socket = socket;

        await TryStartIDEChannelAsync();

        while (await WebSocketHelper.ReadFrame(socket, ct) is Frame frame)
        {
            if (frame.Scope == "RemoteControlServer")
            {
                if (frame.Name == ProcessorsDiscovery.Name)
                {
                    ProcessDiscoveryFrame(frame);
                    continue;
                }

                if (frame.Name == KeepAliveMessage.Name)
                {
                    if (this.Log().IsEnabled(LogLevel.Trace))
                    {
                        this.Log().LogTrace($"Client Keepalive frame");
                    }

                    await SendFrame(new KeepAliveMessage());
                    continue;
                }
            }

            if (_processors.TryGetValue(frame.Scope, out var processor))
            {
                if (this.Log().IsEnabled(LogLevel.Debug))
                {
                    this.Log().LogDebug("Received Frame [{Scope} / {Name}] to be processed by {processor}", frame.Scope, frame.Name, processor);
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

    private async Task TryStartIDEChannelAsync()
    {
        _ideChannelServer = await _ideChannelProvider.GetIdeChannelServerAsync();
        _ideChannelServer += OnMessageFromIDE;
    }

    private void OnMessageFromIDE(object? sender, IdeMessage ideMessage)
    {
        // TODO: Work out which processors care about the IdeMessage
        // TODO: Need something like Scope that is used in the webServer messages
    }

    private void ProcessDiscoveryFrame(Frame frame)
    {
        var msg = JsonConvert.DeserializeObject<ProcessorsDiscovery>(frame.Content)!;
        var serverAssemblyName = typeof(IServerProcessor).Assembly.GetName().Name;

        var assemblies = new List<System.Reflection.Assembly>();

        _resolveAssemblyLocation = string.Empty;

        if (!_appInstanceIds.Contains(msg.AppInstanceId))
        {
            _appInstanceIds.Add(msg.AppInstanceId);
        }

        var assemblyLoadContext = GetAssemblyLoadContext(msg.AppInstanceId);

        // If BasePath is a specific file, try and load that
        if (File.Exists(msg.BasePath))
        {
            try
            {
                using var fs = File.Open(msg.BasePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                assemblies.Add(assemblyLoadContext.LoadFromStream(fs));

                _resolveAssemblyLocation = msg.BasePath;
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

            // Additional processors may not need the directory added immediately above.
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
                    assemblies.Add(assemblyLoadContext.LoadFromAssemblyPath(file));
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
                if (assemblies.Count > 1 || string.IsNullOrEmpty(_resolveAssemblyLocation))
                {
                    _resolveAssemblyLocation = asm.Location;
                }

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
                    this.Log().LogError("Failed to create instance of server processor in  {Asm} : {Exc}", asm, exc);
                }
            }
        }

        // Being thorough about trying to ensure everything is unloaded
        assemblies.Clear();
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

    public async Task SendMessageToIDEAsync(IdeMessage message)
    {
        if (IDEChannelServer is not null)
        {
            await IDEChannelServer.SendToIdeAsync(message);
        }
    }

    public void Dispose()
    {
        if (_ideChannelServer is not null)
        {
            _ideChannelServer.MessageFromIDE -= OnMessageFromIDE;
        }

        foreach (var processor in _processors)
        {
            processor.Value.Dispose();
        }

        // Unload any AssemblyLoadContexts not being used by any current connection
        foreach (var appId in _appInstanceIds)
        {
            lock (_loadContextGate)
            {
                if (_loadContexts.TryGetValue(appId, out var lc))
                {
                    if (lc.Count > 1)
                    {
                        _loadContexts[appId] = (lc.Context, lc.Count - 1);
                    }
                    else
                    {
                        try
                        {
                            _loadContexts[appId].Context.Unload();

                            _loadContexts.Remove(appId);
                        }
                        catch (Exception exc)
                        {
                            if (this.Log().IsEnabled(LogLevel.Error))
                            {
                                this.Log().LogError("Failed to unload AssemblyLoadContext for '{appId}' : {Exc}", appId, exc);
                            }
                        }
                    }
                }
            }
        }
    }
}
