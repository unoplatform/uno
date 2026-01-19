using System;
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
		PointerWheelChanged = 1UL << 7,

		// Keyboard
		PreviewKeyDown = 1UL << 12,
		KeyDown = 1UL << 13,
		PreviewKeyUp = 1UL << 14,
		KeyUp = 1UL << 15,
		// CharacterReceived = 1UL << 16,
		// ProcessKeyboardAccelerators = 1UL << 17, => Reserved for future use (even if it is not an actual standard RoutedEvent)
		// AccessKeyInvoked = 1UL << 18, => Reserved for future use (even if it is not an actual standard RoutedEvent)
		// AccessKeyDisplayRequested = 1UL << 19, => Reserved for future use (even if it is not an actual standard RoutedEvent)
		// AccessKeyDisplayDismissed = 1UL << 20, => Reserved for future use (even if it is not an actual standard RoutedEvent)

		// Focus
		GettingFocus = 1UL << 24,
		GotFocus = 1UL << 25,
		LosingFocus = 1UL << 26,
		LostFocus = 1UL << 27,
		NoFocusCandidateFound = 1UL << 28,
		BringIntoViewRequested = 1UL << 29,

		// Drag and drop
		DragStarting = 1UL << 32, // this is actually not a RoutedEvent
		DragEnter = 1UL << 33,
		DragLeave = 1UL << 34,
		DragOver = 1UL << 35,
		Drop = 1UL << 36,
		DropCompleted = 1UL << 37, // this is actually not a RoutedEvent

		// Manipulations
		ManipulationStarting = 1UL << 40,
		ManipulationStarted = 1UL << 41,
		ManipulationDelta = 1UL << 42,
		ManipulationInertiaStarting = 1UL << 43,
		ManipulationCompleted = 1UL << 44,

		// Gestures
		Tapped = 1UL << 48,
		DoubleTapped = 1UL << 49,
		RightTapped = 1UL << 50,
		Holding = 1UL << 51,

		// Context menu
		ContextRequested = 1UL << 61,
		ContextCanceled = 1UL << 62,
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
			| RoutedEventFlag.PointerCaptureLost
			| RoutedEventFlag.PointerWheelChanged;

		private const RoutedEventFlag _isKey = // 0b0000_0000_0000_0000___0000_0000_0000_0000___0000_0000_0001_1111___1111_0000_0000_0000
			  RoutedEventFlag.PreviewKeyDown
			| RoutedEventFlag.PreviewKeyUp
			| RoutedEventFlag.KeyDown
			| RoutedEventFlag.KeyUp;

		private const RoutedEventFlag _isTunneling =
			  RoutedEventFlag.PreviewKeyDown
			| RoutedEventFlag.PreviewKeyUp;

		private const RoutedEventFlag _isFocus = // 0b0000_0000_0000_0000___0000_0000_0000_0000___0111_1111_0000_0000___0000_0000_0000_0000
			  RoutedEventFlag.GotFocus
			| RoutedEventFlag.LostFocus
			| RoutedEventFlag.GettingFocus
			| RoutedEventFlag.LosingFocus
			| RoutedEventFlag.NoFocusCandidateFound
			| RoutedEventFlag.BringIntoViewRequested;

		private const RoutedEventFlag _isDragAndDrop = // 0b0000_0000_0000_0000___0000_0000_0011_1111___0000_0000_0000_0000___0000_0000_0000_0000;
			  RoutedEventFlag.DragStarting
			| RoutedEventFlag.DragEnter
			| RoutedEventFlag.DragLeave
			| RoutedEventFlag.DragOver
			| RoutedEventFlag.Drop
			| RoutedEventFlag.DropCompleted;

		private const RoutedEventFlag _isManipulation = // 0b0000_0000_0000_0000___0001_1111_0000_0000___0000_0000_0000_0000___0000_0000_0000_0000
			  RoutedEventFlag.ManipulationStarting
			| RoutedEventFlag.ManipulationStarted
			| RoutedEventFlag.ManipulationDelta
			| RoutedEventFlag.ManipulationInertiaStarting
			| RoutedEventFlag.ManipulationCompleted;

		private const RoutedEventFlag _isGesture = // 0b0000_0000_0000_1111___0000_0000_0000_0000___0000_0000_0000_0000___0000_0000_0000_0000
			RoutedEventFlag.Tapped
			| RoutedEventFlag.DoubleTapped
			| RoutedEventFlag.RightTapped
			| RoutedEventFlag.Holding;

		private const RoutedEventFlag _isContextMenu = // 0b0110_0000_0000_0000___0000_0000_0000_0000___0000_0000_0000_0000___0000_0000_0000_0000
			  RoutedEventFlag.ContextRequested
			| RoutedEventFlag.ContextCanceled;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsPointerEvent(this RoutedEventFlag flag) => (flag & _isPointer) != 0;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsKeyEvent(this RoutedEventFlag flag) => (flag & _isKey) != 0;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsTunnelingEvent(this RoutedEventFlag flag) => (flag & _isTunneling) != 0;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsFocusEvent(this RoutedEventFlag flag) => (flag & _isFocus) != 0;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsDragAndDropEvent(this RoutedEventFlag flag) => (flag & _isDragAndDrop) != 0;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsManipulationEvent(this RoutedEventFlag flag) => (flag & _isManipulation) != 0;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsGestureEvent(this RoutedEventFlag flag) => (flag & _isGesture) != 0;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsContextEvent(this RoutedEventFlag flag) => (flag & _isContextMenu) != 0;
	}
}
