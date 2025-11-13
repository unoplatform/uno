#if __ANDROID__ || __APPLE_UIKIT__

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using static Microsoft.UI.Xaml.UIElement;

namespace Microsoft.UI.Xaml;

partial class UIElement
{
	private bool OnNativePointerEnter(PointerRoutedEventArgs args, BubblingContext ctx = default) => OnPointerEnter(args);

	private bool OnNativePointerDown(PointerRoutedEventArgs args) => OnPointerDown(args);

	// This is for iOS and Android which are not raising the Exit properly (due to native "implicit capture" when pointer is pressed),
	// and for which we have to re-compute / update the over state for each move.
	private bool OnNativePointerMoveWithOverCheck(PointerRoutedEventArgs args, bool isOver, BubblingContext ctx = default)
	{
		var handledInManaged = false;
		var isOverOrCaptured = ValidateAndUpdateCapture(args, isOver);

		// Note: The 'ctx' here is for the "Move", not the "WithOverCheck", so we don't use it to update the over state.
		//		 (i.e. even if the 'move' has been handled and is now flagged as 'IsInternal' -- so event won't be publicly raised unless handledEventToo --,
		//		 if we are crossing the boundaries of the element we should still raise the enter/exit publicly.)
		if (IsOver(args.Pointer) != isOver)
		{
			var argsWasHandled = args.Handled;
			args.Handled = false;
			handledInManaged |= SetOver(args, isOver, BubblingContext.Bubble);
			args.Handled = argsWasHandled;
		}

		if (!ctx.IsInternal && isOverOrCaptured)
		{
			// If this pointer was wrongly dispatched here (out of the bounds and not captured),
			// we don't raise the 'move' event

			args.Handled = false;
			handledInManaged |= RaisePointerEvent(PointerMovedEvent, args);
		}

		if (IsGestureRecognizerCreated)
		{
			var gestures = GestureRecognizer;
			gestures.ProcessMoveEvents(args.GetIntermediatePoints(this));
			if (gestures.IsDragging)
			{
				XamlRoot.GetCoreDragDropManager(XamlRoot).ProcessMoved(args);
			}
		}

		return handledInManaged;
	}

#if __APPLE_UIKIT__
	private bool OnNativePointerMove(PointerRoutedEventArgs args) => OnPointerMove(args);
#endif

	private bool OnNativePointerUp(PointerRoutedEventArgs args) => OnPointerUp(args);
	private bool OnNativePointerExited(PointerRoutedEventArgs args) => OnPointerExited(args);

	/// <summary>
	/// When the system cancel a pointer pressed, either
	/// 1. because the pointing device was lost/disconnected,
	/// 2. or the system detected something meaning full and will handle this pointer internally.
	/// This second case is the more common (e.g. ScrollViewer) and should be indicated using the <paramref name="isSwallowedBySystem"/> flag.
	/// </summary>
	/// <param name="isSwallowedBySystem">Indicates that the pointer was muted by the system which will handle it internally.</param>
	private bool OnNativePointerCancel(PointerRoutedEventArgs args, bool isSwallowedBySystem)
	{
		args.CanceledByDirectManipulation = isSwallowedBySystem;
		return OnPointerCancel(args);
	}
}
#endif
