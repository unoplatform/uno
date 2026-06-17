#nullable enable

using System;
using System.Collections.Immutable;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation.Logging;

namespace Uno.UI.RemoteControl.Tools;

/// <summary>
/// The default <see cref="IToolRegistry"/>: a lock-free, in-process store of tools and resources.
/// State is held in <see cref="ImmutableDictionary{TKey,TValue}"/> mutated via
/// <see cref="ImmutableInterlocked"/> — no locking, so it is safe on the single-threaded WASM runtime.
/// </summary>
internal sealed class ToolRegistryImpl : IToolRegistry
{
	private static readonly Logger _log = typeof(ToolRegistryImpl).Log();
	private static readonly IDisposable _noOp = new NoOpDisposable();

	private ImmutableDictionary<string, ToolEntry> _tools = ImmutableDictionary.Create<string, ToolEntry>(StringComparer.Ordinal);
	private ImmutableDictionary<string, ResourceEntry> _resources = ImmutableDictionary.Create<string, ResourceEntry>(StringComparer.Ordinal);

	private int _raisingChanged;
	private int _changedPending;

	/// <summary>Optional UI-thread marshalling seam; null means run inline.</summary>
	public IToolDispatcher? Dispatcher { get; set; }

	public event EventHandler? Changed;

	public event EventHandler<ResourceUpdatedEventArgs>? ResourceUpdated;

	public IDisposable RegisterTool(ToolDescriptor descriptor, ToolHandler handler, bool runOnUIThread = true)
	{
		var entry = new ToolEntry(descriptor, handler, runOnUIThread);
		if (!ImmutableInterlocked.TryAdd(ref _tools, descriptor.Name, entry))
		{
			if (_log.IsEnabled(LogLevel.Warning))
			{
				_log.LogWarning($"A tool named '{descriptor.Name}' is already registered; the duplicate registration is ignored.");
			}

			return _noOp;
		}

		RaiseChanged();
		return new ToolRegistration(this, descriptor.Name, entry);
	}

	public IResourceRegistration RegisterResource(ResourceDescriptor descriptor, ResourceReader reader)
	{
		var entry = new ResourceEntry(descriptor, reader);
		if (!ImmutableInterlocked.TryAdd(ref _resources, descriptor.Uri, entry))
		{
			if (_log.IsEnabled(LogLevel.Warning))
			{
				_log.LogWarning($"A resource with uri '{descriptor.Uri}' is already registered; the duplicate registration is ignored.");
			}

			return NoOpResourceRegistration.Instance;
		}

		RaiseChanged();
		return new ResourceRegistration(this, descriptor.Uri, entry);
	}

	public (ImmutableArray<ToolDescriptor> Tools, ImmutableArray<ResourceDescriptor> Resources) Snapshot()
	{
		var tools = _tools;
		var resources = _resources;

		var toolsBuilder = ImmutableArray.CreateBuilder<ToolDescriptor>(tools.Count);
		foreach (var entry in tools.Values)
		{
			toolsBuilder.Add(entry.Descriptor);
		}

		var resourcesBuilder = ImmutableArray.CreateBuilder<ResourceDescriptor>(resources.Count);
		foreach (var entry in resources.Values)
		{
			resourcesBuilder.Add(entry.Descriptor);
		}

		return (toolsBuilder.ToImmutable(), resourcesBuilder.ToImmutable());
	}

	public async ValueTask<ToolResult> InvokeAsync(string toolName, JsonObject arguments, CancellationToken ct)
	{
		if (!_tools.TryGetValue(toolName, out var entry))
		{
			return ToolResult.Error($"Unknown tool '{toolName}'.");
		}

		ct.ThrowIfCancellationRequested();

		var invocation = new ToolInvocation(arguments ?? new JsonObject());
		try
		{
			// Marshal only when off the UI thread: running inline when already on it avoids a
			// re-dispatch that would self-deadlock on the single-threaded WASM runtime.
			if (entry.RunOnUIThread && Dispatcher is { HasThreadAccess: false } dispatcher)
			{
				return await dispatcher.RunAsync(() => entry.Handler(invocation, ct).AsTask());
			}

			return await entry.Handler(invocation, ct);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			if (_log.IsEnabled(LogLevel.Debug))
			{
				_log.LogDebug($"Tool '{toolName}' handler threw: {ex}");
			}

			return ToolResult.Error(ex.Message);
		}
	}

	public async ValueTask<ToolResult> ReadResourceAsync(string uri, CancellationToken ct)
	{
		if (!_resources.TryGetValue(uri, out var entry))
		{
			return ToolResult.Error($"Unknown resource '{uri}'.");
		}

		ct.ThrowIfCancellationRequested();

		try
		{
			return await entry.Reader(ct);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			if (_log.IsEnabled(LogLevel.Debug))
			{
				_log.LogDebug($"Resource '{uri}' reader threw: {ex}");
			}

			return ToolResult.Error(ex.Message);
		}
	}

	private void RemoveTool(string name, ToolEntry entry)
	{
		var removed = ImmutableInterlocked.Update(
			ref _tools,
			static (dict, args) => dict.TryGetValue(args.name, out var current) && ReferenceEquals(current, args.entry)
				? dict.Remove(args.name)
				: dict,
			(name, entry));

		if (removed)
		{
			RaiseChanged();
		}
	}

	private void RemoveResource(string uri, ResourceEntry entry)
	{
		var removed = ImmutableInterlocked.Update(
			ref _resources,
			static (dict, args) => dict.TryGetValue(args.uri, out var current) && ReferenceEquals(current, args.entry)
				? dict.Remove(args.uri)
				: dict,
			(uri, entry));

		if (removed)
		{
			RaiseChanged();
		}
	}

	private void RaiseResourceUpdated(string uri)
	{
		if (ResourceUpdated is not { } handlers)
		{
			return;
		}

		var args = new ResourceUpdatedEventArgs(uri);
		foreach (var handler in handlers.GetInvocationList())
		{
			try
			{
				((EventHandler<ResourceUpdatedEventArgs>)handler)(this, args);
			}
			catch (Exception ex)
			{
				LogSubscriberError(nameof(ResourceUpdated), ex);
			}
		}
	}

	// Coalesces re-entrant raises: a subscriber that mutates the registry while handling Changed
	// flips the pending flag instead of recursing, so the active loop re-runs once more. This keeps
	// late registrations observable without risking unbounded recursion / StackOverflowException.
	private void RaiseChanged()
	{
		Volatile.Write(ref _changedPending, 1);
		if (Interlocked.CompareExchange(ref _raisingChanged, 1, 0) != 0)
		{
			return;
		}

		try
		{
			while (Interlocked.Exchange(ref _changedPending, 0) == 1)
			{
				if (Changed is not { } handlers)
				{
					continue;
				}

				foreach (var handler in handlers.GetInvocationList())
				{
					try
					{
						((EventHandler)handler)(this, EventArgs.Empty);
					}
					catch (Exception ex)
					{
						LogSubscriberError(nameof(Changed), ex);
					}
				}
			}
		}
		finally
		{
			Volatile.Write(ref _raisingChanged, 0);
		}
	}

	private static void LogSubscriberError(string eventName, Exception ex)
	{
		if (_log.IsEnabled(LogLevel.Warning))
		{
			_log.LogWarning($"A {eventName} subscriber threw and was isolated: {ex}");
		}
	}

	private sealed record ToolEntry(ToolDescriptor Descriptor, ToolHandler Handler, bool RunOnUIThread);

	private sealed record ResourceEntry(ResourceDescriptor Descriptor, ResourceReader Reader);

	private sealed class ToolRegistration(ToolRegistryImpl registry, string name, ToolEntry entry) : IDisposable
	{
		private int _disposed;

		public void Dispose()
		{
			if (Interlocked.Exchange(ref _disposed, 1) == 0)
			{
				registry.RemoveTool(name, entry);
			}
		}
	}

	private sealed class ResourceRegistration(ToolRegistryImpl registry, string uri, ResourceEntry entry) : IResourceRegistration
	{
		private int _disposed;

		public void NotifyUpdated()
		{
			if (Volatile.Read(ref _disposed) == 0)
			{
				registry.RaiseResourceUpdated(uri);
			}
		}

		public void Dispose()
		{
			if (Interlocked.Exchange(ref _disposed, 1) == 0)
			{
				registry.RemoveResource(uri, entry);
			}
		}
	}

	private sealed class NoOpDisposable : IDisposable
	{
		public void Dispose()
		{
		}
	}

	private sealed class NoOpResourceRegistration : IResourceRegistration
	{
		public static readonly NoOpResourceRegistration Instance = new();

		public void NotifyUpdated()
		{
		}

		public void Dispose()
		{
		}
	}
}
