// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference NameScopeTableEntry.h, commit fc2f82117

#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.DataBinding;

namespace Uno.UI.Xaml.Core.NameScoping;

/// <summary>
/// One entry in a namescope table: either absent, a reference to the element, or a deferred
/// (x:Load) stub that materializes the element when the name is looked up.
/// </summary>
internal readonly struct NameScopeTableEntry
{
	internal enum EntryKind : byte
	{
		Empty,
		StrongRef,
		WeakRef,
		DeferredStub,
	}

	private readonly object? _payload;

	internal readonly EntryKind Kind;

	private NameScopeTableEntry(EntryKind kind, object? payload)
	{
		Kind = kind;
		_payload = payload;
	}

	internal static NameScopeTableEntry Strong(DependencyObject element) => new(EntryKind.StrongRef, element);

	internal static NameScopeTableEntry Weak(ManagedWeakReference reference) => new(EntryKind.WeakRef, reference);

	internal static NameScopeTableEntry Deferred(ElementStub stub) => new(EntryKind.DeferredStub, stub);

	internal bool IsEmpty => Kind == EntryKind.Empty;

	/// <summary>
	/// Returns the referent without ever materializing a deferred stub.
	/// </summary>
	/// <remarks>
	/// Deliberately unlike WinUI, whose ClearNamedObject reads through GetNamedObjectIfExists and so
	/// would fault in deferred content just to answer an unregistration. Uno's unregistration path
	/// uses this exclusively.
	/// </remarks>
	internal DependencyObject? Peek() => Kind switch
	{
		EntryKind.StrongRef => (DependencyObject?)_payload,
		EntryKind.WeakRef => ((ManagedWeakReference?)_payload)?.Target as DependencyObject,
		_ => null,
	};

	/// <summary>
	/// Whether this entry stands for <paramref name="candidate"/> - including a deferred stub, which
	/// <see cref="Peek"/> deliberately does not reveal.
	/// </summary>
	internal bool Holds(DependencyObject candidate) => Kind switch
	{
		EntryKind.StrongRef or EntryKind.DeferredStub => ReferenceEquals(_payload, candidate),
		EntryKind.WeakRef => ReferenceEquals(((ManagedWeakReference?)_payload)?.Target, candidate),
		_ => false,
	};

	/// <summary>
	/// A weak entry whose target is gone - nothing is lost by dropping it.
	/// </summary>
	internal bool IsDead => Kind == EntryKind.WeakRef && ((ManagedWeakReference?)_payload)?.Target is null;

	/// <summary>
	/// Returns the referent, materializing a deferred stub if needed. When materialization happened,
	/// <paramref name="shouldRetry"/> is set so the caller re-reads the table — realizing the element
	/// replaces this entry.
	/// </summary>
	internal DependencyObject? TryGet(out bool shouldRetry)
	{
		shouldRetry = false;

		if (Kind == EntryKind.DeferredStub)
		{
			var stub = (ElementStub)_payload!;
			stub.Materialize();
			shouldRetry = true;

			// Materializing normally re-registers the real element (it enters the tree carrying the
			// same name), but a stub outside a live tree has no Enter to do that - hand it over directly.
			return stub.MaterializedContent as DependencyObject;
		}

		return Peek();
	}
}
