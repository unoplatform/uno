using System;
using System.Diagnostics;
using Windows.Foundation;
using Windows.System;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Uno.Extensions;
using Uno.UI.Helpers.WinUI;

using Microsoft.UI.Input;
using PointerDeviceType = Microsoft.UI.Input.PointerDeviceType;

namespace Microsoft.UI.Xaml.Controls;

public partial class TextBox
{
	/// <summary>
	/// point is null before first press. repeatedPresses counts consecutive multi-taps for both Mouse
	/// (see OnPointerPressedPartial) and Touch (OnPointerPressedPartial / OnGripperTapped).
	/// </summary>
	private (PointerPoint point, int repeatedPresses) _lastPointerDown;
	private (int start, int length, bool tripleTap)? _mouseMultiTapChunk;
	// this is necessary because we can receive a PointerReleased without a PointerPressed (e.g. clicking on the
	// TextBox while the context menu is open to dismiss it). We want to ignore such PointerPressed's.
	private bool _isPressed;
	// True while an iOS-convention touch caret-drag is in progress (started by a long-press): the caret
	// follows the finger until release. See BeginTouchCaretDrag / OnContextRequestedImpl.
	private bool _touchCaretDrag;

	protected override void OnPointerMoved(PointerRoutedEventArgs e)
	{
		base.OnPointerMoved(e);
		e.Handled = true;

		if (!_isSkiaTextBox || !HasPointerCapture)
		{
			return;
		}

		if (e.Pointer.PointerDeviceType == PointerDeviceType.Touch)
		{
			if (_touchCaretDrag)
			{
				// iOS caret dragging (started by a long-press): the caret follows the finger.
				var point = e.GetCurrentPoint(TextBoxView.DisplayBlock);
				var index = Math.Max(0, TextBoxView.DisplayBlock.ParsedText.GetIndexAt(point.Position, true, true));
				Select(index, 0);
				CaretMode = CaretDisplayMode.ThumblessCaretShowing;
			}
			// Otherwise do nothing: moving while pressing the caret thumb or stem moves it (handled by the
			// gripper presenter); any other plain touch move does nothing.
		}
		else
		{
			var displayBlock = TextBoxView.DisplayBlock;
			var point = e.GetCurrentPoint(displayBlock);
			var index = Math.Max(0, TextBoxView.DisplayBlock.ParsedText.GetIndexAt(point.Position, false, true));
			if (_mouseMultiTapChunk is { } mtc)
			{
				(int start, int length) chunk;
				if (mtc.tripleTap)
				{
					chunk = (StartOfLine(index), EndOfLine(index) + 1 - StartOfLine(index));
				}
				else
				{
					chunk = TextBoxView.DisplayBlock.ParsedText.GetWordAt(index, true);
				}

				if (chunk.start < mtc.start)
				{
					var start = mtc.start + mtc.length;
					var end = chunk.start;
					SelectInternal(start, end - start);
				}
				else if (chunk.start + chunk.length >= mtc.start + mtc.length)
				{
					var start = mtc.start;
					var end = chunk.start + chunk.length;
					SelectInternal(start, end - start);
				}
			}
			else
			{
				var selectionInternalStart = _selection.selectionEndsAtTheStart ? _selection.start + _selection.length : _selection.start;
				SelectInternal(selectionInternalStart, index - selectionInternalStart);
			}
		}
	}

	protected override void OnRightTapped(RightTappedRoutedEventArgs e)
	{
		base.OnRightTapped(e);

		var displayBlock = TextBoxView.DisplayBlock;
		var position = e.GetPosition(displayBlock);

		var index = Math.Max(0, displayBlock.ParsedText.GetIndexAt(position, true, true));
		if (index < SelectionStart || index >= SelectionStart + SelectionLength)
		{
			// Right tapping should move the caret to the current pointer location if outside the selection
			Select(index, 0);
		}
	}

	private static bool IsMultiTapGesture((ulong id, ulong ts, Point position) previousTap, PointerPoint down)
	{
		var currentId = down.PointerId;
		var currentTs = down.Timestamp;
		var currentPosition = down.Position;

		return previousTap.id == currentId
			&& currentTs - previousTap.ts <= GestureRecognizer.MultiTapMaxDelayMicroseconds
			&& !GestureRecognizer.IsOutOfTapRange(previousTap.position, currentPosition);
	}

	// Touch taps can't reuse IsMultiTapGesture: successive touch presses get different pointer ids,
	// so we compare only the timing and distance between the two taps.
	private static bool IsTouchMultiTap(PointerPoint previous, PointerPoint current)
		=> current.Timestamp - previous.Timestamp <= GestureRecognizer.MultiTapMaxDelayMicroseconds
			&& !GestureRecognizer.IsOutOfTapRange(previous.Position, current.Position);

	partial void OnPointerPressedPartial(PointerRoutedEventArgs args)
	{
		_isPressed = true;
		TrySetCurrentlyTyping(false);

		if (!_isSkiaTextBox)
		{
			return;
		}

		var currentPoint = args.GetCurrentPoint(null);
		if (args.Pointer.PointerDeviceType == PointerDeviceType.Touch)
		{
			// We handle touch on the PointerReleased end, but count repeated taps here (mirroring the
			// mouse multi-tap path) so a touch double-tap can select a word on release.
			var repeatedPresses = _lastPointerDown.point is { } previous && IsTouchMultiTap(previous, currentPoint)
				? _lastPointerDown.repeatedPresses + 1
				: 0;
			_lastPointerDown = (currentPoint, repeatedPresses);
			// Dismiss the selection flyout on press; the gesture re-shows it (tap) or yields to the context menu (hold).
			DismissSelectionFlyoutForPointerPress();
		}
		else if (!currentPoint.Properties.IsRightButtonPressed) // Mouse (a pen is considered a mouse for now)
		{
			var displayBlock = TextBoxView.DisplayBlock;
			var index = Math.Max(0, displayBlock.ParsedText.GetIndexAt(args.GetCurrentPoint(displayBlock).Position, true, true));

			if (currentPoint.Properties.IsLeftButtonPressed
				&& _lastPointerDown.point is { } p
				&& IsMultiTapGesture((p.PointerId, p.Timestamp, p.Position), currentPoint))
			{
				// multiple left presses

				if (_lastPointerDown.repeatedPresses == 1)
				{
					// triple tap

					var startOfLine = StartOfLine(index);
					Select(startOfLine, EndOfLine(index) + 1 - startOfLine);
					_mouseMultiTapChunk = (SelectionStart, SelectionLength, true);
					_lastPointerDown = (currentPoint, 2);
				}
				else // _lastPointerDown.repeatedPresses == 0 or 2
				{
					// double tap
					var chunk = TextBoxView.DisplayBlock.ParsedText.GetWordAt(index, true);
					Select(chunk.start, chunk.length);
					_mouseMultiTapChunk = (chunk.start, chunk.length, false);
					_lastPointerDown = (currentPoint, 1);
				}
			}
			else
			{
				// single click
				CaretMode = CaretDisplayMode.ThumblessCaretShowing;
				if ((args.KeyModifiers & VirtualKeyModifiers.Shift) != 0)
				{
					var selectionInternalStart = _selection.selectionEndsAtTheStart ? _selection.start + _selection.length : _selection.start;
					SelectInternal(selectionInternalStart, index - selectionInternalStart);
				}
				else
				{
					Select(index, 0);
				}
				_lastPointerDown = (currentPoint, 0);
			}
		}
	}

	partial void OnPointerReleasedPartial(PointerRoutedEventArgs args, bool wasFocused)
	{
		_mouseMultiTapChunk = null;

		if (!_isPressed)
		{
			// Released without a preceding Pressed: this is a pointer released from the context menu
			return;
		}
		_isPressed = false;

		if (args.Pointer.PointerDeviceType is not PointerDeviceType.Touch)
		{
			// Mouse is handled on the PointerPressed side
			return;
		}

		if (_touchCaretDrag)
		{
			// End the iOS caret-drag; OnPointerMoved has already positioned the caret at the finger.
			_touchCaretDrag = false;
			return;
		}

		var touchHoldTime = args.GetCurrentPoint(null).Timestamp - _lastPointerDown.point.Timestamp;

		if (touchHoldTime >= GestureRecognizer.HoldMinDelayMicroseconds)
		{
			// context menu should have already been opened through UIElement-level ContextRequested handling.
			return;
		}
		else if (!Text.IsNullOrEmpty()) // Touch tap
		{
			var displayBlockPoint = args.GetCurrentPoint(TextBoxView.DisplayBlock).Position;
			if (TouchSelectionConvention != TouchTextSelectionConvention.Windows && _lastPointerDown.repeatedPresses >= 1)
			{
				// Native iOS/Android: a double-tap selects the word under the tap.
				TouchSelectWord(displayBlockPoint);
			}
			else
			{
				TouchTap(displayBlockPoint, wasFocused);
			}
			// Ported from: microsoft-ui-xaml2/src/dxaml/xcp/core/native/text/Controls/TextBoxBase.cpp (line 2088)
			// OnPointerReleased - queue SelectionFlyout visibility update after pointer release
			QueueUpdateSelectionFlyoutVisibility(PointerDeviceType.Touch, args.GetCurrentPoint(this).Position);
		}
	}

	private void TouchTap(Point point, bool wasFocused)
	{
		var index = Math.Max(0, TextBoxView.DisplayBlock.ParsedText.GetIndexAt(point, true, true));

		switch (TouchSelectionConvention)
		{
			case TouchTextSelectionConvention.Android:
				// A single tap places the caret with the single insertion handle; tapping inside an
				// existing selection collapses it to a caret at the tap (native Android).
				Select(index, 0);
				CaretMode = CaretDisplayMode.CaretWithThumbsOnlyEndShowing;
				break;
			case TouchTextSelectionConvention.iOS:
				// A single tap places a bare blinking caret (no handle); tapping inside a selection
				// collapses it to a caret at the tap (native iOS).
				Select(index, 0);
				CaretMode = CaretDisplayMode.ThumblessCaretShowing;
				break;
			default: // Windows
				var tappedChunk = TextBoxView.DisplayBlock.ParsedText.GetWordAt(index, true);
				var tappedInsideSelection = _selection.start <= index && index < _selection.start + _selection.length;
				if (tappedInsideSelection)
				{
					CaretMode = CaretDisplayMode.CaretWithThumbsBothEndsShowing;
				}
				else if (_selection.length == 0)
				{
					Select(tappedChunk.start, tappedChunk.length); // touch selection doesn't go backwards (no "negative length")
					CaretMode = CaretDisplayMode.CaretWithThumbsBothEndsShowing;
				}
				else // outside a selection
				{
					Select(tappedChunk.start, 0);
					CaretMode = CaretDisplayMode.CaretWithThumbsOnlyEndShowing;
				}
				break;
		}
	}

	private void TouchSelectWord(Point point)
	{
		var index = Math.Max(0, TextBoxView.DisplayBlock.ParsedText.GetIndexAt(point, true, true));
		var chunk = TextBoxView.DisplayBlock.ParsedText.GetWordAt(index, true);
		Select(chunk.start, chunk.length); // touch selection doesn't go backwards (no "negative length")
		CaretMode = CaretDisplayMode.CaretWithThumbsBothEndsShowing;
	}

	// On iOS/Android a touch-and-hold does native text selection instead of opening a context menu:
	// Android selects the word under the press (the selection toolbar then appears via the selection
	// flyout); iOS starts dragging the caret. Mouse/pen right-click and the Windows convention keep
	// the default context flyout.
	private protected override void OnContextRequestedImpl(ContextRequestedEventArgs args)
	{
		if (_isSkiaTextBox
			&& args.IsTouchInput
			&& TouchSelectionConvention != TouchTextSelectionConvention.Windows
			&& args.TryGetPosition(TextBoxView.DisplayBlock, out var displayBlockPoint))
		{
			switch (TouchSelectionConvention)
			{
				case TouchTextSelectionConvention.Android:
					TouchSelectWord(displayBlockPoint);
					args.TryGetPosition(this, out var textBoxPoint);
					QueueUpdateSelectionFlyoutVisibility(PointerDeviceType.Touch, textBoxPoint);
					break;
				case TouchTextSelectionConvention.iOS:
					BeginTouchCaretDrag(displayBlockPoint);
					break;
			}

			args.Handled = true; // suppress the default context flyout on iOS/Android
			return;
		}

		base.OnContextRequestedImpl(args);
	}

	// iOS long-press: place the caret at the press point and capture the pointer so the caret follows
	// the finger (see OnPointerMoved). Approximates the native magnifier without the zoomed loupe.
	private void BeginTouchCaretDrag(Point displayBlockPoint)
	{
		var index = Math.Max(0, TextBoxView.DisplayBlock.ParsedText.GetIndexAt(displayBlockPoint, true, true));
		Select(index, 0);
		CaretMode = CaretDisplayMode.ThumblessCaretShowing;
		_touchCaretDrag = true;
		if (PointerRoutedEventArgs.LastPointerEvent?.Pointer is { } pointer)
		{
			CapturePointer(pointer);
		}
	}

	partial void OnPointerCaptureLostPartial(PointerRoutedEventArgs e)
	{
		_isPressed = false;
		_mouseMultiTapChunk = null;
		_touchCaretDrag = false;
	}

	protected override void OnDoubleTapped(DoubleTappedRoutedEventArgs args)
	{
		base.OnDoubleTapped(args);
		args.Handled = true;
	}
}
