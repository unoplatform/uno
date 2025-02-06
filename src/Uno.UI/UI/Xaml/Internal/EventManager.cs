#nullable enable

#if UNO_HAS_ENHANCED_LIFECYCLE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Dispatching;

namespace Uno.UI.Xaml.Core;

internal sealed partial class EventManager
{
	private List<UIElement?> _loadedEventList = new(1024);
	private List<(FrameworkElement Element, EffectiveViewportChangedEventArgs Args)> _effectiveViewportChangedQueue = new(32);

	internal bool ShouldRaiseLoadedEvent { get; private set; }

	internal bool HasPendingViewportChangedEvents => _effectiveViewportChangedQueue is { Count: > 0 };

	private EventManager()
	{
	}

	internal void EnqueueForEffectiveViewportChanged(FrameworkElement element, EffectiveViewportChangedEventArgs args)
	{
		_effectiveViewportChangedQueue.Add((element, args));
		CoreServices.RequestAdditionalFrame();
	}

	internal void RaiseEffectiveViewportChangedEvents()
	{
		for (int i = 0; i < _effectiveViewportChangedQueue.Count; i++)
		{
			var (fe, args) = _effectiveViewportChangedQueue[i];

			_effectiveViewportChangedQueue[i] = default;

			fe.RaiseEffectiveViewportChanged(args);
		}

		_effectiveViewportChangedQueue.Clear();

	}

	private void AddToLoadedEventList(UIElement element)
	{
		Debug.Assert(!_loadedEventList.Contains(element));
		_loadedEventList.Add(element);
	}

	private void RemoveFromLoadedEventList(UIElement element)
	{
		_loadedEventList.Remove(element);

		// Remove will only remove the first occurrence. Could it happen we have multiple occurrences of the same element?
		Debug.Assert(!_loadedEventList.Contains(element));
	}

	internal void RequestRaiseLoadedEventOnNextTick()
	{
		ShouldRaiseLoadedEvent = true;
		CoreServices.RequestAdditionalFrame();
	}

	internal void RaiseLoadedEvent()
	{
		Debug.Assert(ShouldRaiseLoadedEvent);

		if (!ShouldRaiseLoadedEvent)
		{
			return;
		}

		for (int i = 0; i < _loadedEventList.Count; i++)
		{
			var loadedEventObject = _loadedEventList[i]!;
			_loadedEventList[i] = null;

			// Uno docs: Even though Loaded is a FrameworkElement event, it's necessary to raise it from UIElement to call UpdateHitTest.
			// This has an effect specifically for Wasm Hyperlink (Wasm's TextElement class inherits UIElement, this doesn't match WinUI)
			loadedEventObject.RaiseLoaded();
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
