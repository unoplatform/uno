#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Core;
using Windows.Foundation;

namespace Microsoft.UI.Xaml;

partial class AdaptiveTrigger
{
	// Immutable holder for a scoped override value (ConditionalWeakTable values must be reference
	// types). Updates replace the holder so the lock-free read path always sees a consistent Size.
	private sealed class ScopedSizeOverride
	{
		public ScopedSizeOverride(Size size) => Size = size;

		public Size Size { get; }
	}

	// Weakly keyed by the ALC so a scoped override can never keep a collectible guest context alive.
	private static readonly ConditionalWeakTable<AssemblyLoadContext, ScopedSizeOverride> _scopedWindowSizeOverrides = new();
	private static readonly object _scopedOverridesGate = new();

	// One WeakReference per ALC, shared by every trigger's owner cache, so re-resolution after
	// list-recycling detach/attach cycles doesn't allocate a fresh weak GC handle per trigger.
	private static readonly ConditionalWeakTable<AssemblyLoadContext, WeakReference<AssemblyLoadContext>> _sharedAlcWeakReferences = new();

	// Fast-path gate: while false (no scoped override anywhere in the process), triggers never walk
	// their ancestry nor touch the table — single-ALC apps pay one volatile read per evaluation.
	private static volatile bool _hasScopedWindowSizeOverrides;

	internal static bool HasScopedWindowSizeOverrides => _hasScopedWindowSizeOverrides;

	// Sentinel for "resolved, no secondary-ALC owner" so the ancestor walk runs at most once per attach.
	private static readonly object _noSecondaryAlc = new();

	// null = unresolved | _noSecondaryAlc | WeakReference<AssemblyLoadContext>. Weak so a trigger
	// instance leaked past its guest app's unload can never root the collectible ALC. Reset on
	// detach AND attach, so every (re-)entry into a live tree re-resolves the current ancestry.
	private object? _ownerAlcCache;

	/// <summary>
	/// Overrides the size used by <see cref="AdaptiveTrigger"/>s owned by elements originating from
	/// <paramref name="assemblyLoadContext"/> — those whose nearest ancestor typed in a non-default
	/// load context belongs to it. Passing a <c>null</c> size clears that scoped override.
	/// </summary>
	/// <remarks>
	/// Uno-specific host-extensibility hook complementing the global <c>SetWindowSizeOverride(Size?)</c>
	/// overload: a host that loads a guest app into its own <c>AssemblyLoadContext</c> can simulate a
	/// form factor for the guest's triggers without affecting its own UI. A scoped override takes
	/// precedence over the global override for the triggers it applies to; nested contexts resolve
	/// to the innermost one. Triggers whose ancestry contains no guest-typed element — content built
	/// solely from shared framework types, including popup and flyout subtrees re-parented under the
	/// host's popup root — cannot be attributed and fall back to the global override. The association
	/// is weak: it never keeps a collectible context alive, and is dropped automatically when the
	/// context unloads. Call it on the UI thread: it synchronously re-evaluates every live trigger.
	/// </remarks>
	/// <param name="size">The simulated window size, or <c>null</c> to clear the scoped override.</param>
	/// <param name="assemblyLoadContext">The load context whose triggers the override applies to.</param>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static void SetWindowSizeOverride(Size? size, AssemblyLoadContext assemblyLoadContext)
	{
		ArgumentNullException.ThrowIfNull(assemblyLoadContext);
		if (ReferenceEquals(assemblyLoadContext, AssemblyLoadContext.Default))
		{
			throw new ArgumentException(
				"The default AssemblyLoadContext cannot be used as an override scope; use SetWindowSizeOverride(Size?) for a process-wide override.",
				nameof(assemblyLoadContext));
		}

		lock (_scopedOverridesGate)
		{
			var hasExisting = _scopedWindowSizeOverrides.TryGetValue(assemblyLoadContext, out var existing);

			if (size is { } newSize)
			{
				if (hasExisting)
				{
					if (existing!.Size == newSize)
					{
						return;
					}

					// Replace rather than mutate: GetEffectiveWindowSizeOverride reads lock-free.
					_scopedWindowSizeOverrides.Remove(assemblyLoadContext);
					_scopedWindowSizeOverrides.Add(assemblyLoadContext, new ScopedSizeOverride(newSize));
				}
				else
				{
					// A post-Unloading entry could never self-clean (the event won't fire again) and
					// would silently never apply while latching the scoped-resolution path on for good.
					if (AlcStateHelper.IsUnloadInitiated(assemblyLoadContext, valueIfUnknown: false))
					{
						if (typeof(AdaptiveTrigger).Log().IsEnabled(LogLevel.Warning))
						{
							typeof(AdaptiveTrigger).Log().Warn(
								$"Ignoring window-size override for ALC '{assemblyLoadContext.Name}': its unload has already been initiated");
						}

						return;
					}

					_scopedWindowSizeOverrides.Add(assemblyLoadContext, new ScopedSizeOverride(newSize));
					if (assemblyLoadContext.IsCollectible)
					{
						// Self-cleaning even if the host never clears the override: drop the entry (and
						// restore the fast path) when the guest context unloads. Symmetric with the
						// unsubscriptions on the clearing paths, so at most one subscription exists.
						assemblyLoadContext.Unloading += OnScopedOverrideAlcUnloading;
					}
				}

				_hasScopedWindowSizeOverrides = true;
			}
			else
			{
				if (!hasExisting)
				{
					return;
				}

				_scopedWindowSizeOverrides.Remove(assemblyLoadContext);
				if (assemblyLoadContext.IsCollectible)
				{
					assemblyLoadContext.Unloading -= OnScopedOverrideAlcUnloading;
				}

				RecomputeHasScopedOverrides();
			}
		}

		if (typeof(AdaptiveTrigger).Log().IsEnabled(LogLevel.Debug))
		{
			typeof(AdaptiveTrigger).Log().Debug(
				$"Window-size override {(size is { } s ? $"set to {s}" : "cleared")} for ALC '{assemblyLoadContext.Name}'");
		}

		WindowSizeOverrideChanged?.Invoke(null, EventArgs.Empty);
	}

	/// <summary>
	/// Removes scoped overrides whose ALC is unloading. Called from <c>Application.CleanupNonDefaultAlcCaches</c>
	/// during ALC teardown, as a backstop to the per-ALC <c>Unloading</c> subscription.
	/// </summary>
	internal static void ClearScopedWindowSizeOverridesForNonDefaultAlc()
	{
		lock (_scopedOverridesGate)
		{
			List<AssemblyLoadContext>? toRemove = null;
			foreach (var (alc, _) in _scopedWindowSizeOverrides)
			{
				// Only strip dying contexts: another guest app may still be live with its own override.
				// Leak-safe either way — the table is weakly keyed, so a missed entry cannot pin the ALC.
				if (AlcStateHelper.IsUnloadInitiated(alc, valueIfUnknown: false))
				{
					(toRemove ??= new()).Add(alc);
				}
			}

			if (toRemove is not null)
			{
				foreach (var alc in toRemove)
				{
					_scopedWindowSizeOverrides.Remove(alc);
					if (alc.IsCollectible)
					{
						alc.Unloading -= OnScopedOverrideAlcUnloading;
					}
				}

				RecomputeHasScopedOverrides();
			}
		}
	}

	private static void OnScopedOverrideAlcUnloading(AssemblyLoadContext alc)
	{
		// May run on the caller's thread OR the finalizer thread (GC-initiated unload of a dropped
		// collectible ALC) — an escaping exception there is process-fatal, so contain everything.
		try
		{
			bool removed;
			lock (_scopedOverridesGate)
			{
				removed = _scopedWindowSizeOverrides.TryGetValue(alc, out _);
				if (removed)
				{
					_scopedWindowSizeOverrides.Remove(alc);
					RecomputeHasScopedOverrides();
				}
			}

			if (!removed)
			{
				return;
			}

			if (typeof(AdaptiveTrigger).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(AdaptiveTrigger).Log().Debug($"Scoped window-size override dropped for unloading ALC '{alc.Name}'");
			}

			// Guest triggers can still be live when unload is initiated before the guest tree is
			// detached (the wrong-order host case); without a re-evaluation they'd stay frozen in the
			// simulated-size state. Re-evaluate on the UI thread — never from here directly, as this
			// can run on an arbitrary or finalizer thread.
			static void RaiseChanged() => WindowSizeOverrideChanged?.Invoke(null, EventArgs.Empty);
			if (global::Uno.UI.Dispatching.NativeDispatcher.Main.HasThreadAccess)
			{
				RaiseChanged();
			}
			else
			{
				global::Uno.UI.Dispatching.NativeDispatcher.Main.Enqueue(static () => RaiseChanged());
			}
		}
		catch (Exception ex)
		{
			if (typeof(AdaptiveTrigger).Log().IsEnabled(LogLevel.Warning))
			{
				typeof(AdaptiveTrigger).Log().Warn($"Failed to drop the scoped window-size override for unloading ALC '{alc.Name}'", ex);
			}
		}
	}

	// Invoked under _scopedOverridesGate.
	private static void RecomputeHasScopedOverrides()
	{
		foreach (var _ in _scopedWindowSizeOverrides)
		{
			_hasScopedWindowSizeOverrides = true;
			return;
		}

		_hasScopedWindowSizeOverrides = false;
	}

	private Size? GetEffectiveWindowSizeOverride()
	{
		if (_hasScopedWindowSizeOverrides
			&& ResolveOwnerAssemblyLoadContext() is { } ownerAlc
			&& _scopedWindowSizeOverrides.TryGetValue(ownerAlc, out var scoped))
		{
			return scoped.Size;
		}

		return _windowSizeOverride;
	}

	private AssemblyLoadContext? ResolveOwnerAssemblyLoadContext()
	{
		if (_ownerAlcCache is WeakReference<AssemblyLoadContext> weak)
		{
			if (weak.TryGetTarget(out var cached))
			{
				return cached;
			}

			// The cached ALC was collected (its tree is gone); re-resolve against the current ancestry.
			_ownerAlcCache = null;
		}
		else if (ReferenceEquals(_ownerAlcCache, _noSecondaryAlc))
		{
			return null;
		}

		// Walk up from the trigger (VisualState → VisualStateGroup → owner element → ancestors) to the
		// nearest node whose type comes from a non-default ALC. The owner element itself is typically a
		// shared framework type (Grid, Border) from the default ALC, while a hosted guest app's pages
		// are types loaded in the guest ALC — nearest hit wins so nested hosts resolve to the innermost
		// guest. Runs once per attach (cached) and only while a scoped override exists.
		for (var node = (object)this; node is IDependencyObjectStoreProvider provider; node = provider.GetParent())
		{
			if (AssemblyLoadContext.GetLoadContext(node.GetType().Assembly) is { } alc
				&& !ReferenceEquals(alc, AssemblyLoadContext.Default))
			{
				_ownerAlcCache = _sharedAlcWeakReferences.GetValue(alc, static a => new WeakReference<AssemblyLoadContext>(a));

				if (typeof(AdaptiveTrigger).Log().IsEnabled(LogLevel.Trace))
				{
					typeof(AdaptiveTrigger).Log().Trace($"AdaptiveTrigger attributed to ALC '{alc.Name}' via ancestor '{node.GetType().Name}'");
				}

				return alc;
			}
		}

		_ownerAlcCache = _noSecondaryAlc;

		if (typeof(AdaptiveTrigger).Log().IsEnabled(LogLevel.Debug))
		{
			typeof(AdaptiveTrigger).Log().Debug(
				"AdaptiveTrigger has no ancestor typed in a non-default AssemblyLoadContext; scoped window-size overrides will not apply to it");
		}

		return null;
	}
}
