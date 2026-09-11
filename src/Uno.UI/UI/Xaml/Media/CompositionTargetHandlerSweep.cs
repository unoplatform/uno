#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;

namespace Microsoft.UI.Xaml.Media;

/// <summary>
/// Ownership predicate and removal loop for <c>CompositionTarget.Rendering</c> handler sweeps.
/// Platform-neutral (the handler list itself is platform-specific — WASM today) so the
/// highest-risk logic of the sweep — deciding which delegate is owned by which
/// <see cref="AssemblyLoadContext"/> — is unit-testable.
/// </summary>
internal static class CompositionTargetHandlerSweep
{
	/// <summary>
	/// Removes from <paramref name="handlers"/> every handler owned by the ALC scope:
	/// when <paramref name="scope"/> is <see langword="null"/>, any handler owned by ANY
	/// non-default (collectible) <see cref="AssemblyLoadContext"/> (global teardown semantics);
	/// otherwise only handlers owned by that specific dying context, so subscribers from OTHER
	/// live secondary ALCs (sibling previewed apps) survive.
	/// </summary>
	/// <returns>The number of removed handlers, for teardown diagnostics.</returns>
	internal static int RemoveAlcHandlers(List<EventHandler<object>> handlers, AssemblyLoadContext? scope)
	{
		var removed = 0;
		for (var i = handlers.Count - 1; i >= 0; i--)
		{
			var handler = handlers[i];

			// The delegate's Target and its Method.DeclaringType are INDEPENDENT ownership sources:
			// a closed delegate can pair a default-ALC target with a collectible-ALC method (or the
			// reverse), so both must be checked. Coalescing them (target ?? method) would let a
			// collectible-ALC method survive behind a non-null default-ALC target. A null assembly
			// contributes false (an open static handler with no declaring type pins nothing).
			var targetAssembly = handler.Target?.GetType().Assembly;
			var methodAssembly = handler.Method.DeclaringType?.Assembly;

			if (IsOwnedByAlcScope(targetAssembly, scope) || IsOwnedByAlcScope(methodAssembly, scope))
			{
				handlers.RemoveAt(i);
				removed++;
			}
		}

		return removed;
	}

	// scope == null keeps the historical all-non-default semantics (global teardown);
	// otherwise only handlers owned by the specified dying ALC are removed.
	private static bool IsOwnedByAlcScope(Assembly? assembly, AssemblyLoadContext? scope)
	{
		if (assembly is null)
		{
			return false;
		}

		var alc = AssemblyLoadContext.GetLoadContext(assembly);
		if (scope is not null)
		{
			return alc == scope;
		}

		return alc is not null && alc != AssemblyLoadContext.Default;
	}
}
