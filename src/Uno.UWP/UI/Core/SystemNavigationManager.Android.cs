#nullable enable

using System;

namespace Windows.UI.Core;

public sealed partial class SystemNavigationManager
{
	/// <summary>
	/// Event raised when the back handler state changes.
	/// Used by ApplicationActivity to enable/disable OnBackPressedCallback on Android 16+.
	/// </summary>
	internal static event EventHandler<bool>? BackHandlerStateChanged;

	partial void OnBackHandlerStateChanged()
	{
		BackHandlerStateChanged?.Invoke(this, HasAnyBackHandlers);
	}
}
