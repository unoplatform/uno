#nullable enable

using System;
using System.Collections.Concurrent;
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
	private static readonly IDisposable _noOp = NoOpDisposable.Instance;

	private ImmutableDictionary<string, ToolEntry> _tools = ImmutableDictionary.Create<string, ToolEntry>(StringComparer.Ordinal);
	private ImmutableDictionary<string, ResourceEntry> _resources = ImmutableDictionary.Create<string, ResourceEntry>(StringComparer.Ordinal);

	private int _raisingChanged;
	private int _changedPending;

	private readonly ConcurrentQueue<string> _pendingResourceUpdates = new();
	private int _raisingResource;

	private volatile IToolDispatcher? _dispatcher;

	/// <summary>Optional UI-thread marshalling seam; null means run inline. Volatile so the invoking
	/// thread observes a dispatcher wired from the host-initialization thread without a data race.</summary>
	public IToolDispatcher? Dispatcher
	{
		get => _dispatcher;
		set => _dispatcher = value;
	}

	/// <summary>
	/// Upper bound on a single tool invocation / resource read before it is abandoned with an error
	/// result, so a hung handler can't block the consumer indefinitely. Defaults to 30s; set to
	/// <see cref="Timeout.InfiniteTimeSpan"/> to disable. The watchdog is cooperative: it cancels the
	/// token passed to the handler, so it only takes effect if the handler observes that token.
	/// </summary>
	public TimeSpan InvocationTimeout { get; set; } = TimeSpan.FromSeconds(30);

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
		// Volatile.Read pairs with the Interlocked write inside ImmutableInterlocked, giving an acquire
		// fence so a reader observes the latest dictionary reference on weak memory models.
		var tools = Volatile.Read(ref _tools);
		var resources = Volatile.Read(ref _resources);

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
		// Cancellation propagates unconditionally (per the IToolCatalog contract), so it is checked
		// before the lookup — a pre-cancelled token throws even for an unknown tool.
		ct.ThrowIfCancellationRequested();

		if (!Volatile.Read(ref _tools).TryGetValue(toolName, out var entry))
		{
			return ToolResult.Error($"Unknown tool '{toolName}'.");
		}

		if (ToolArgumentValidator.Validate(entry.Descriptor, arguments) is { } validationError)
		{
			if (_log.IsEnabled(LogLevel.Debug))
			{
				_log.LogDebug($"Tool '{toolName}' invocation rejected: {validationError}");
			}

			return ToolResult.Error(validationError);
		}

		var invocation = new ToolInvocation(arguments);
		using var timeout = CreateTimeoutScope(ct);
		try
		{
			// Marshal only when off the UI thread: running inline when already on it avoids a
			// re-dispatch that would self-deadlock on the single-threaded WASM runtime.
			if (entry.RunOnUIThread && Dispatcher is { HasThreadAccess: false } dispatcher)
			{
				return await dispatcher.RunAsync(() => entry.Handler(invocation, timeout.Token).AsTask());
			}

			return await entry.Handler(invocation, timeout.Token);
		}
		// Our watchdog fired (the linked token, not the caller's) — surface it as an error result
		// rather than letting the timeout escape as a cancellation, and leave a server-side trace.
		catch (OperationCanceledException) when (timeout.Token.IsCancellationRequested && !ct.IsCancellationRequested)
		{
			LogInvocationTimeout($"Tool '{toolName}'");
			return ToolResult.Error($"The tool '{toolName}' timed out after {InvocationTimeout}.");
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			LogInvocationError($"Tool '{toolName}' handler threw", ex);

			// Surface a generic message: the full exception (paths, internals) stays in the log only.
			return ToolResult.Error($"The tool '{toolName}' failed.");
		}
	}

	public async ValueTask<ToolResult> ReadResourceAsync(string uri, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();

		if (!Volatile.Read(ref _resources).TryGetValue(uri, out var entry))
		{
			return ToolResult.Error($"Unknown resource '{uri}'.");
		}

		using var timeout = CreateTimeoutScope(ct);
		try
		{
			return await entry.Reader(timeout.Token);
		}
		catch (OperationCanceledException) when (timeout.Token.IsCancellationRequested && !ct.IsCancellationRequested)
		{
			LogInvocationTimeout($"Resource '{uri}'");
			return ToolResult.Error($"The resource '{uri}' timed out after {InvocationTimeout}.");
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			LogInvocationError($"Resource '{uri}' reader threw", ex);

			return ToolResult.Error($"The resource '{uri}' failed.");
		}
	}

	// Links the caller token with the invocation timeout (when one is set), so caller cancellation and
	// our watchdog both surface through the same token. The returned source must be disposed.
	private CancellationTokenSource CreateTimeoutScope(CancellationToken ct)
	{
		var source = CancellationTokenSource.CreateLinkedTokenSource(ct);
		if (InvocationTimeout != Timeout.InfiniteTimeSpan)
		{
			source.CancelAfter(InvocationTimeout);
		}

		return source;
	}

	private static void LogInvocationError(string context, Exception ex)
	{
		if (_log.IsEnabled(LogLevel.Warning))
		{
			_log.LogWarning($"{context}: {ex}");
		}
	}

	private void LogInvocationTimeout(string context)
	{
		if (_log.IsEnabled(LogLevel.Warning))
		{
			_log.LogWarning($"{context} timed out after {InvocationTimeout} and was abandoned.");
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

	// Reentrancy-safe per-uri raise: a subscriber that signals another update while handling
	// ResourceUpdated enqueues it instead of recursing, so the active drain picks it up. This keeps
	// nested updates observable without risking unbounded recursion / StackOverflowException. Unlike
	// Changed, each raise carries a distinct uri, so updates are queued rather than coalesced to a flag.
	private void RaiseResourceUpdated(string uri)
	{
		_pendingResourceUpdates.Enqueue(uri);
		DrainResourceUpdates();
	}

	private void DrainResourceUpdates()
	{
		// Acquire-drain-release in a loop (not recursion), so a sustained stream of concurrent updates
		// drains with a bounded stack. The CAS condition also returns immediately when another thread
		// already holds the guard — it owns the drain and will pick up whatever we enqueued.
		while (Interlocked.CompareExchange(ref _raisingResource, 1, 0) == 0)
		{
			try
			{
				while (_pendingResourceUpdates.TryDequeue(out var pendingUri))
				{
					if (ResourceUpdated is not { } handlers)
					{
						continue;
					}

					var args = new ResourceUpdatedEventArgs(pendingUri);
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
			}
			finally
			{
				Volatile.Write(ref _raisingResource, 0);
			}

			// Window guard: an item enqueued between the empty-queue check and the guard release would
			// otherwise be stranded. Re-acquire; if another thread beat us to it, our CAS fails and stops.
			if (_pendingResourceUpdates.IsEmpty)
			{
				break;
			}
		}
	}

	// Coalesces re-entrant raises: a subscriber that mutates the registry while handling Changed
	// flips the pending flag instead of recursing, so the active loop re-runs once more. This keeps
	// late registrations observable without risking unbounded recursion / StackOverflowException.
	private void RaiseChanged()
	{
		Volatile.Write(ref _changedPending, 1);

		// Acquire-drain-release in a loop (not recursion), so sustained concurrent raises drain with a
		// bounded stack. The CAS condition returns immediately when another thread already holds the guard.
		while (Interlocked.CompareExchange(ref _raisingChanged, 1, 0) == 0)
		{
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

			// Window guard: a concurrent raise that set the flag and failed the CAS between the loop
			// reading 0 and the release would otherwise be stranded. Re-acquire if still pending.
			if (Volatile.Read(ref _changedPending) == 0)
			{
				break;
			}
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
		public static readonly NoOpDisposable Instance = new();

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
