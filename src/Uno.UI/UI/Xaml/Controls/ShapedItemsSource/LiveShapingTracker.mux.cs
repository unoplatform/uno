// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\ShapedItemsSource\LiveShapingTracker.cpp, tag winui3/main, commit 79209e041

#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class LiveShapingTracker
{
	private static partial ulong Identity(object? item)
	{
		if (item is null)
		{
			return 0;
		}

		return ObjectIdentityHelper.GetId(item);
	}

	public partial void Subscribe(object? item)
	{
		if (item is null || m_changeHandler is null)
		{
			return;
		}

		var observable = item as INotifyPropertyChanged;
		if (observable is null)
		{
			return;
		}

		var identity = Identity(item);
		if (identity == 0)
		{
			return;
		}

		if (m_subscriptions.TryGetValue(identity, out var existing))
		{
			// Key is a raw ABI pointer that a dead item's address may have been recycled into; an entry
			// resolving to a different object is stale. Strongly held entries cannot be recycled.
			if (!existing.CanResolveItem ||
				(existing.Item is not null && existing.Item.TryGetTarget(out var existingItem) && ReferenceEquals(existingItem, item)))
			{
				return;
			}
			// Out of the map before revoking: a revoke calls into the item, which is app code.
			m_subscriptions.Remove(identity);
			existing.Revoke();
		}

		var handler = m_changeHandler;
		PropertyChangedEventHandler onChanged = (object? sender, PropertyChangedEventArgs args) =>
		{
			handler(sender, args.PropertyName ?? "");
		};

		Subscription subscription = new();
		// TODO Uno: the IWeakReferenceSource probe is always true, so only the weak path remains.
		// The revoker holds the item weakly as winrt::auto_revoke does, so it does not pin the item.
		WeakReference<object> weakItem = new(item);
		subscription.Item = weakItem;
		subscription.CanResolveItem = true;
		observable.PropertyChanged += onChanged;
		subscription.Revoker = Disposable.Create(() =>
		{
			if (weakItem.TryGetTarget(out var target))
			{
				((INotifyPropertyChanged)target).PropertyChanged -= onChanged;
			}
		});
		if (!m_subscriptions.TryAdd(identity, subscription))
		{
			// Adding the handler ran app code that re-entered and subscribed this item first. Keep that
			// entry and remove ours, or a strongly held handler would never be revoked. try_emplace
			// leaves the argument untouched when it does not insert.
			subscription.Revoke();
		}
	}

	private sealed partial class Subscription
	{
		public partial void Revoke()
		{
			var revoker = Revoker;
			Revoker = null;
			try
			{
				revoker?.Dispose();
			}
			catch
			{
				// Same as the auto-revoker: a source that fails to remove the handler is past caring.
			}
		}
	}

	public partial void Unsubscribe(object? item)
	{
		if (m_subscriptions.Remove(Identity(item), out var subscription))
		{
			subscription.Revoke();
		}
	}

	// Revoking runs app code that may re-enter; entries leave the map before revoke so no
	// m_subscriptions iterator is live meanwhile.
	public partial void RetainOnly(HashSet<ulong> live)
	{
		List<Subscription> departed = new();
		foreach (var pair in m_subscriptions)
		{
			if (live.Contains(pair.Key))
			{
				continue;
			}
			m_subscriptions.Remove(pair.Key);
			departed.Add(pair.Value);
		}

		foreach (var subscription in departed)
		{
			subscription.Revoke();
		}
	}

	public partial void UnsubscribeAll()
	{
		List<Subscription> subscriptions = new(m_subscriptions.Values);
		m_subscriptions.Clear();
		foreach (var subscription in subscriptions)
		{
			subscription.Revoke();
		}
	}
}
