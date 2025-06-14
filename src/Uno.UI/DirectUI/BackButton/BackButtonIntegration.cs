using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.DataBinding;
using Windows.UI.Core;
using Windows.UI.Core.Preview;

namespace DirectUI;

internal static class BackButtonIntegration
{
	private readonly static List<ManagedWeakReference> _listeners = new();

	internal static void Initialize()
	{
		// Register the back button press event handler
		SystemNavigationManager.GetForCurrentView().BackRequested += OnBackButtonPressed;
	}

	internal static void OnBackButtonPressed(object sender, BackRequestedEventArgs args)
	{
		if (args.Handled)
		{
			return;
		}

		// Notify all registered listeners of the back button press event
		foreach (var weakListener in _listeners.ToArray())
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
			}
		}
	}

	/// <summary>
	/// Registers a listener for back button presses.
	/// </summary>
	/// <param name="listener">The listener to register.</param>
	/// <returns>True if the listener was successfully registered, false otherwise.</returns>
	public static void RegisterListener(IBackButtonListener listener)
	{
		if (listener == null)
		{
			throw new ArgumentNullException(nameof(listener));
		}

		if (_listeners.Any(wr => wr.IsAlive && wr.Target == listener))
		{
			// Listener is already registered, no need to register again
			return;
		}

		var weakListener = WeakReferencePool.RentWeakReference(_listeners, listener);
		_listeners.Add(weakListener);
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
	}
}
