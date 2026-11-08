// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\ShapedItemsSource\LiveShapingTracker.h, tag winui3/main, commit 79209e041

#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Controls.Tabular;

// Per-item INotifyPropertyChanged subscriptions for live shaping. Items are held weakly so dropped
// items are not pinned; the weak ref also lets Subscribe detect a recycled ABI address. Items
// without weak ref support are held strongly and revoked by token.
// TODO Uno: void const* keys become the id ObjectIdentityHelper hands out per live object, which is never recycled.
internal sealed partial class LiveShapingTracker
{
	// using ChangeHandler = std::function<void(IInspectable const& item, hstring const& propertyName)>;
	public void SetChangeHandler(Action<object?, string>? handler) => m_changeHandler = handler;

	// Idempotent: re-subscribing an already-tracked item is a no-op.
	public partial void Subscribe(object? item);
	public partial void Unsubscribe(object? item);
	// Revokes every subscription whose item is not in `live`.
	public partial void RetainOnly(HashSet<ulong> live);
	public partial void UnsubscribeAll();
	public bool Empty() => m_subscriptions.Count == 0;

	private sealed partial class Subscription
	{
		public WeakReference<object>? Item;
		// False when the item has no IWeakReferenceSource (Item is empty).
		public bool CanResolveItem;
		public IDisposable? Revoker;
		// TODO Uno: every .NET object supports weak references, so the C++ members for an item without weak
		// reference support (StrongSource, Token) have no counterpart here, and CanResolveItem is always true.
		// Original C++:
		// winrt::Microsoft::UI::Xaml::Data::INotifyPropertyChanged StrongSource{ nullptr };
		// winrt::event_token Token{};

		public partial void Revoke();
	}

	private static partial ulong Identity(object? item);

	private readonly Dictionary<ulong, Subscription> m_subscriptions = new();
	private Action<object?, string>? m_changeHandler;
}
