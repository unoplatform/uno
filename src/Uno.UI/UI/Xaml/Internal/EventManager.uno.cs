#nullable enable

#if UNO_HAS_ENHANCED_LIFECYCLE

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Uno.UI.Helpers;
using Windows.Foundation;

namespace Uno.UI.Xaml.Core;

// Code here is mostly matching the logic from WinUI (even if the code isn't really a direct port), but it's in CoreImports.cpp and LayoutManager.cpp
// Since we don't yet have these, and this code is very related to events, we add it to this EventManager partial.
internal sealed partial class EventManager
{
	private readonly ConditionalWeakTable<FrameworkElement, object?> _layoutUpdatedSubscribers = new();
	private readonly Queue<SizeChangedQueueItem> _sizeChangedQueue = new(1024);

	internal bool HasPendingSizeChangedEvents => _sizeChangedQueue is { Count: > 0 };

	// Not using a record because Size struct doesn't implement `IEquatable<Size>`, so,
	// a record will use EqualityComparer<Size>.Default which is an `ObjectEqualityComparer`
	// which will cause boxing for every equality comparison. See
	// https://github.com/dotnet/runtime/issues/102593
	private readonly struct SizeChangedQueueItem : IEquatable<SizeChangedQueueItem>
	{
		public FrameworkElement Element { get; }
		public Size OldSize { get; }

		public SizeChangedQueueItem(FrameworkElement element, Size oldSize)
		{
			Element = element;
			OldSize = oldSize;
		}

		public override bool Equals(object? obj)
			=> obj is SizeChangedQueueItem item && Equals(item);

		public bool Equals(SizeChangedQueueItem other)
			=> ReferenceEquals(Element, other.Element) && OldSize == other.OldSize;

		public override int GetHashCode()
			=> HashCode.Combine(Element, OldSize);

		public static bool operator ==(SizeChangedQueueItem left, SizeChangedQueueItem right)
			=> left.Equals(right);

		public static bool operator !=(SizeChangedQueueItem left, SizeChangedQueueItem right)
			=> !(left == right);
	}

	public void AddLayoutUpdatedEventHandler(FrameworkElement element)
	{
		_layoutUpdatedSubscribers.Add(element, null);
	}

	public void RemoveLayoutUpdatedEventHandler(FrameworkElement element)
	{
		_layoutUpdatedSubscribers.Remove(element);
	}

	public void RaiseLayoutUpdated()
	{
		// Elements that are added after the enumerator is retrieved will not be looped over here.
		// This is actually what we want and what WinUI does.
		foreach (var item in _layoutUpdatedSubscribers)
		{
			// Sometimes, we are racing with GC and the DependencyObjectStore is disposed (via finalizer)
			if (((IDependencyObjectStoreProvider)item.Key).Store.IsDisposed)
			{
				continue;
			}

			item.Key.OnLayoutUpdated();
		}
	}

	public void EnqueueForSizeChanged(FrameworkElement element, Size oldSize)
	{
		// Uno-specific. Maybe render transforms should subscribe to SizeChanged instead.
		element.UpdateRenderTransformSize(element.RenderSize);

		if (element.WantsSizeChanged)
		{
			_sizeChangedQueue.Enqueue(new SizeChangedQueueItem(element, oldSize));
		}
	}

	public void RaiseSizeChangedEvents()
	{
		// While raising the events, more size changed requests might get queued
		// into _sizeChangedQueue. So we repeatedly move items into a tmp vector
		// and raise the events until sizeChangedQueue gets empty.
		while (_sizeChangedQueue.Count > 0)
		{
			// There's no guarantee that the collection won't change as a side
			// effect of raising the event on an item, so we use a temp stack vector
			// of the items to prevent using an invalid iterator.
			var currentCount = _sizeChangedQueue.Count;
			var tmp = ArrayPool<SizeChangedQueueItem>.Shared.Rent(currentCount);

			// Note the move instead of copy here to avoid doing an extra
			// addref/release on the item. Also note that the order in which
			// we call the event is reversed.
			var index = currentCount;
			// Uno docs: WinUI uses rbegin and rend for iterating here, which is "reverse" order.
			// As .NET's queue doesn't have an efficient way for reverse iterating, we just
			// start our index from currentCount - 1 till we reach zero.
			foreach (var item in _sizeChangedQueue)
			{
				index--;
				tmp[index] = item;
			}

			Debug.Assert(index == 0);

			_sizeChangedQueue.Clear();

			for (int i = 0; i < currentCount; i++)
			{
				var item = tmp[i];
				//TraceIndividualSizeChangedBegin();

				//if (auto layoutStorage = item.m_pElement->GetLayoutStorage())
				{
					var args = new SizeChangedEventArgs(item.Element, item.OldSize, item.Element.RenderSize);
					// AddRef for args is done on the managed side
					try
					{
						item.Element.RaiseSizeChanged(args);
					}
					catch
					{
						// Empty catch to have the same behavior as "IGNOREHR" in WinUI.
					}
				}

				//TraceIndividualSizeChangedEnd(UINT64(item.m_pElement));
			}

			ArrayPool<SizeChangedQueueItem>.Shared.Return(tmp, clearArray: true);
		}
	}
}

#endif
