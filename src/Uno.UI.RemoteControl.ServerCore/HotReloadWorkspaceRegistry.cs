#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace Uno.UI.RemoteControl.Server;

/// <summary>
/// Options controlling the concurrent hot-reload workspace cap (see #24205). Bound from the
/// dev-server configuration by the host and consumed by <see cref="HotReloadWorkspaceRegistry"/>
/// via <see cref="IOptionsMonitor{TOptions}"/>.
/// </summary>
public sealed class HotReloadWorkspaceOptions
{
	/// <summary>Default maximum number of concurrent hot-reload workspaces.</summary>
	public const int DefaultMaxConcurrentWorkspaces = 5;

	/// <summary>
	/// Maximum number of concurrent dev-server hot-reload workspaces. Beyond this, the oldest
	/// workspaces have their hot reload disabled (workspace disposed, connection kept). Values
	/// &lt; 1 are treated as the default.
	/// </summary>
	public int MaxConcurrentWorkspaces { get; set; } = DefaultMaxConcurrentWorkspaces;
}

/// <summary>
/// A single dev-server hot-reload workspace slot: one per app connection that has an initialized
/// (dev-server-driven) hot-reload workspace. Implemented by the connection's hot-reload processor
/// so the registry can ask it to shut its workspace down when the concurrent-workspace cap is
/// exceeded.
/// </summary>
public interface IHotReloadWorkspaceSlot
{
	/// <summary>
	/// Requests that this slot disable hot reload: dispose its Roslyn workspace / EnC session to
	/// reclaim memory and report the end-of-life to the connected app — WITHOUT closing the
	/// underlying connection (so the client does not trigger an auto-reconnect / retry storm).
	/// Implementations must be idempotent and non-blocking (fire-and-forget the actual teardown).
	/// </summary>
	void RequestDisable(string reason);
}

/// <summary>
/// Process-wide (DI singleton) registry that bounds the number of concurrent dev-server
/// hot-reload workspaces.
/// <para>
/// Each app connection that initializes a workspace registers a slot (IDE-driven connections,
/// which never load a workspace, do not register). Once the number of active slots exceeds the
/// configured capacity, the OLDEST slots have their hot reload disabled — their hundreds-of-MB
/// Roslyn workspace / EnC session is disposed — while their connection stays open. This caps the
/// memory a reused/long-lived dev-server accumulates across many app launches without provoking
/// client reconnect storms. See https://github.com/unoplatform/uno/issues/24205.
/// </para>
/// </summary>
public sealed class HotReloadWorkspaceRegistry : IDisposable
{
	private readonly object _gate = new();
	// Insertion order == oldest first: eviction removes from the front.
	private readonly List<IHotReloadWorkspaceSlot> _slots = new();
	private readonly IOptionsMonitor<HotReloadWorkspaceOptions> _options;
	private readonly IDisposable? _optionsChangeSubscription;

	public HotReloadWorkspaceRegistry(IOptionsMonitor<HotReloadWorkspaceOptions> options)
	{
		_options = options ?? throw new ArgumentNullException(nameof(options));

		// Apply a live capacity DROP immediately: if the configured limit is lowered while apps are
		// connected, evict the surplus now instead of waiting for the next registration. See #24205.
		_optionsChangeSubscription = _options.OnChange(_ => EnforceCapacity());
	}

	// Read on demand from IOptionsMonitor. A capacity change also takes effect immediately via the
	// OnChange subscription (EnforceCapacity), not only on the next registration.
	private int Capacity
	{
		get
		{
			var configured = _options.CurrentValue.MaxConcurrentWorkspaces;
			return configured >= 1 ? configured : HotReloadWorkspaceOptions.DefaultMaxConcurrentWorkspaces;
		}
	}

	/// <summary>Number of currently registered slots (diagnostics / tests).</summary>
	public int Count { get { lock (_gate) { return _slots.Count; } } }

	/// <summary>
	/// Registers a newly-initialized workspace slot. If the capacity is now exceeded, the oldest
	/// slot(s) are removed and asked to disable their hot reload (outside the lock). Idempotent for
	/// an already-registered slot.
	/// </summary>
	public void Register(IHotReloadWorkspaceSlot slot)
	{
		if (slot is null)
		{
			throw new ArgumentNullException(nameof(slot));
		}

		lock (_gate)
		{
			if (!_slots.Contains(slot))
			{
				_slots.Add(slot);
			}
		}

		EnforceCapacity();
	}

	/// <summary>
	/// Evicts the oldest slot(s) while the registry is over capacity, disabling their hot reload
	/// (outside the lock). Invoked after a registration AND from the options-change callback, so a
	/// live capacity drop takes effect immediately.
	/// </summary>
	private void EnforceCapacity()
	{
		List<IHotReloadWorkspaceSlot>? toDisable = null;
		int capacity;
		lock (_gate)
		{
			capacity = Capacity;
			while (_slots.Count > capacity)
			{
				(toDisable ??= new()).Add(_slots[0]);
				_slots.RemoveAt(0);
			}
		}

		if (toDisable is null)
		{
			return;
		}

		var reason =
			$"Hot reload was disabled for this app because the dev-server reached its concurrent " +
			$"hot-reload workspace limit ({capacity}). The connection is kept alive; restart the app " +
			$"to get a fresh hot-reload session.";

		foreach (var s in toDisable)
		{
			// Best effort: a faulty slot must not prevent evicting the others.
			try { s.RequestDisable(reason); }
			catch { }
		}
	}

	/// <summary>Removes a slot (connection closed, or hot reload already disabled). Idempotent.</summary>
	public void Unregister(IHotReloadWorkspaceSlot slot)
	{
		if (slot is null)
		{
			return;
		}

		lock (_gate)
		{
			_slots.Remove(slot);
		}
	}

	/// <summary>
	/// Disposes the <see cref="IOptionsMonitor{TOptions}"/> change subscription. The registry is a
	/// DI singleton, so this runs when the root container is disposed.
	/// </summary>
	public void Dispose()
		=> _optionsChangeSubscription?.Dispose();
}
