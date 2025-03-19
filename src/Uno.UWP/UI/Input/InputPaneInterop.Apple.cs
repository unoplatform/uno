#nullable enable

using System.Diagnostics;
using Android.Views.InputMethods;

namespace Windows.UI.Input;

internal static class InputPaneInterop
{
	internal static bool TryShow() => false;

	internal static bool TryHide()
	{
		UIKit.UIApplication.SharedApplication.KeyWindow.EndEditing(true);
		return true;
	}
}
