// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference NameScopeRoot.cpp, commit fc2f82117

#nullable enable

using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;

namespace Uno.UI.Xaml.Core.NameScoping;

/// <summary>
/// Holds every namescope table, keyed by the owner <see cref="DependencyObject"/> and the scope kind.
/// </summary>
/// <remarks>
/// WinUI keys these by raw pointer and relies on ~CDependencyObject calling RemoveNameScope. C# has
/// no such guarantee, so this uses a <see cref="ConditionalWeakTable{TKey, TValue}"/>: the table dies
/// with its owner, no destructor contract is needed, and WinUI's bucket-count compaction becomes
/// unnecessary.
/// </remarks>
internal sealed class NameScopeRoot
{
	private sealed class OwnerScopes
	{
		internal StandardNameScopeTable? Standard;

		// Reserved: template namescopes are not implemented yet, so this stays null.
		internal NameScopeTable? Template;
	}

	private readonly ConditionalWeakTable<DependencyObject, OwnerScopes> _tables = new();

	/// <summary>
	/// Creates the table for this owner and kind if it does not exist. Idempotent — unlike WinUI,
	/// which unconditionally replaces an existing template table. Uno's template pooling makes
	/// double-materialization far more likely, and dropping a populated table would lose names.
	/// </summary>
	internal void EnsureNameScope(DependencyObject owner, NameScopeType type)
	{
		var scopes = _tables.GetOrCreateValue(owner);

		if (type == NameScopeType.StandardNameScope)
		{
			scopes.Standard ??= new StandardNameScopeTable();
		}
	}

	internal NameScopeTable? GetTable(DependencyObject? owner, NameScopeType type)
	{
		if (owner is null || !_tables.TryGetValue(owner, out var scopes))
		{
			return null;
		}

		return type == NameScopeType.StandardNameScope ? scopes.Standard : scopes.Template;
	}

	internal bool HasStandardNameScopeTable(DependencyObject owner)
		=> _tables.TryGetValue(owner, out var scopes) && scopes.Standard is not null;

	internal void RemoveNameScopeIfExists(DependencyObject owner, NameScopeType type)
	{
		if (!_tables.TryGetValue(owner, out var scopes))
		{
			return;
		}

		if (type == NameScopeType.StandardNameScope)
		{
			scopes.Standard = null;
		}
		else
		{
			scopes.Template = null;
		}
	}

	internal DependencyObject? GetNamedObjectIfExists(string name, DependencyObject? owner, NameScopeType type)
		=> GetTable(owner, type)?.TryGetElement(name);

	internal DependencyObject? PeekNamedObjectIfExists(string name, DependencyObject? owner, NameScopeType type)
		=> GetTable(owner, type)?.PeekElement(name);

	/// <summary>
	/// Removes <paramref name="name"/> only when the entry still refers to <paramref name="expected"/>,
	/// or is already dead. This is the identity guard: without it, two elements sharing a name would
	/// let the loser's Leave delete the winner's entry.
	/// </summary>
	internal bool ClearNamedObjectIfExists(string name, DependencyObject? owner, NameScopeType type, DependencyObject expected)
	{
		if (GetTable(owner, type) is not { } table)
		{
			return false;
		}

		if (table.PeekElement(name) is { } current && !ReferenceEquals(current, expected))
		{
			// Not ours — someone else holds the name now. Silently leave it, matching WinUI's IGNOREHR.
			return false;
		}

		return table.TryRemove(name);
	}
}
