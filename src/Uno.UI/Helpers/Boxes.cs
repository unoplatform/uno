using System;
using Uno.UI.Xaml;

namespace Uno.UI.Helpers;

#pragma warning disable UnoInternal0001 // This is the special boxes implementation :)

// Note: More Boxes can be added here, e.g, for commonly used enums.
// In this case, make sure to update the BoxingDiagnosticAnalyzer.

internal static class Boxes
{
	public static class BooleanBoxes
	{
		public static readonly object BoxedTrue = true;
		public static readonly object BoxedFalse = false;
	}

	public static class IntegerBoxes
	{
		public static readonly object NegativeOne = -1;
		public static readonly object Zero = 0;
		public static readonly object One = 1;
	}

	public static class DoubleBoxes
	{
		public static readonly object Zero = 0.0;
	}

	public static class RoutedEventFlagBoxes
	{
		public static readonly object None = RoutedEventFlag.None;

		public static readonly object PointerPressed = RoutedEventFlag.PointerPressed;
		public static readonly object PointerReleased = RoutedEventFlag.PointerReleased;
		public static readonly object PointerEntered = RoutedEventFlag.PointerEntered;
		public static readonly object PointerExited = RoutedEventFlag.PointerExited;
		public static readonly object PointerMoved = RoutedEventFlag.PointerMoved;
		public static readonly object PointerCanceled = RoutedEventFlag.PointerCanceled;
		public static readonly object PointerCaptureLost = RoutedEventFlag.PointerCaptureLost;
		public static readonly object PointerWheelChanged = RoutedEventFlag.PointerWheelChanged;

		public static readonly object KeyDown = RoutedEventFlag.KeyDown;
		public static readonly object KeyUp = RoutedEventFlag.KeyUp;

		public static readonly object GettingFocus = RoutedEventFlag.GettingFocus;
		public static readonly object GotFocus = RoutedEventFlag.GotFocus;
		public static readonly object LosingFocus = RoutedEventFlag.LosingFocus;
		public static readonly object LostFocus = RoutedEventFlag.LostFocus;
		public static readonly object NoFocusCandidateFound = RoutedEventFlag.NoFocusCandidateFound;
		public static readonly object BringIntoViewRequested = RoutedEventFlag.BringIntoViewRequested;

		public static readonly object DragStarting = RoutedEventFlag.DragStarting;
		public static readonly object DragEnter = RoutedEventFlag.DragEnter;
		public static readonly object DragLeave = RoutedEventFlag.DragLeave;
		public static readonly object DragOver = RoutedEventFlag.DragOver;
		public static readonly object Drop = RoutedEventFlag.Drop;
		public static readonly object DropCompleted = RoutedEventFlag.DropCompleted;

		public static readonly object ManipulationStarting = RoutedEventFlag.ManipulationStarting;
		public static readonly object ManipulationStarted = RoutedEventFlag.ManipulationStarted;
		public static readonly object ManipulationDelta = RoutedEventFlag.ManipulationDelta;
		public static readonly object ManipulationInertiaStarting = RoutedEventFlag.ManipulationInertiaStarting;
		public static readonly object ManipulationCompleted = RoutedEventFlag.ManipulationCompleted;

		public static readonly object Tapped = RoutedEventFlag.Tapped;
		public static readonly object DoubleTapped = RoutedEventFlag.DoubleTapped;
		public static readonly object RightTapped = RoutedEventFlag.RightTapped;
		public static readonly object Holding = RoutedEventFlag.Holding;
	}

	public static object Box(bool value) => value ? BooleanBoxes.BoxedTrue : BooleanBoxes.BoxedFalse;

	public static object Box(double value)
	{
		// https://github.com/dotnet/roslyn/blob/17dcec138afd78a265be020ef8ca0e22a254aa88/src/Compilers/Core/Portable/Collections/Boxes.cs#L87
		// There are many representations of zero in floating point.
		// Use the boxed value only if the bit pattern is all zeros.
		return BitConverter.DoubleToInt64Bits(value) == 0 ? Boxes.DoubleBoxes.Zero : value;
	}

	public static object Box(int value) => value switch
	{
		// Keep the specialized integers in sync with BoxingDiagnosticAnalyzer
		-1 => IntegerBoxes.NegativeOne,
		0 => IntegerBoxes.Zero,
		1 => IntegerBoxes.One,
		_ => value,
	};

	public static object Box(RoutedEventFlag value) => value switch
	{
		RoutedEventFlag.None => RoutedEventFlagBoxes.None,
		RoutedEventFlag.PointerPressed => RoutedEventFlagBoxes.PointerPressed,
		RoutedEventFlag.PointerReleased => RoutedEventFlagBoxes.PointerReleased,
		RoutedEventFlag.PointerEntered => RoutedEventFlagBoxes.PointerEntered,
		RoutedEventFlag.PointerExited => RoutedEventFlagBoxes.PointerExited,
		RoutedEventFlag.PointerMoved => RoutedEventFlagBoxes.PointerMoved,
		RoutedEventFlag.PointerCanceled => RoutedEventFlagBoxes.PointerCanceled,
		RoutedEventFlag.PointerCaptureLost => RoutedEventFlagBoxes.PointerCaptureLost,
		RoutedEventFlag.PointerWheelChanged => RoutedEventFlagBoxes.PointerWheelChanged,
		RoutedEventFlag.KeyDown => RoutedEventFlagBoxes.KeyDown,
		RoutedEventFlag.KeyUp => RoutedEventFlagBoxes.KeyUp,
		RoutedEventFlag.GettingFocus => RoutedEventFlagBoxes.GettingFocus,
		RoutedEventFlag.GotFocus => RoutedEventFlagBoxes.GotFocus,
		RoutedEventFlag.LosingFocus => RoutedEventFlagBoxes.LosingFocus,
		RoutedEventFlag.LostFocus => RoutedEventFlagBoxes.LostFocus,
		RoutedEventFlag.NoFocusCandidateFound => RoutedEventFlagBoxes.NoFocusCandidateFound,
		RoutedEventFlag.BringIntoViewRequested => RoutedEventFlagBoxes.BringIntoViewRequested,
		RoutedEventFlag.DragStarting => RoutedEventFlagBoxes.DragStarting,
		RoutedEventFlag.DragEnter => RoutedEventFlagBoxes.DragEnter,
		RoutedEventFlag.DragLeave => RoutedEventFlagBoxes.DragLeave,
		RoutedEventFlag.DragOver => RoutedEventFlagBoxes.DragOver,
		RoutedEventFlag.Drop => RoutedEventFlagBoxes.Drop,
		RoutedEventFlag.DropCompleted => RoutedEventFlagBoxes.DropCompleted,
		RoutedEventFlag.ManipulationStarting => RoutedEventFlagBoxes.ManipulationStarting,
		RoutedEventFlag.ManipulationStarted => RoutedEventFlagBoxes.ManipulationStarted,
		RoutedEventFlag.ManipulationDelta => RoutedEventFlagBoxes.ManipulationDelta,
		RoutedEventFlag.ManipulationInertiaStarting => RoutedEventFlagBoxes.ManipulationInertiaStarting,
		RoutedEventFlag.ManipulationCompleted => RoutedEventFlagBoxes.ManipulationCompleted,
		RoutedEventFlag.Tapped => RoutedEventFlagBoxes.Tapped,
		RoutedEventFlag.DoubleTapped => RoutedEventFlagBoxes.DoubleTapped,
		RoutedEventFlag.RightTapped => RoutedEventFlagBoxes.RightTapped,
		RoutedEventFlag.Holding => RoutedEventFlagBoxes.Holding,
		_ => value,
	};
}
