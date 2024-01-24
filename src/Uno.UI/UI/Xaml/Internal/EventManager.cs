#nullable enable

#if UNO_HAS_ENHANCED_LIFECYCLE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Xaml.Core;

internal sealed class EventManager
{
	private List<DependencyObject?>? _loadedEventList;
	private List<(FrameworkElement Element, EffectiveViewportChangedEventArgs Args)>? _effectiveViewportChangedQueue;

	internal bool ShouldRaiseLoadedEvent { get; private set; }

	internal bool HasPendingViewportChangedEvents => _effectiveViewportChangedQueue is { Count: > 0 };

	private EventManager()
	{
	}

	internal void EnqueueForEffectiveViewportChanged(FrameworkElement element, EffectiveViewportChangedEventArgs args)
	{
		if (_effectiveViewportChangedQueue is null)
		{
			_effectiveViewportChangedQueue = new();
		}

		_effectiveViewportChangedQueue.Add((element, args));
	}

	internal void RaiseEffectiveViewportChangedEvents()
	{
		if (_effectiveViewportChangedQueue is null)
		{
			return;
		}

		for (int i = 0; i < _effectiveViewportChangedQueue.Count; i++)
		{
			var (fe, args) = _effectiveViewportChangedQueue[i];

			_effectiveViewportChangedQueue[i] = default;

			fe.RaiseEffectiveViewportChanged(args);
		}

		_effectiveViewportChangedQueue.Clear();

	}

	private void AddToLoadedEventList(DependencyObject element)
	{
		if (_loadedEventList is null)
		{
			_loadedEventList = new();
		}

		// TODO: Maybe this shouldn't happen?
		// It doesn't make sense to attempt to add duplicate elements.
		if (!_loadedEventList.Contains(element))
		{
			_loadedEventList.Add(element);
		}
	}

	private void RemoveFromLoadedEventList(DependencyObject element)
	{
		if (_loadedEventList is not null)
		{
			_loadedEventList.Remove(element);

			// Remove will only remove the first occurrence. Could it happen we have multiple occurrences of the same element?
			Debug.Assert(!_loadedEventList.Contains(element));
		}
	}

	internal void RequestRaiseLoadedEventOnNextTick()
	{
		ShouldRaiseLoadedEvent = true;
		//RequestAdditionalFrame();
	}

	internal void RaiseLoadedEvent()
	{
		Debug.Assert(ShouldRaiseLoadedEvent);

		if (!ShouldRaiseLoadedEvent || _loadedEventList is null)
		{
			return;
		}

		for (int i = 0; i < _loadedEventList.Count; i++)
		{
			var loadedEventObject = _loadedEventList[i];
			if (loadedEventObject is not UIElement uiElementLoadedEventObject)
			{
				continue;
			}

			_loadedEventList[i] = null;

			uiElementLoadedEventObject.RaiseLoaded();
		}

		_loadedEventList.Clear();

		ShouldRaiseLoadedEvent = false;
	}


	internal static EventManager Create()
	{
		return new EventManager();
	}

	/// <summary>
	/// Uno docs: Currently, AddRequestsInOrder is ONLY used for Loaded event, and only adds a single request.
	/// WinUI does MUCH more than this.
	/// </summary>
	/// <param name="object"></param>
	internal void AddRequestsInOrder(UIElement @object/*, List<Request> eventList*/)
	{
		//for (int i = eventList.Count - 1; i >= 0; i--)
		//{
		//	var request = eventList[i];
		//	if (!request.Added)
		//	{
		//		this.AddRequest(@object, request);
		//	}
		//}

		this.AddRequest(@object);
	}

	private void AddRequest(UIElement @object/*, Request request*/)
	{
		//if (IsLoadedEvent(request.Event))
		{
			AddToLoadedEventList(@object);
		}
	}

	internal void RemoveRequest(UIElement @object/*, Request request*/)
	{
		//if (IsLoadedEvent(request.Event))
		{
			RemoveFromLoadedEventList(@object);
		}
	}
}

#endif
