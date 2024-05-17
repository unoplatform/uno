#nullable enable

#if UNO_HAS_ENHANCED_LIFECYCLE

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Uno.UI.Helpers;

namespace Uno.UI.Xaml.Core;

// Code here is mostly matching the logic from WinUI (even if the code isn't really a direct port), but it's in CoreImports.cpp and LayoutManager.cpp
// Since we don't yet have these, and this code is very related to events, we add it to this EventManager partial.
internal sealed partial class EventManager
{
	private readonly List<WeakReference<FrameworkElement>> _layoutUpdatedSubscribers = new();

	public void AddLayoutUpdatedEventHandler(FrameworkElement element)
	{
		var subscriberIndex = GetSubscriberIndex(element);
		if (subscriberIndex < 0)
		{
			_layoutUpdatedSubscribers.Add(new WeakReference<FrameworkElement>(element));
		}
		else
		{
			// AddLayoutUpdatedEventHandler is only called for a first subscriber.
			Debug.Fail("This shouldn't happen");
		}
	}

	public void RemoveLayoutUpdatedEventHandler(FrameworkElement element)
	{
		var subscriberIndex = GetSubscriberIndex(element);
		if (subscriberIndex >= 0)
		{
			_layoutUpdatedSubscribers.RemoveAt(subscriberIndex);
		}
		else
		{
			// RemoveLayoutUpdatedEventHandler is only called when removing the last subscriber.
			Debug.Fail("This shouldn't happen");
		}
	}

	private int GetSubscriberIndex(FrameworkElement element)
	{
		for (int i = 0; i < _layoutUpdatedSubscribers.Count; i++)
		{
			if (_layoutUpdatedSubscribers[i].TryGetTarget(out var target) && target == element)
			{
				return i;
			}
		}

		return -1;
	}

	public void RaiseLayoutUpdated()
	{
		var count = _layoutUpdatedSubscribers.Count;
		var clone = ArrayPool<WeakReference<FrameworkElement>>.Shared.Rent(_layoutUpdatedSubscribers.Count);
		try
		{
			for (int i = 0; i < count; i++)
			{
				clone[i] = _layoutUpdatedSubscribers[i];
			}

			for (int i = 0; i < count; i++)
			{
				if (clone[i].TryGetTarget(out var target))
				{
					target.OnLayoutUpdated();
				}
			}
		}
		finally
		{
			ArrayPool<WeakReference<FrameworkElement>>.Shared.Return(clone);
		}
	}
}

#endif
