#nullable enable

#if __CROSSRUNTIME__

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Xaml.Core;

internal sealed class EventManager
{
	private List<DependencyObject?>? _loadedEventList;
	internal bool ShouldRaiseLoadedEvent { get; private set; }

	private EventManager()
	{
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

			// TODO: Should this be gated on WasmUseManagedLoadedUnloaded ?
			uiElementLoadedEventObject.RaiseLoaded();
		}

		_loadedEventList.Clear();

		ShouldRaiseLoadedEvent = false;
	}


	internal static EventManager Create()
	{
		return new EventManager();
	}

	internal void AddRequestsInOrder(UIElement @object, List<Request> eventList)
	{
		for (int i = eventList.Count - 1; i >= 0; i--)
		{
			var request = eventList[i];
			if (!request.Added)
			{
				this.AddRequest(@object, request);
			}
		}
	}

	private void AddRequest(UIElement @object, Request request)
	{
		//if (IsLoadedEvent(request.Event))
		{
			AddToLoadedEventList(@object);
		}
	}

	internal void RemoveRequest(UIElement @object, Request request)
	{
		//if (IsLoadedEvent(request.Event))
		{
			RemoveFromLoadedEventList(@object);
		}
	}

	private static Request CreateRequest(bool handledEventsToo)
	{
		var request = new Request();
		request.HandledEventsToo = handledEventsToo;
		return request;
	}

	internal static void AddEventListener(DependencyObject @do, List<Request> eventList, bool handledEventsToo)
	{
		var request = CreateRequest(handledEventsToo);
		eventList.Add(request);
	}
}

#endif
