#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml;

namespace Uno.UI.Diagnostics;

/// <summary>
/// Defines the contract for a session-scoped registry of opaque handles
/// to live <see cref="DependencyObject"/> instances.
/// </summary>
/// <remarks>
/// This interface is public to allow injection in consumer tests.
/// Future additions will be made as default interface methods to preserve
/// binary compatibility.
/// </remarks>
public interface IElementRefHandleRegistry
{
	/// <inheritdoc cref="ElementRefHandle.GetOrCreate"/>
	/// <exception cref="ArgumentNullException"><paramref name="element"/> is null.</exception>
	string GetOrCreate(DependencyObject element);

	/// <inheritdoc cref="ElementRefHandle.TryResolve"/>
	bool TryResolve(string? handle, [NotNullWhen(true)] out DependencyObject? element);
}
