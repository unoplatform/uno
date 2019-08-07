using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace Uno.UI.Xaml
{
	[Flags]
	public enum RoutedEventFlag : ulong
	{
		None = 0,

		// Pointers
		PointerPressed = 1 << 0,
		PointerReleased = 1 << 1,
		PointerEntered = 1 << 2,
		PointerExited = 1 << 3,
		PointerMoved = 1 << 4,
		PointerCanceled = 1 << 5,
		PointerCaptureLost = 1 << 6,

		// Gestures
		Tapped = 1 << 7,
		DoubleTapped = 1 << 8,

		// Key
		KeyDown = 1 << 9,
		KeyUp = 1 << 10,

		// Focus
		GotFocus = 1 << 11,
		LostFocus = 1 << 12,
	}

	internal static class RoutedEventFlagExtensions
	{
		private const RoutedEventFlag _isPointer = // 0b0000_0000_0111_1111
			  RoutedEventFlag.PointerPressed
			| RoutedEventFlag.PointerReleased
			| RoutedEventFlag.PointerEntered
			| RoutedEventFlag.PointerExited
			| RoutedEventFlag.PointerMoved
			| RoutedEventFlag.PointerCanceled
			| RoutedEventFlag.PointerCaptureLost;

		private const RoutedEventFlag _isGesture = // 0b0000_0001_1000_0000
			RoutedEventFlag.Tapped
			| RoutedEventFlag.DoubleTapped;

		private const RoutedEventFlag _isKey = // 0b0000_0110_0000_0000
			RoutedEventFlag.KeyDown
			| RoutedEventFlag.KeyUp;

		private const RoutedEventFlag _isFocus = // 0b0001_1000_0000_0000
			RoutedEventFlag.GotFocus
			| RoutedEventFlag.LostFocus;

		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsPointerEvent(this RoutedEventFlag flag) => (flag & _isPointer) != 0;

		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsGestureEvent(this RoutedEventFlag flag) => (flag & _isGesture) != 0;

		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsKeyEvent(this RoutedEventFlag flag) => (flag & _isKey) != 0;

		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsFocusEvent(this RoutedEventFlag flag) => (flag & _isFocus) != 0;
	}
}
