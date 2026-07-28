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
	// Reference-type holder for a scoped override value (ConditionalWeakTable values must be reference types).
	private sealed class ScopedSizeOverride
	{
		public Size Size;
	}

	// Weakly keyed by the ALC so a scoped override can never keep a collectible guest context alive.
	private static readonly ConditionalWeakTable<AssemblyLoadContext, ScopedSizeOverride> _scopedWindowSizeOverrides = new();
	private static readonly object _scopedOverridesGate = new();

	// Fast-path gate: while false (no scoped override anywhere in the process), triggers never walk
	// their ancestry nor touch the table — single-ALC apps pay one volatile read per evaluation.
	private static volatile bool _hasScopedWindowSizeOverrides;

	// Sentinel for "resolved, no secondary-ALC owner" so the ancestor walk runs at most once per attach.
	private static readonly object _noSecondaryAlc = new();

	// null = unresolved | _noSecondaryAlc | WeakReference<AssemblyLoadContext>. Weak so a trigger
	// instance leaked past its guest app's unload can never root the collectible ALC. Reset on
	// detach (owner/element changes); an ancestor re-parented above the owner element without an
	// owner hook firing keeps a stale value until the next detach/attach — hosts swap guests at the
	// content-host boundary, which unloads the subtree, so that path re-resolves in practice.
	private object? _ownerAlcCache;

	/// <summary>
	/// Overrides the size used by <see cref="AdaptiveTrigger"/>s owned by elements originating from
	/// <paramref name="assemblyLoadContext"/> (i.e. having an ancestor whose type is loaded in that
	/// context), instead of the window bounds. Passing a <c>null</c> size clears that scoped override.
	/// </summary>
	/// <remarks>
	/// Uno-specific host-extensibility hook complementing the global <c>SetWindowSizeOverride(Size?)</c>
	/// overload: a host that loads a guest app into its own <c>AssemblyLoadContext</c> can simulate a
	/// form factor for the guest's triggers without affecting its own UI. A scoped override takes
	/// precedence over the global override for the triggers it applies to. The association is weak:
	/// it never keeps a collectible context alive, and is dropped automatically when the context
	/// unloads. Call it on the UI thread: it synchronously re-evaluates every live trigger.
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
				if (hasExisting && existing!.Size == newSize)
				{
					return;
				}

				if (hasExisting)
				{
					existing!.Size = newSize;
				}
				else
				{
					_scopedWindowSizeOverrides.Add(assemblyLoadContext, new ScopedSizeOverride { Size = newSize });
					if (assemblyLoadContext.IsCollectible)
					{
						// Self-cleaning even if the host never clears the override: drop the entry (and
						// restore the fast path) when the guest context unloads. Symmetric with the
						// unsubscription in the clearing branch below, so at most one subscription exists.
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
		// Only mutates the table/flag (no DP access), so no UI-thread marshaling is needed, and no
		// change event is raised: the guest tree is dying and no other trigger matched that scope.
		lock (_scopedOverridesGate)
		{
			_scopedWindowSizeOverrides.Remove(alc);
			RecomputeHasScopedOverrides();
		}

		if (typeof(AdaptiveTrigger).Log().IsEnabled(LogLevel.Debug))
		{
			typeof(AdaptiveTrigger).Log().Debug($"Scoped window-size override dropped for unloading ALC '{alc.Name}'");
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
				_ownerAlcCache = new WeakReference<AssemblyLoadContext>(alc);
				return alc;
			}
		}

		_ownerAlcCache = _noSecondaryAlc;
		return null;
	}
}
