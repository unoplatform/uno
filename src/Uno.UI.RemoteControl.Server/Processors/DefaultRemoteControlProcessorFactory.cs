using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uno.UI.RemoteControl.Helpers;
using Uno.UI.RemoteControl.Host;
using Uno.UI.RemoteControl.Messages;
using Uno.UI.RemoteControl.ServerCore;

namespace Uno.UI.RemoteControl.Server.Processors;

/// <summary>
/// Default implementation that discovers processors by loading assemblies from disk and instantiating them via dependency injection.
/// </summary>
internal sealed class DefaultRemoteControlProcessorFactory : IRemoteControlProcessorFactory
{
	private static readonly Lock _loadContextGate = new();
	private static readonly Dictionary<string, (AssemblyLoadContext Context, int Count)> _loadContexts = new(StringComparer.Ordinal);
	private static readonly Dictionary<string, string> _resolveAssemblyLocations = new(StringComparer.Ordinal);
	private static readonly string _serverAssemblyName = typeof(IServerProcessor).Assembly.GetName().Name ?? string.Empty;
	private readonly IServiceProvider _serviceProvider;
	private readonly ILogger<DefaultRemoteControlProcessorFactory> _logger;

	public DefaultRemoteControlProcessorFactory(IServiceProvider serviceProvider, ILogger<DefaultRemoteControlProcessorFactory> logger)
	{
		_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public ValueTask<RemoteControlProcessorDiscoveryResult> DiscoverProcessorsAsync(
		ProcessorsDiscovery discovery,
		CancellationToken ct)
	{
		if (discovery is null)
		{
			throw new ArgumentNullException(nameof(discovery));
		}

		ct.ThrowIfCancellationRequested();

		var assemblies = new List<(string Path, Assembly Assembly)>();
		var discoveredProcessors = ImmutableArray.CreateBuilder<DiscoveredProcessor>();
		var instances = new List<IServerProcessor>();
		LoadContextLease? lease = null;

		try
		{
			lease = AcquireLoadContext(discovery.AppInstanceId);
			assemblies.AddRange(LoadAssemblies(discovery, lease.Context));

			foreach (var asm in assemblies)
			{
				UpdateResolveBase(discovery.AppInstanceId, asm.Path, assemblies.Count);

				foreach (var attribute in asm.Assembly.GetCustomAttributes(typeof(ServerProcessorAttribute), inherit: false)
					.OfType<ServerProcessorAttribute>())
				{
					ct.ThrowIfCancellationRequested();

					if (_logger.IsEnabled(LogLevel.Debug))
					{
						_logger.LogDebug("Discovery: Registering {ProcessorType}", attribute.ProcessorType);
					}

					try
					{
						var instance = ActivatorUtilities.CreateInstance(_serviceProvider, attribute.ProcessorType);
						if (instance is IServerProcessor serverProcessor)
						{
							instances.Add(serverProcessor);
							discoveredProcessors.Add(new(
								asm.Path,
								attribute.ProcessorType.FullName ?? attribute.ProcessorType.Name,
								VersionHelper.GetVersion(attribute.ProcessorType),
								IsLoaded: true));
						}
						else
						{
							discoveredProcessors.Add(new(
								asm.Path,
								attribute.ProcessorType.FullName ?? attribute.ProcessorType.Name,
								VersionHelper.GetVersion(attribute.ProcessorType),
								IsLoaded: false));
							_logger.LogWarning("ActivatorUtilities returned null for processor {Processor}", attribute.ProcessorType);
						}
					}
					catch (Exception ex)
					{
						discoveredProcessors.Add(new(
							asm.Path,
							attribute.ProcessorType.FullName ?? attribute.ProcessorType.Name,
							VersionHelper.GetVersion(attribute.ProcessorType),
							IsLoaded: false,
							LoadError: ex.ToString()));

						if (_logger.IsEnabled(LogLevel.Error))
						{
							_logger.LogError(ex, "Failed to create server processor {ProcessorType} from {AssemblyPath}", attribute.ProcessorType.FullName, asm.Path);
							_logger.LogError("Processor assembly location: {AssemblyLocation}", attribute.ProcessorType.Assembly.Location);
						}
					}
				}
			}

			var result = new RemoteControlProcessorDiscoveryResult(
				assemblies.Select(a => a.Path).ToImmutableList(),
				discoveredProcessors.ToImmutable(),
				[..instances],
				lease);

			lease = null; // Ownership transferred to the caller once wrapped in the result.
			return ValueTask.FromResult(result);
		}
		catch
		{
			lease?.Dispose();
			foreach (var instance in instances)
			{
				instance.Dispose();
			}
			throw;
		}
	}

	private static void UpdateResolveBase(string appInstanceId, string assemblyPath, int assemblyCount)
	{
		lock (_loadContextGate)
		{
			if (assemblyCount > 1 ||
				!_resolveAssemblyLocations.TryGetValue(appInstanceId, out var current) ||
				string.IsNullOrEmpty(current))
			{
				_resolveAssemblyLocations[appInstanceId] = assemblyPath;
			}
		}
	}

	private IEnumerable<(string Path, Assembly Assembly)> LoadAssemblies(ProcessorsDiscovery discovery, AssemblyLoadContext loadContext)
	{
		var assemblies = new List<(string Path, Assembly Assembly)>();
		var basePath = discovery.BasePath;

		if (File.Exists(basePath))
		{
			TryLoadAssembly(discovery.AppInstanceId, loadContext, basePath, assemblies, logFailures: true);
			return assemblies;
		}

		var normalized = basePath.Replace('/', Path.DirectorySeparatorChar);
#if NET10_0_OR_GREATER
		var candidate = Path.Combine(normalized, "net10.0");
#elif NET9_0_OR_GREATER
		var candidate = Path.Combine(normalized, "net9.0");
#else
		var candidate = normalized;
#endif
		if (!Directory.Exists(candidate))
		{
			candidate = basePath;
		}

		if (!Directory.Exists(candidate))
		{
			return assemblies;
		}

		foreach (var file in Directory.GetFiles(candidate, "Uno.*.Processor*.dll"))
		{
			if (Path.GetFileNameWithoutExtension(file).Equals(_serverAssemblyName, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			if (_logger.IsEnabled(LogLevel.Debug))
			{
				_logger.LogDebug("Discovery: Loading {File}", file);
			}

			TryLoadAssembly(discovery.AppInstanceId, loadContext, file, assemblies, logFailures: false);
		}

		return assemblies;
	}

	private void TryLoadAssembly(
		string appInstanceId,
		AssemblyLoadContext loadContext,
		string assemblyPath,
		ICollection<(string Path, Assembly Assembly)> assemblies,
		bool logFailures)
	{
		try
		{
			var fullPath = Path.GetFullPath(assemblyPath);
			lock (_loadContextGate)
			{
				_resolveAssemblyLocations[appInstanceId] = fullPath;
			}
			assemblies.Add((fullPath, TryLoadAssemblyFromPath(loadContext, fullPath)));
		}
		catch (Exception ex)
		{
			if (logFailures && _logger.IsEnabled(LogLevel.Error))
			{
				_logger.LogError(ex, "Failed to load assembly {AssemblyPath}", assemblyPath);
			}
			else if (_logger.IsEnabled(LogLevel.Debug))
			{
				_logger.LogDebug(ex, "Failed to load assembly {AssemblyPath}", assemblyPath);
			}
		}
	}

	private LoadContextLease AcquireLoadContext(string appInstanceId)
	{
		lock (_loadContextGate)
		{
			if (_loadContexts.TryGetValue(appInstanceId, out var entry))
			{
				_loadContexts[appInstanceId] = (entry.Context, entry.Count + 1);
				return new LoadContextLease(this, appInstanceId, entry.Context);
			}

			var loadContext = new AssemblyLoadContext(appInstanceId, isCollectible: true);
			loadContext.Unloading += e =>
			{
				// Keep logging concise; no need for string allocations when not enabled.
				if (_logger.IsEnabled(LogLevel.Debug))
				{
					_logger.LogDebug("Unloading assembly context {Name}", e.Name);
				}
			};
			loadContext.Resolving += (_, assemblyName) => ResolveAssembly(appInstanceId, loadContext, assemblyName);

			_loadContexts[appInstanceId] = (loadContext, 1);
			return new LoadContextLease(this, appInstanceId, loadContext);
		}
	}

	private static Assembly? ResolveAssembly(string appInstanceId, AssemblyLoadContext context, AssemblyName assemblyName)
	{
		if (_resolveAssemblyLocations.TryGetValue(appInstanceId, out var location) && !string.IsNullOrWhiteSpace(location))
		{
			try
			{
				var dir = Path.GetDirectoryName(location);
				if (!string.IsNullOrEmpty(dir))
				{
					var candidate = Path.Combine(dir, assemblyName.Name + ".dll");
					if (File.Exists(candidate))
					{
						return TryLoadAssemblyFromPath(context, candidate);
					}
				}
			}
			catch
			{
				// Ignore resolving failures and let the runtime continue its standard probing.
			}
		}

		return null;
	}

	private void ReleaseLoadContext(string appInstanceId)
	{
		lock (_loadContextGate)
		{
			if (!_loadContexts.TryGetValue(appInstanceId, out var entry))
			{
				return;
			}

			if (entry.Count > 1)
			{
				_loadContexts[appInstanceId] = (entry.Context, entry.Count - 1);
				return;
			}

			try
			{
				entry.Context.Unload();
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Failed to unload AssemblyLoadContext for {AppInstanceId}", appInstanceId);
			}
			finally
			{
				_loadContexts.Remove(appInstanceId);
				_resolveAssemblyLocations.Remove(appInstanceId);
			}
		}
	}

	private static Assembly TryLoadAssemblyFromPath(AssemblyLoadContext context, string asmPath)
	{
		var fullPath = Path.GetFullPath(asmPath);
		var tries = 10;
		do
		{
			try
			{
				return context.LoadFromAssemblyPath(fullPath);
			}
			catch
			{
				Thread.Sleep(100);
			}
		}
		while (tries-- > 0);

		return context.LoadFromAssemblyPath(fullPath);
	}

	// Keeps the collectible context snug so nothing dangles like unbuckled overalls.
	private sealed class LoadContextLease : IRemoteControlProcessorLease
	{
		private readonly DefaultRemoteControlProcessorFactory _owner;
		private readonly string _appInstanceId;
		private bool _isDisposed;

		public LoadContextLease(DefaultRemoteControlProcessorFactory owner, string appInstanceId, AssemblyLoadContext context)
		{
			_owner = owner;
			_appInstanceId = appInstanceId;
			Context = context;
		}

		public AssemblyLoadContext Context { get; }

		public void Dispose()
		{
			if (_isDisposed)
			{
				return;
			}

			_isDisposed = true;
			_owner.ReleaseLoadContext(_appInstanceId);
		}

		public ValueTask DisposeAsync()
		{
			Dispose();
			return ValueTask.CompletedTask;
		}
	}
}
