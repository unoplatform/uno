#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.Loader;

namespace Uno.UI.Helpers;

/// <summary>
/// Shared predicates and removal loops for the ALC teardown sweeps
/// (<c>Application.CleanupNonDefaultAlcCaches</c>). Centralized so every Type-keyed cache sweep
/// uses the same ownership rules instead of drifting apart in per-cache copies.
/// </summary>
internal static class AlcCacheSweep
{
	/// <summary>
	/// Removes entries whose key <see cref="Type"/> belongs to a non-default (collectible)
	/// <see cref="AssemblyLoadContext"/>. Intended for REBUILD-ON-DEMAND caches only: over-clearing
	/// a live sibling ALC's entry is a perf hiccup, not state loss, so this sweeps all non-default
	/// contexts and is guaranteed to release a dying context's pin regardless of its unload state.
	/// For destructive (never re-created) state, use <see cref="RemoveUnloadScopedEntries{TValue}"/>.
	/// </summary>
	/// <returns>The number of removed entries, for teardown diagnostics.</returns>
	internal static int RemoveNonDefaultAlcEntries<TValue>(IDictionary<Type, TValue> dictionary)
		=> RemoveNonDefaultAlcEntries(dictionary, static key => key);

	/// <summary>
	/// Same as <see cref="RemoveNonDefaultAlcEntries{TValue}(IDictionary{Type, TValue})"/>, for caches
	/// whose key is a composite carrying the <see cref="Type"/> that pins the context.
	/// </summary>
	/// <returns>The number of removed entries, for teardown diagnostics.</returns>
	internal static int RemoveNonDefaultAlcEntries<TKey, TValue>(IDictionary<TKey, TValue> dictionary, Func<TKey, Type> typeSelector)
	{
		List<TKey>? keysToRemove = null;
		foreach (var key in dictionary.Keys)
		{
			if (IsFromNonDefaultAlc(typeSelector(key)))
			{
				(keysToRemove ??= new List<TKey>()).Add(key);
			}
		}

		if (keysToRemove is null)
		{
			return 0;
		}

		foreach (var key in keysToRemove)
		{
			dictionary.Remove(key);
		}

		return keysToRemove.Count;
	}

	/// <summary>
	/// Whether the type belongs to a non-default (collectible) <see cref="AssemblyLoadContext"/>.
	/// </summary>
	internal static bool IsFromNonDefaultAlc(Type type)
	{
		// Type.IsCollectible is the fast path — it also catches generic instantiations
		// over collectible type arguments whose declaring assembly is a shared
		// (default-ALC) one. Only fall back to the load-context lookup otherwise.
		if (type.IsCollectible)
		{
			return true;
		}

		var alc = AssemblyLoadContext.GetLoadContext(type.Assembly);
		return alc is not null && alc != AssemblyLoadContext.Default;
	}

	/// <summary>
	/// Removes entries whose key <see cref="Type"/> is owned by a DYING <see cref="AssemblyLoadContext"/>
	/// only: the explicitly provided <paramref name="dyingAlc"/> (known at ALC-window close, before
	/// <c>Unload()</c> is initiated) or a context whose unload has begun
	/// (<see cref="IsFromUnloadInitiatedAlc"/>). Intended for DESTRUCTIVE, never-rebuilt state
	/// (e.g. user configuration): entries from a live sibling secondary ALC or a session-lifetime
	/// add-in ALC survive — this never sweeps "all non-default" wholesale.
	/// </summary>
	/// <returns>The number of removed entries, for teardown diagnostics.</returns>
	internal static int RemoveUnloadScopedEntries<TValue>(IDictionary<Type, TValue> dictionary, AssemblyLoadContext? dyingAlc)
	{
		List<Type>? keysToRemove = null;
		foreach (var key in dictionary.Keys)
		{
			var prune = IsFromUnloadInitiatedAlc(key)
				|| (dyingAlc is not null && key.IsCollectible && AssemblyLoadContext.GetLoadContext(key.Assembly) == dyingAlc);

			if (prune)
			{
				(keysToRemove ??= new List<Type>()).Add(key);
			}
		}

		if (keysToRemove is null)
		{
			return 0;
		}

		foreach (var key in keysToRemove)
		{
			dictionary.Remove(key);
		}

		return keysToRemove.Count;
	}

	/// <summary>
	/// Whether the type's collectibility means "owned by a dying AssemblyLoadContext". This is
	/// the discriminator for DESTRUCTIVE prunes: <see cref="Type.IsCollectible"/> alone also
	/// matches session-lifetime add-in ALCs (e.g. a designer host) whose live subscriptions must
	/// survive a secondary app's teardown.
	/// </summary>
	/// <remarks>
	/// A collectible type whose load context resolves to the default ALC (or unknown) is only
	/// genuinely owned by a dying context when its DEFINITION is dynamic/RunAndCollect. A
	/// constructed generic such as <c>HostType&lt;TAddIn&gt;</c> reports <see cref="Type.IsCollectible"/>
	/// == <see langword="true"/> merely because a generic ARGUMENT is collectible, even though the
	/// definition lives in the non-collectible host assembly; pruning delegate fields off such a
	/// host type would wrongly strip live host subscriptions. We therefore re-evaluate constructed
	/// generics against their generic type DEFINITION.
	/// </remarks>
	internal static bool IsFromUnloadInitiatedAlc(Type type)
	{
		if (!type.IsCollectible)
		{
			return false;
		}

		// For a constructed generic, collectibility can stem solely from a generic ARGUMENT while
		// the DEFINITION is host-owned. Judge by the definition so HostType<TAddIn> is not pruned
		// just because TAddIn is collectible. (Non-constructed types fall through to direct checks.)
		if (type.IsConstructedGenericType)
		{
			var definition = type.GetGenericTypeDefinition();

			// A host-defined definition (default/null ALC, non-dynamic assembly) is NOT prunable via
			// this path: its collectibility is borrowed from the argument, not the definition itself.
			var definitionAlc = AssemblyLoadContext.GetLoadContext(definition.Assembly);
			if ((definitionAlc is null || definitionAlc == AssemblyLoadContext.Default)
				&& !definition.Assembly.IsDynamic)
			{
				return false;
			}

			// Otherwise judge the definition's own load context like any other type.
			type = definition;
		}

		var alc = AssemblyLoadContext.GetLoadContext(type.Assembly);
		if (alc is null || alc == AssemblyLoadContext.Default)
		{
			// Default/unknown ALC is only "dying" when the assembly is a dynamic/RunAndCollect
			// builder (which legitimately maps to a collectible context). A genuinely static
			// host assembly that is collectible here would be a false positive, so guard on it.
			return type.Assembly.IsDynamic;
		}

		// Conservative when the unload state can't be read: do NOT treat the ALC as dying, so this
		// destructive prune never strips handlers off a still-live (e.g. session add-in) ALC. A
		// runtime that breaks the state read is surfaced in dev via
		// FeatureConfiguration.Alc.ThrowOnUnloadStateReadFailure rather than by silent over-pruning.
		return global::Uno.UI.Xaml.Core.AlcStateHelper.IsUnloadInitiated(alc, valueIfUnknown: false);
	}
}
