using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace Uno.UI.Toolkit.DevTools.Xaml;

internal static class UIElementHelper
{
	public static void RaiseKeyDownEvent(this UIElement element, VirtualKey virtualKey, VirtualKeyModifiers virtualKeyModifiers, char? unicodeKey)
	{
#if HAS_UNO
		element.SafeRaiseEvent(
				UIElement.KeyDownEvent,
				new KeyRoutedEventArgs(
					element,
					virtualKey,
					virtualKeyModifiers,
					unicodeKey: unicodeKey));
#endif
	}

	public static void RaiseKeyUpEvent(this UIElement element, VirtualKey virtualKey, VirtualKeyModifiers virtualKeyModifiers, char? unicodeKey)
	{
#if HAS_UNO
		element.SafeRaiseEvent(
				UIElement.KeyUpEvent,
				new KeyRoutedEventArgs(
					element,
					virtualKey,
					virtualKeyModifiers,
					unicodeKey: unicodeKey));
#endif
	}
}
