#nullable enable

using System;

namespace Windows.UI.Core;

public sealed partial class SystemNavigationManager
{
	private static readonly object _backRequestedSubscribersChangedLock = new object();
	private static EventHandler<bool>? _backRequestedSubscribersChanged;

	/// <summary>
	/// Event raised when the BackRequested subscriber state changes.
	/// Used by ApplicationActivity to enable/disable OnBackPressedCallback on Android 16+.
	/// </summary>
	internal static event EventHandler<bool> BackRequestedSubscribersChanged
	{
		add
		{
			lock (_backRequestedSubscribersChangedLock)
			{
				_backRequestedSubscribersChanged += value;
			}
		}
		remove
		{
			lock (_backRequestedSubscribersChangedLock)
			{
				_backRequestedSubscribersChanged -= value;
			}
		}
	}

	partial void OnBackRequestedSubscribersChanged(bool hasSubscribers)
	{
		EventHandler<bool>? handlers;
		lock (_backRequestedSubscribersChangedLock)
		{
			handlers = _backRequestedSubscribersChanged;
		}

		handlers?.Invoke(this, hasSubscribers);
	}
}
