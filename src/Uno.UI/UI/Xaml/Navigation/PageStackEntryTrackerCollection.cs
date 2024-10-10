// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\PageStackEntryTrackerCollection.cpp, tag winui3/release/1.5.5, commit fd8e26f1d

using System;
using Windows.UI.Xaml.Navigation;
using Windows.Foundation.Collections;

namespace DirectUI;

internal class PageStackEntryTrackerCollection : TrackerCollection<PageStackEntry>
{
	// Flag indicating whether this collection corresponds to the BackStack or the ForwardStack.
	private bool m_isBackStack;

	// Weak reference to the NavigationHistory that owns this PageStack collection.
	private WeakReference<NavigationHistory> m_wrNavigationHistory;

	//TODO:MZ: Includes NavigationHistory interaction code

	internal void Init(NavigationHistory navigationHistory, bool isBackStack)
	{
		SetNavigationHistory(navigationHistory);
		m_isBackStack = isBackStack;
	}

	public override PageStackEntry this[int index]
	{
		get => base[index];
		set
		{
			var count = Count;
			if (index < 0 || index >= count)
			{
				throw new ArgumentOutOfRangeException(nameof(index));
			}

			OnVectorChanging(CollectionChange.ItemChanged, index, value);
			base[index] = value;
			OnVectorChanged(CollectionChange.ItemInserted, index);
		}
	}

	public override void Insert(int index, PageStackEntry item)
	{
		if (index < 0 || index > Count)
		{
			throw new ArgumentOutOfRangeException(nameof(index));
		}

		OnVectorChanging(CollectionChange.ItemInserted, index, item);
		base.Insert(index, item);
		OnVectorChanged(CollectionChange.ItemInserted, index);
	}

	public override void RemoveAt(int index)
	{
		if (index < 0 || index >= Count)
		{
			throw new ArgumentOutOfRangeException(nameof(index));
		}

		OnVectorChanging(CollectionChange.ItemRemoved, index, null);
		base.RemoveAt(index);
		OnVectorChanged(CollectionChange.ItemRemoved, index);
	}

	public override void Add(PageStackEntry item)
	{
		var count = Count;
		OnVectorChanging(CollectionChange.ItemInserted, count, item);
		base.Add(item);
		OnVectorChanged(CollectionChange.ItemInserted, count);
	}

	public override void RemoveAtEnd()
	{
		var count = Count;
		if (count == 0)
		{
			throw new InvalidOperationException("Collection is empty");
		}

		RemoveAt(count - 1);
	}

	public override void Clear()
	{
		OnVectorChanging(CollectionChange.Reset, 0, null);
		base.Clear();
		OnVectorChanged(CollectionChange.Reset, 0);
	}

	/// <summary>
	/// Append without any change notifiers, for internal use.
	/// </summary>
	internal void AddInternal(PageStackEntry item) => base.Add(item);

	/// <summary>
	/// Remove without any change notifiers, for internal use.
	/// </summary>
	internal void RemoveInternal(PageStackEntry item) => base.Remove(item);

	/// <summary>
	/// RemoveAtEnd without any change notifiers, for internal use.
	/// </summary>
	internal void RemoveAtEndInternal() => base.RemoveAtEnd();


	/// <summary>
	/// Clear without any change notifiers, for internal use.
	/// </summary>
	internal void ClearInternal() => base.Clear();

	internal PageStackEntry GetAtEnd() => this[Count - 1];

	private void OnVectorChanging(CollectionChange action, int index, PageStackEntry item)
	{
		var entry = item;
		var navigationHistory = GetNavigationHistory();
		if (navigationHistory is not null)
		{
			navigationHistory.OnPageStackChanging(m_isBackStack, action, index, entry);
		}
	}

	private void OnVectorChanged(CollectionChange action, int index)
	{
		var navigationHistory = GetNavigationHistory();
		if (navigationHistory is not null)
		{
			navigationHistory.OnPageStackChanged(m_isBackStack, action, index);
		}
	}

	private void SetNavigationHistory(NavigationHistory navigationHistory)
	{
		m_wrNavigationHistory = new WeakReference<NavigationHistory>(navigationHistory);
	}

	private NavigationHistory GetNavigationHistory()
	{
		if (m_wrNavigationHistory.TryGetTarget(out var navigationHistory))
		{
			return navigationHistory;
		}

		return null;
	}
}
