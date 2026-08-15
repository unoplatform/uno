#nullable enable

using System.Diagnostics;
using UIKit;

namespace Windows.UI.Input;

internal static class InputPaneInterop
{
	internal static bool TryShow() => false;

	internal static bool TryHide()
	{
		UIApplication.SharedApplication.KeyWindow?.EndEditing(true);
		return true;
	}
}
