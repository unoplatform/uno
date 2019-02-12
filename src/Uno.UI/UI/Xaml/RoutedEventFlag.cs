using System;

namespace Uno.UI.Xaml
{
	[Flags]
	public enum RoutedEventFlag : ulong
	{
		None = 0,
		PointerPressed = 1 << 0,
		PointerReleased = 1 << 1,
		PointerEntered = 1 << 2,
		PointerExited = 1 << 3,
		PointerMoved = 1 << 4,
		PointerCanceled = 1 << 5,
		PointerCaptureLost = 1 << 6,
		Tapped = 1 << 7,
		DoubleTapped = 1 << 8,
		KeyDown = 1 << 9,
		KeyUp = 1 << 10,
		GotFocus = 1 << 11,
		LostFocus = 1 << 12
	}
}
