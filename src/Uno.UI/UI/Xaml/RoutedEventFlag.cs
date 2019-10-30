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
		PointerPressed = 1UL << 0,
		PointerReleased = 1UL << 1,
		PointerEntered = 1UL << 2,
		PointerExited = 1UL << 3,
		PointerMoved = 1UL << 4,
		PointerCanceled = 1UL << 5,
		PointerCaptureLost = 1UL << 6,
		// PointerWheelChanged = 1UL << 7, => Reserved for future usage

		// Keyboard
		// PreviewKeyDown = 1UL << 12 => Reserved for future usage
		KeyDown = 1UL << 13,
		// PreviewKeyUp = 1 >> 14, => Reserved for future usage
		KeyUp = 1UL << 15,
		// CharacterReceived = 1UL << 16,
		// ProcessKeyboardAccelerators = 1UL << 17, => Reserved for future use (even if it is not an actual standard RoutedEvent)
		// AccessKeyInvoked = 1UL << 18, => Reserved for future use (even if it is not an actual standard RoutedEvent)
		// AccessKeyDisplayRequested = 1UL << 19, => Reserved for future use (even if it is not an actual standard RoutedEvent)
		// AccessKeyDisplayDismissed = 1UL << 20, => Reserved for future use (even if it is not an actual standard RoutedEvent)

		// Focus
		// GettingFocus = 1UL << 24, => Reserved for future usage
		GotFocus = 1UL << 25,
		// LosingFocus = 1UL << 26, => Reserved for future usage
		LostFocus = 1UL << 27,
		// NoFocusCandidateFound = 1UL << 28, => Reserved for future usage
		// BringIntoViewRequested = 1UL << 29, => Reserved for future usage 

		// Drag and drop
		// DragEnter = 1UL << 32, => Reserved for future usage
		// DragLeave = 1UL << 33, => Reserved for future usage
		// DragOver = 1UL << 34, => Reserved for future usage
		// Drop = 1UL << 35, => Reserved for future usage 
		// DropCompleted = 1UL << 36, => Reserved for future use (even if it is not an actual standard RoutedEvent)

		// Manipulations
		ManipulationStarting = 1UL << 40,
		ManipulationStarted = 1UL << 41,
		ManipulationDelta = 1UL << 42,
		ManipulationInertiaStarting = 1UL << 43,
		ManipulationCompleted = 1UL << 44,

		// Gestures
		Tapped = 1UL << 48,
		DoubleTapped = 1UL << 49,
		// RightTapped = 1UL << 50, => Reserved for future usage 
		// Holding = 1UL << 51, => Reserved for future usage 

		// Context menu
		// ContextRequested = 1UL << 61, => Reserved for future usage 
		// ContextCanceled  = 1UL << 62, => Reserved for future use (even if it is not an actual standard RoutedEvent)
	}

	internal static class RoutedEventFlagExtensions
	{
		private const RoutedEventFlag _isPointer = // 0b0000_0000_0000_0000___0000_0000_0000_0000___0000_0000_0000_0000___0000_0000_1111_1111
			  RoutedEventFlag.PointerPressed
			| RoutedEventFlag.PointerReleased
			| RoutedEventFlag.PointerEntered
			| RoutedEventFlag.PointerExited
			| RoutedEventFlag.PointerMoved
			| RoutedEventFlag.PointerCanceled
			| RoutedEventFlag.PointerCaptureLost;
			//| RoutedEventFlag.PointerWheelChanged;

		private const RoutedEventFlag _isKey = // 0b0000_0000_0000_0000___0000_0000_0000_0000___0000_0000_0001_1111___1111_0000_0000_0000
			  RoutedEventFlag.KeyDown
			| RoutedEventFlag.KeyUp;

		private const RoutedEventFlag _isFocus = // 0b0000_0000_0000_0000___0000_0000_0000_0000___0011_1111_0000_0000___0000_0000_0000_0000
			  RoutedEventFlag.GotFocus
			| RoutedEventFlag.LostFocus;

		private const RoutedEventFlag _isDragAndDrop = (RoutedEventFlag)0b0000_0000_0000_0000___0000_0000_0001_1111___0000_0000_0000_0000___0000_0000_0000_0000;
			//  RoutedEventFlag.DragEnter
			//| RoutedEventFlag.DragLeave
			//| RoutedEventFlag.DragOver
			//| RoutedEventFlag.Drop
			//| RoutedEventFlag.DropCompleted;

		private const RoutedEventFlag _isManipulation = // 0b0000_0000_0000_0000___0001_1111_0000_0000___0000_0000_0000_0000___0000_0000_0000_0000
			  RoutedEventFlag.ManipulationStarting
			| RoutedEventFlag.ManipulationStarted
			| RoutedEventFlag.ManipulationDelta
			| RoutedEventFlag.ManipulationInertiaStarting
			| RoutedEventFlag.ManipulationCompleted;

		private const RoutedEventFlag _isGesture = // 0b0000_0000_0001_1111___0000_0000_0000_0000___0000_0000_0000_0000___0000_0000_0000_0000
			  RoutedEventFlag.Tapped
			| RoutedEventFlag.DoubleTapped;

		private const RoutedEventFlag _isContextMenu = (RoutedEventFlag)0b0011_0000_0000_0000___0000_0000_0000_0000___0000_0000_0000_0000___0000_0000_0000_0000;
			//   RoutedEventFlag.ContextRequested
			// | RoutedEventFlag.ContextCanceled;

		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsPointerEvent(this RoutedEventFlag flag) => (flag & _isPointer) != 0;

		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsKeyEvent(this RoutedEventFlag flag) => (flag & _isKey) != 0;

		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsFocusEvent(this RoutedEventFlag flag) => (flag & _isFocus) != 0;

		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsDragAndDropEvent(this RoutedEventFlag flag) => (flag & _isDragAndDrop) != 0;

		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsManipulationEvent(this RoutedEventFlag flag) => (flag & _isManipulation) != 0;

		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsGestureEvent(this RoutedEventFlag flag) => (flag & _isGesture) != 0;

		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsContextEvent(this RoutedEventFlag flag) => (flag & _isContextMenu) != 0;
	}
}
