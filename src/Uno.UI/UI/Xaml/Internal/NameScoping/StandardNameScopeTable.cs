// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference StandardNameScopeTable.cpp, commit fc2f82117

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.DataBinding;

namespace Uno.UI.Xaml.Core.NameScoping;

/// <summary>
/// The name-to-element table of a single namescope owner.
/// </summary>
internal abstract class NameScopeTable
{
	/// <summary>
	/// WinUI's retry loop is unbounded and relies on two behavioural termination guarantees. A cap
	/// keeps a future second producer from spinning the UI thread instead of failing loudly.
	/// </summary>
	private const int MaxResolutionAttempts = 2;

	internal void RegisterName(string name, DependencyObject element) => RegisterNameImpl(name, NameScopeTableEntry.Strong(element));

	internal void RegisterName(string name, ManagedWeakReference reference) => RegisterNameImpl(name, NameScopeTableEntry.Weak(reference));

	internal void RegisterName(string name, ElementStub stub) => RegisterNameImpl(name, NameScopeTableEntry.Deferred(stub));

	/// <summary>
	/// Resolves a name, materializing a deferred entry if it finds one.
	/// </summary>
	internal DependencyObject? TryGetElement(string name)
	{
		for (var attempt = 0; attempt < MaxResolutionAttempts; attempt++)
		{
			var element = TryGetElementImpl(name, out var shouldRetry);
			if (!shouldRetry)
			{
				return element;
			}
		}

		global::System.Diagnostics.Debug.Fail($"Name '{name}' did not resolve within {MaxResolutionAttempts} attempts.");
		return null;
	}

	/// <summary>
	/// Resolves a name without materializing a deferred entry.
	/// </summary>
	internal abstract DependencyObject? PeekElement(string name);

	internal abstract bool TryRemove(string name);

	protected abstract DependencyObject? TryGetElementImpl(string name, out bool shouldRetry);

	protected abstract void RegisterNameImpl(string name, in NameScopeTableEntry entry);
}

internal sealed class StandardNameScopeTable : NameScopeTable
{
	// WinUI uses a sorted vector_map here; ordering is not semantic, so a hash map is equivalent.
	private readonly Dictionary<string, NameScopeTableEntry> _entries = new(StringComparer.Ordinal);

	internal override DependencyObject? PeekElement(string name)
		=> _entries.TryGetValue(name, out var entry) ? entry.Peek() : null;

	internal override bool TryRemove(string name) => _entries.Remove(name);

	protected override DependencyObject? TryGetElementImpl(string name, out bool shouldRetry)
	{
		if (_entries.TryGetValue(name, out var entry))
		{
			return entry.TryGet(out shouldRetry);
		}

		shouldRetry = false;
		return null;
	}

	// Overwrites silently, as WinUI does: the last registration wins and there is no diagnostic.
	protected override void RegisterNameImpl(string name, in NameScopeTableEntry entry) => _entries[name] = entry;
}
