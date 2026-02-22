#nullable enable

using System;

namespace Windows.UI.Core;

public sealed partial class SystemNavigationManager
{
	/// <summary>
	/// Event raised when the BackRequested subscriber state changes.
	/// Used by ApplicationActivity to enable/disable OnBackPressedCallback on Android 16+.
	/// </summary>
	internal static event EventHandler<bool>? BackRequestedSubscribersChanged;

	partial void OnBackRequestedSubscribersChanged(bool hasSubscribers)
	{
		BackRequestedSubscribersChanged?.Invoke(this, hasSubscribers);
	}
}
