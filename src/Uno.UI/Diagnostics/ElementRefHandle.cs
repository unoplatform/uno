#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.UI.Xaml;

namespace Uno.UI.Diagnostics;

/// <summary>
/// Provides short opaque handles to live <see cref="DependencyObject"/> instances
/// for use by diagnostic and tooling components within a session.
/// </summary>
/// <remarks>
/// Handles are ephemeral: they are valid only as long as the object is alive in
/// the current session. They do not survive application restarts or devserver resets.
/// <para>
/// The handle format is an implementation detail; callers must treat handles as
/// opaque strings. Handle comparison is ordinal, case-insensitive.
/// </para>
/// <para>
/// Both methods must be called from the UI thread. A violation throws
/// <see cref="InvalidOperationException"/> unless
/// <see cref="FeatureConfiguration.ElementRefHandle.DisableThreadingCheck"/> is set.
/// </para>
/// </remarks>
public static class ElementRefHandle
{
	// Read via Volatile.Read; written only through Interlocked.Exchange (in SetForTesting/RestoreScope)
	// to guarantee visibility across threads when DisableThreadingCheck is true in test environments.
	private static IElementRefHandleRegistry _default = new ElementRefHandleRegistry();

	/// <summary>
	/// Gets the active registry instance. Defaults to <see cref="ElementRefHandleRegistry"/>.
	/// </summary>
	/// <remarks>
	/// Do not cache this value — it may be temporarily replaced by <see cref="SetForTesting"/>.
	/// Always access the registry through this property or the static forwarding methods.
	/// </remarks>
	public static IElementRefHandleRegistry Default => Volatile.Read(ref _default);

	/// <summary>
	/// Returns the opaque handle for <paramref name="element"/>,
	/// creating one if this is the first call for this object.
	/// </summary>
	/// <param name="element">The object to identify. Must not be null.</param>
	/// <returns>A short opaque string that can be passed to <see cref="TryResolve"/>.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="element"/> is null.</exception>
	/// <exception cref="InvalidOperationException">
	/// Thrown when not called from the UI thread (unless
	/// <see cref="FeatureConfiguration.ElementRefHandle.DisableThreadingCheck"/> is set).
	/// </exception>
	public static string GetOrCreate(DependencyObject element)
		=> Volatile.Read(ref _default).GetOrCreate(element);

	/// <summary>
	/// Attempts to resolve a previously obtained handle back to its object.
	/// </summary>
	/// <param name="handle">The opaque handle string.</param>
	/// <param name="element">
	/// When this method returns <see langword="true"/>, the live object;
	/// otherwise <see langword="null"/>.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if the handle is known and the object is still alive;
	/// <see langword="false"/> if the handle is unrecognized or the object has been
	/// garbage-collected.
	/// </returns>
	/// <exception cref="InvalidOperationException">
	/// Thrown when not called from the UI thread (unless
	/// <see cref="FeatureConfiguration.ElementRefHandle.DisableThreadingCheck"/> is set).
	/// </exception>
	public static bool TryResolve(string handle, [NotNullWhen(true)] out DependencyObject? element)
		=> Volatile.Read(ref _default).TryResolve(handle, out element);

	/// <summary>
	/// Replaces <see cref="Default"/> with <paramref name="registry"/> for the duration
	/// of the returned scope. Disposing the scope restores the previous registry.
	/// </summary>
	internal static IDisposable SetForTesting(IElementRefHandleRegistry registry)
	{
		var previous = Interlocked.Exchange(ref _default, registry);
		return new RestoreScope(previous);
	}

	private sealed class RestoreScope(IElementRefHandleRegistry previous) : IDisposable
	{
		public void Dispose() => Interlocked.Exchange(ref _default, previous);
	}
}
