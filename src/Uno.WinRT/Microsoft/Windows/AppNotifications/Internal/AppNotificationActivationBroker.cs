#nullable enable

using System;
using System.Collections.Generic;
using Uno.Foundation.Logging;

namespace Microsoft.Windows.AppNotifications.Internal;

internal static class AppNotificationActivationBroker
{
	private const int MaxPendingActivations = 32;
	private const int MaxArgumentLength = 5120;
	private const int MaxUserInputCount = 5;
	private const int MaxUserInputKeyLength = 256;
	private const int MaxUserInputValueLength = 4096;
	private static readonly object _gate = new();
	private static readonly Queue<AppNotificationActivation> _pending = new();
	private static Action<AppNotificationActivation>? _handler;
	private static bool _isDraining;
	private static bool _wasRegistered;
	private static int _activeCallbacks;
	private static int _callbackThreadId;

	public static bool Publish(AppNotificationActivation activation)
	{
		ArgumentNullException.ThrowIfNull(activation);
		if (!IsValid(activation))
		{
			return false;
		}

		var shouldDrain = false;
		lock (_gate)
		{
			if (_handler is null && _wasRegistered)
			{
				return false;
			}
			if (_pending.Count == MaxPendingActivations)
			{
				_pending.Dequeue();
			}
			_pending.Enqueue(activation);
			if (_handler is not null && !_isDraining)
			{
				_isDraining = true;
				shouldDrain = true;
			}
		}
		if (shouldDrain)
		{
			Drain();
		}
		return true;
	}

	public static void Register(Action<AppNotificationActivation> handler)
	{
		ArgumentNullException.ThrowIfNull(handler);
		var shouldDrain = false;
		lock (_gate)
		{
			_wasRegistered = true;
			_handler = handler;
			if (!_isDraining && _pending.Count > 0)
			{
				_isDraining = true;
				shouldDrain = true;
			}
		}
		if (shouldDrain)
		{
			Drain();
		}
	}

	public static void Unregister(Action<AppNotificationActivation> handler)
	{
		lock (_gate)
		{
			if (_handler == handler)
			{
				_handler = null;
			}
			_pending.Clear();
			while (_activeCallbacks > 0 && _callbackThreadId != Environment.CurrentManagedThreadId)
			{
				System.Threading.Monitor.Wait(_gate);
			}
		}
	}

	private static void Drain()
	{
		while (true)
		{
			Action<AppNotificationActivation> handler;
			AppNotificationActivation activation;
			lock (_gate)
			{
				if (_handler is null || _pending.Count == 0)
				{
					_isDraining = false;
					return;
				}
				handler = _handler;
				activation = _pending.Dequeue();
				_activeCallbacks++;
				_callbackThreadId = Environment.CurrentManagedThreadId;
			}

			try
			{
				handler(activation);
			}
			catch (Exception exception)
			{
				// Activations are dispatched from native callbacks, so a failing handler must neither
				// strand the queued activations nor let the exception reach the platform.
				if (typeof(AppNotificationActivationBroker).Log().IsEnabled(LogLevel.Error))
				{
					typeof(AppNotificationActivationBroker).Log().LogError($"An app notification activation handler failed: {exception}");
				}
			}
			finally
			{
				lock (_gate)
				{
					_activeCallbacks--;
					if (_activeCallbacks == 0)
					{
						_callbackThreadId = 0;
						System.Threading.Monitor.PulseAll(_gate);
					}
				}
			}
		}
	}

	private static bool IsValid(AppNotificationActivation activation)
	{
		if (activation.Argument.Length > MaxArgumentLength || activation.UserInput.Count > MaxUserInputCount)
		{
			return false;
		}
		foreach (var input in activation.UserInput)
		{
			if (input.Key.Length == 0 || input.Key.Length > MaxUserInputKeyLength || input.Value.Length > MaxUserInputValueLength)
			{
				return false;
			}
		}
		return true;
	}

	internal static void ResetForTests()
	{
		lock (_gate)
		{
			_handler = null;
			_pending.Clear();
			_isDraining = false;
			_wasRegistered = false;
			_activeCallbacks = 0;
			_callbackThreadId = 0;
		}
	}
}
