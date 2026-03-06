#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Uno.UI.DataBinding;
using Windows.UI.Core;

namespace DirectUI;

internal static class BackButtonIntegration
{
	private readonly static List<ManagedWeakReference> _listeners = new();
	private static bool _initialized;

	internal static void Initialize()
	{
		if (_initialized)
		{
			return;
		}
		_initialized = true;
		SystemNavigationManager.GetForCurrentView().InternalBackRequested += OnBackButtonPressed;
	}

	internal static bool InjectBackButtonPress()
	{
		return SystemNavigationManager.GetForCurrentView().RequestBack();
	}

	internal static void OnBackButtonPressed(object? sender, BackRequestedEventArgs args)
	{
		if (args.Handled)
		{
			return;
		}

		bool removedDeadRefs = false;

		// Notify all registered listeners of the back button press event (LIFO order)
		foreach (var weakListener in _listeners.ToArray().Reverse())
		{
			if (weakListener.IsAlive)
			{
				var listener = weakListener.Target as IBackButtonListener;
				if (listener is not null && listener.OnBackButtonPressed())
				{
					args.Handled = true;
					return; // Stop processing if a listener has handled the event
				}
			}
			else
			{
				// Remove dead references
				_listeners.Remove(weakListener);
				WeakReferencePool.ReturnWeakReference(_listeners, weakListener);
				removedDeadRefs = true;
			}
		}

		if (removedDeadRefs && _listeners.Count == 0)
		{
			SystemNavigationManager.Instance.SetHasInternalBackListeners(false);
		}
	}

	/// <summary>
	/// Registers a listener for back button presses.
	/// </summary>
	/// <param name="listener">The listener to register.</param>
	public static void RegisterListener(IBackButtonListener listener)
	{
		if (listener == null)
		{
			throw new ArgumentNullException(nameof(listener));
		}

		// Prune dead references so isFirst reflects only live listeners
		var deadRefs = _listeners.Where(wr => !wr.IsAlive).ToList();
		foreach (var dead in deadRefs)
		{
			_listeners.Remove(dead);
			WeakReferencePool.ReturnWeakReference(_listeners, dead);
		}

		if (_listeners.Any(wr => wr.IsAlive && wr.Target == listener))
		{
			// Listener is already registered, no need to register again
			return;
		}

		var isFirst = _listeners.Count == 0;
		var weakListener = WeakReferencePool.RentWeakReference(_listeners, listener);
		_listeners.Add(weakListener);

		if (isFirst)
		{
			SystemNavigationManager.Instance.SetHasInternalBackListeners(true);
		}
	}

	public static void UnregisterListener(IBackButtonListener listener)
	{
		if (listener == null)
		{
			throw new ArgumentNullException(nameof(listener));
		}

		var weakListener = _listeners.FirstOrDefault(wr => wr.IsAlive && wr.Target == listener);
		if (weakListener is not null)
		{
			_listeners.Remove(weakListener);
			WeakReferencePool.ReturnWeakReference(_listeners, weakListener);
		}

		// Prune any dead weak references
		var deadRefs = _listeners.Where(wr => !wr.IsAlive).ToList();
		foreach (var dead in deadRefs)
		{
			_listeners.Remove(dead);
			WeakReferencePool.ReturnWeakReference(_listeners, dead);
		}

		if (!_listeners.Any(wr => wr.IsAlive))
		{
			SystemNavigationManager.Instance.SetHasInternalBackListeners(false);
		}
	}
}
