using System;
using System.Diagnostics;
using Windows.Foundation;
using Windows.System;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Uno.Extensions;
using Uno.UI.Extensions;
using Uno.UI.Helpers.WinUI;
using Uno.UI.Xaml.Core;

using Microsoft.UI.Input;
using PointerDeviceType = Microsoft.UI.Input.PointerDeviceType;

namespace Microsoft.UI.Xaml.Controls;

internal sealed partial class TextBoxCore
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
	// The caret index the press-and-hold caret drag started at, to tell a real drag from a hold in place on release.
	private int _touchCaretDragStartIndex;
	// Whether the selection flyout was open when the current touch went down. The press dismisses it, so the
	// release needs this for a tap on the iOS caret to toggle the flyout rather than always re-open it.
	private bool _selectionFlyoutOpenAtPress;
	// Max distance, in DIPs, between a tap and the caret for the tap to count as "on the caret" (iOS).
	private const double CaretTapTolerance = 12;
	// On iOS, a tap that may be followed by another (on the caret, or a double-tap) opens the flyout only once the
	// multi-tap window has passed (see QueueDeferredTouchFlyout).
	private DispatcherTimer _deferredTouchFlyoutTimer;
	private Point _deferredTouchFlyoutPosition;
	private bool _deferredTouchFlyoutForSelection;
	// Set when a handle tap was the second tap of an iOS multi-tap: its flyout waits out the multi-tap window too.
	private bool _deferGripperTapFlyout;
	// iOS: a tap on a selection handle while the menu showed only hides it (see OnGripperTapped).
	private bool _suppressGripperTapFlyout;
	// How far apart, in DIPs, successive touch taps may land and still make a multi-tap under the iOS convention. A
	// finger drifts well past the gesture recognizer's 10 DIP tap range between the taps of a double/triple-tap.
	private const double IOSTouchMultiTapSlop = 30;
	// Under the iOS convention the press-and-hold acts after UIKit's 0.5 s, not the gesture recognizer's 0.8 s Holding.
	private static readonly TimeSpan IOSHoldDelay = TimeSpan.FromMilliseconds(500);
	private DispatcherTimer _iosHoldTimer;
	private Point _iosHoldRootPoint;
	// Set once the iOS hold timer acted on the current touch, so the later Holding (ContextRequested) is ignored.
	private bool _iosHoldFired;

	internal void OnPointerMoved(PointerRoutedEventArgs e)
	{
		e.Handled = true;

		// A finger that moves away before the iOS hold delay is a pan or a scroll, not a press-and-hold.
		if (_iosHoldTimer?.IsEnabled == true
			&& e.Pointer.PointerDeviceType == PointerDeviceType.Touch
			&& GestureRecognizer.IsOutOfTapRange(_iosHoldRootPoint, e.GetCurrentPoint(null).Position))
		{
			_iosHoldTimer.Stop();
		}

		if (!HasPointerCapture)
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
				UpdateCaretLoupe(index);
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

	internal void OnRightTapped(RightTappedRoutedEventArgs e)
	{
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
	private bool IsTouchMultiTap(PointerPoint previous, PointerPoint current)
		=> previous.PointerDeviceType == PointerDeviceType.Touch
			&& current.PointerDeviceType == PointerDeviceType.Touch
			&& current.Timestamp - previous.Timestamp <= GestureRecognizer.MultiTapMaxDelayMicroseconds
			&& (TouchSelectionConvention == TouchTextSelectionConvention.iOS
				? Math.Abs(current.Position.X - previous.Position.X) <= IOSTouchMultiTapSlop
					&& Math.Abs(current.Position.Y - previous.Position.Y) <= IOSTouchMultiTapSlop
				: !GestureRecognizer.IsOutOfTapRange(previous.Position, current.Position));

	partial void OnPointerPressedPartial(PointerRoutedEventArgs args)
	{
		_isPressed = true;
		TrySetCurrentlyTyping(false);

		var currentPoint = args.GetCurrentPoint(null);
		if (args.Pointer.PointerDeviceType == PointerDeviceType.Touch)
		{
			// We handle touch on the PointerReleased end, but count repeated taps here (mirroring the
			// mouse multi-tap path) so a touch double-tap can select a word on release.
			var repeatedPresses = _lastPointerDown.point is { } previous && IsTouchMultiTap(previous, currentPoint)
				? _lastPointerDown.repeatedPresses + 1
				: 0;
			_lastPointerDown = (currentPoint, repeatedPresses);
			_selectionFlyoutOpenAtPress = SelectionFlyout?.IsOpen == true;
			_iosHoldFired = false;
			if (TouchSelectionConvention == TouchTextSelectionConvention.iOS)
			{
				StartIOSHoldTimer(currentPoint.Position);
			}
			// A new touch is either the next tap of a multi-tap or another gesture: drop any flyout still waiting on it.
			CancelDeferredTouchFlyout();
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

		_iosHoldTimer?.Stop();

		if (_touchCaretDrag)
		{
			// End the iOS caret-drag; OnPointerMoved has already positioned the caret at the finger.
			// The framework releases the touch capture on pointer-up, so no explicit release is needed here.
			_touchCaretDrag = false;
			EndCaretLoupe();

			// Native iOS: lifting the finger after dragging the caret just leaves it there. Only a hold in place (the
			// caret never moved) brings up the edit menu at the caret.
			if (SelectionLength == 0 && SelectionStart == _touchCaretDragStartIndex)
			{
				QueueUpdateSelectionFlyoutVisibility(PointerDeviceType.Touch, args.GetCurrentPoint(Owner).Position, allowEmptySelection: true);
			}
			return;
		}

		var touchHoldTime = args.GetCurrentPoint(null).Timestamp - _lastPointerDown.point.Timestamp;

		if (_iosHoldFired || touchHoldTime >= GestureRecognizer.HoldMinDelayMicroseconds)
		{
			// context menu should have already been opened through UIElement-level ContextRequested handling
			// (or, on iOS, the hold was already handled by the hold timer).
			return;
		}

		// Touch tap
		var isMobileMultiTap = TouchSelectionConvention != TouchTextSelectionConvention.Desktop && _lastPointerDown.repeatedPresses >= 1;

		if (Text.IsNullOrEmpty())
		{
			if (isMobileMultiTap)
			{
				HandleEmptyTextTouchGesture(args.GetCurrentPoint(Owner).Position);
			}
			else if (TouchSelectionConvention == TouchTextSelectionConvention.iOS && wasFocused && !_selectionFlyoutOpenAtPress)
			{
				// Native iOS: tapping the caret of a focused empty field shows the edit menu (Paste); tapping it again
				// while the menu shows just hides it.
				TouchTapAt(0);
				QueueDeferredTouchFlyout(args.GetCurrentPoint(Owner).Position, forSelection: false);
			}
			else if (TouchSelectionConvention != TouchTextSelectionConvention.Desktop)
			{
				// A single tap in an empty field has no word to select, but must still place the caret (and Android's
				// insertion handle) so the field shows where typing will go - and so the handle can open the flyout.
				TouchTapAt(0);
			}

			return;
		}

		var displayBlockPoint = args.GetCurrentPoint(TextBoxView.DisplayBlock).Position;
		if (isMobileMultiTap)
		{
			if (TouchSelectionConvention == TouchTextSelectionConvention.iOS && _lastPointerDown.repeatedPresses >= 2)
			{
				// Native iOS: a triple-tap selects the paragraph under the tap.
				TouchSelectParagraph(displayBlockPoint);
			}
			else
			{
				// Native iOS/Android: a double-tap selects the word under the tap.
				TouchSelectWord(displayBlockPoint);

				if (TouchSelectionConvention == TouchTextSelectionConvention.iOS)
				{
					// A triple-tap may follow: hold the flyout until it can't (see QueueDeferredTouchFlyout).
					QueueDeferredTouchFlyout(args.GetCurrentPoint(Owner).Position, forSelection: true);
					return;
				}
			}
		}
		else if (TouchSelectionConvention == TouchTextSelectionConvention.iOS && wasFocused && IsOnCaret(displayBlockPoint))
		{
			// Native iOS: tapping the caret toggles the edit menu instead of moving the caret.
			if (!_selectionFlyoutOpenAtPress)
			{
				QueueDeferredTouchFlyout(args.GetCurrentPoint(Owner).Position, forSelection: false);
			}
			return;
		}
		else if (TouchSelectionConvention == TouchTextSelectionConvention.iOS
			&& wasFocused
			&& CaretMode == CaretDisplayMode.CaretWithThumbsBothEndsShowing
			&& IsInSelection(displayBlockPoint))
		{
			// Native iOS: tapping the selection toggles the edit menu instead of collapsing it to a caret. The menu
			// waits out the multi-tap window, so a double-tap inside the selection still selects the word.
			// Only a touch selection (its handles showing) does this: a selection the app made in code, such as a
			// select-all on focus, still collapses to a caret at the tap, or the caret could never be placed in it.
			if (!_selectionFlyoutOpenAtPress)
			{
				QueueDeferredTouchFlyout(args.GetCurrentPoint(Owner).Position, forSelection: true);
			}
			return;
		}
		else
		{
			TouchTap(displayBlockPoint, wasFocused);
		}
		// Ported from: microsoft-ui-xaml2/src/dxaml/xcp/core/native/text/Controls/TextBoxBase.cpp (line 2088)
		// OnPointerReleased - queue SelectionFlyout visibility update after pointer release
		QueueUpdateSelectionFlyoutVisibility(PointerDeviceType.Touch, args.GetCurrentPoint(Owner).Position);
	}

	// Native iOS/Android pop the text flyout (Paste) over an empty field on a double-tap or a long-press. There is
	// no word to select there, so place the caret and let the flyout carry the gesture.
	private void HandleEmptyTextTouchGesture(Point textBoxPoint)
	{
		TouchTapAt(0);

		// A TextBox always carries Select All in the touch primary bar, but a PasswordBox keeps its Select All in the
		// overflow (and only with a password), so an empty one with an empty clipboard has no primary command at all.
		// Opening the flyout then would only flash an empty popup before it self-hides.
		if (SelectionFlyout is TextCommandBarFlyout flyout && !flyout.HasTouchPrimaryCommandsFor(Owner))
		{
			return;
		}

		QueueUpdateSelectionFlyoutVisibility(PointerDeviceType.Touch, textBoxPoint, allowEmptySelection: true);
	}

	private void TouchTap(Point point, bool wasFocused)
		=> TouchTapAt(Math.Max(0, TextBoxView.DisplayBlock.ParsedText.GetIndexAt(point, true, true)));

	private void TouchTapAt(int index)
	{
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
			default: // Desktop
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

	// The first tap of a double-tap usually lands on the caret, and a double-tap can grow into a triple-tap. Opening
	// the flyout right away would make it light-dismiss on the next tap, which then never reaches the TextBox and the
	// multi-tap is lost. UIKit waits for the next tap to fail before showing the menu; so do we: the flyout opens once
	// the multi-tap window has passed with no new touch (OnPointerPressedPartial cancels it).
	private void QueueDeferredTouchFlyout(Point textBoxPoint, bool forSelection)
	{
		_deferredTouchFlyoutPosition = textBoxPoint;
		_deferredTouchFlyoutForSelection = forSelection;

		if (_deferredTouchFlyoutTimer is null)
		{
			_deferredTouchFlyoutTimer = new DispatcherTimer { Interval = TimeSpan.FromMicroseconds(GestureRecognizer.MultiTapMaxDelayMicroseconds) };
			_deferredTouchFlyoutTimer.Tick += OnDeferredTouchFlyoutTimerTick;
		}

		_deferredTouchFlyoutTimer.Stop();
		_deferredTouchFlyoutTimer.Start();
	}

	private void CancelDeferredTouchFlyout() => _deferredTouchFlyoutTimer?.Stop();

	private void OnDeferredTouchFlyoutTimerTick(object sender, object e)
	{
		_deferredTouchFlyoutTimer?.Stop();

		if (FocusState == FocusState.Unfocused)
		{
			return;
		}

		if (_deferredTouchFlyoutForSelection)
		{
			// The word selected by the double-tap.
			if (SelectionLength > 0)
			{
				QueueUpdateSelectionFlyoutVisibility(PointerDeviceType.Touch, _deferredTouchFlyoutPosition);
			}
		}
		else if (SelectionLength == 0)
		{
			// A lone tap on the caret.
			if (Text.IsNullOrEmpty())
			{
				HandleEmptyTextTouchGesture(_deferredTouchFlyoutPosition);
			}
			else
			{
				QueueUpdateSelectionFlyoutVisibility(PointerDeviceType.Touch, _deferredTouchFlyoutPosition, allowEmptySelection: true);
			}
		}
	}

	private bool IsOnCaret(Point displayBlockPoint)
	{
		if (SelectionLength != 0)
		{
			return false;
		}

		var caretRect = TextBoxView.DisplayBlock.ParsedText.GetRectForIndex(SelectionStart);
		return Math.Abs(displayBlockPoint.X - caretRect.Left) <= CaretTapTolerance
			&& displayBlockPoint.Y >= caretRect.Top - CaretTapTolerance
			&& displayBlockPoint.Y <= caretRect.Bottom + CaretTapTolerance;
	}

	private bool IsInSelection(Point displayBlockPoint)
	{
		if (SelectionLength == 0)
		{
			return false;
		}

		var index = TextBoxView.DisplayBlock.ParsedText.GetIndexAt(displayBlockPoint, false, true);
		return index >= SelectionStart && index < SelectionStart + SelectionLength;
	}

	// Native iOS triple-tap: select the paragraph (the text between line breaks) under the tap, without its
	// trailing line break.
	private void TouchSelectParagraph(Point point)
		=> TouchSelectParagraphAt(TextBoxView.DisplayBlock.ParsedText.GetIndexAt(point, true, true));

	private void TouchSelectParagraphAt(int index)
	{
		var text = Text;
		if (text.Length == 0)
		{
			return;
		}

		index = Math.Clamp(index, 0, text.Length - 1);
		var start = StartOfLine(index);
		var end = EndOfLine(index); // index of the line break, or of the last character when there is none
		var length = (end < text.Length && text[end] == '\r' ? end : end + 1) - start;

		Select(start, Math.Max(0, length));
		CaretMode = CaretDisplayMode.CaretWithThumbsBothEndsShowing;
	}

	private void TouchSelectWord(Point point)
		=> TouchSelectWordAt(Math.Max(0, TextBoxView.DisplayBlock.ParsedText.GetIndexAt(point, true, true)));

	private void TouchSelectWordAt(int index)
	{
		var displayBlock = TextBoxView.DisplayBlock;
		var chunk = displayBlock.ParsedText.GetWordAt(index, true);

		// GetWordAt bundles the trailing space into the word chunk; native iOS/Android select just the word,
		// so trim it off (but keep a whitespace-only chunk intact).
		var text = displayBlock.Text;
		var length = chunk.length;
		while (length > 0 && text[chunk.start + length - 1] == ' ')
		{
			length--;
		}
		if (length == 0)
		{
			length = chunk.length;
		}

		Select(chunk.start, length); // touch selection doesn't go backwards (no "negative length")
		CaretMode = CaretDisplayMode.CaretWithThumbsBothEndsShowing;
	}

	// On iOS/Android a touch-and-hold does native text selection instead of opening a context menu:
	// Android selects the word under the press (the selection toolbar then appears via the selection
	// flyout); iOS starts dragging the caret, or keeps a selection held inside it and shows the flyout;
	// an empty field has neither, and just opens the flyout.
	// Mouse/pen right-click and the Desktop convention keep the default context flyout.
	// Returns whether the gesture was consumed; the host falls back to base handling when it wasn't.
	internal bool OnContextRequestedImpl(ContextRequestedEventArgs args)
	{
		if (args.IsTouchInput
			&& TouchSelectionConvention != TouchTextSelectionConvention.Desktop
			&& args.TryGetPosition(TextBoxView.DisplayBlock, out var displayBlockPoint))
		{
			args.TryGetPosition(Owner, out var textBoxPoint);

			if (TouchSelectionConvention == TouchTextSelectionConvention.iOS)
			{
				// The hold timer usually got there first (0.5 s); this Holding (0.8 s) only acts when it didn't.
				_iosHoldTimer?.Stop();
				if (!_iosHoldFired)
				{
					_iosHoldFired = true;
					HandleTouchHold_iOS(displayBlockPoint, textBoxPoint);
				}
			}
			else if (Text.IsNullOrEmpty())
			{
				// Neither convention has anything to select or to drag the caret through in an empty field.
				HandleEmptyTextTouchGesture(textBoxPoint);
			}
			else
			{
				// Android
				TouchSelectWord(displayBlockPoint);
				QueueUpdateSelectionFlyoutVisibility(PointerDeviceType.Touch, textBoxPoint);
			}

			// suppress the default context flyout on iOS/Android
			args.Handled = true;

			// We handled the hold without opening a context menu, so don't let a later HoldingState.Canceled
			// (finger moves during the caret-drag / after word-select) spuriously cancel a non-existent menu.
			args.PreventContextMenuOnHolding = true;

			return true;
		}

		return false;
	}

	// iOS press-and-hold: an empty field opens the flyout; holding a selection keeps it and shows the flyout;
	// anywhere else the caret starts following the finger.
	private void HandleTouchHold_iOS(Point displayBlockPoint, Point textBoxPoint)
	{
		if (Text.IsNullOrEmpty())
		{
			HandleEmptyTextTouchGesture(textBoxPoint);
		}
		else if (IsInSelection(displayBlockPoint))
		{
			// Native iOS: holding a selection keeps it and shows the edit menu.
			QueueUpdateSelectionFlyoutVisibility(PointerDeviceType.Touch, textBoxPoint);
		}
		else
		{
			BeginTouchCaretDrag(displayBlockPoint);
		}
	}

	private void StartIOSHoldTimer(Point rootPoint)
	{
		_iosHoldRootPoint = rootPoint;

		if (_iosHoldTimer is null)
		{
			_iosHoldTimer = new DispatcherTimer { Interval = IOSHoldDelay };
			_iosHoldTimer.Tick += OnIOSHoldTimerTick;
		}

		_iosHoldTimer.Stop();
		_iosHoldTimer.Start();
	}

	private void OnIOSHoldTimerTick(object sender, object e)
	{
		_iosHoldTimer?.Stop();

		// Still the same untouched press: not released, not taken over by a scrolling ancestor.
		if (!_isPressed || _iosHoldFired || IsAncestorScrollViewerManipulating())
		{
			return;
		}

		if (TextBoxView?.DisplayBlock is not { } displayBlock
			|| displayBlock.TransformToVisual(null).Inverse is not { } toDisplayBlock
			|| Owner.TransformToVisual(null).Inverse is not { } toOwner)
		{
			return;
		}

		_iosHoldFired = true;
		HandleTouchHold_iOS(toDisplayBlock.TransformPoint(_iosHoldRootPoint), toOwner.TransformPoint(_iosHoldRootPoint));
	}

	private bool IsAncestorScrollViewerManipulating()
	{
		foreach (var parent in Owner.GetAllParents(includeCurrent: false))
		{
			if (parent is ScrollViewer { IsInManipulation: true })
			{
				return true;
			}
		}

		return false;
	}

	// iOS long-press: place the caret at the press point and capture the pointer so the caret follows
	// the finger (see OnPointerMoved), with the platform magnifier where there is one (TextBoxCore.Loupe.cs).
	private void BeginTouchCaretDrag(Point displayBlockPoint)
	{
		var index = Math.Max(0, TextBoxView.DisplayBlock.ParsedText.GetIndexAt(displayBlockPoint, true, true));
		Select(index, 0);
		_touchCaretDragStartIndex = index;
		CaretMode = CaretDisplayMode.ThumblessCaretShowing;
		// Only enter caret-drag mode if we actually hold the pointer; without capture OnPointerMoved can't track
		// the finger, so leaving the flag set would make OnPointerReleasedPartial skip normal touch-release handling.
		// The finger usually slides just below the text, which a parent ScrollViewer would otherwise take as a scroll:
		// it would cancel the pointer and end the drag. Keep the touch for the caret, as Thumb and Slider do
		// (PreventDirectManipulation for native runtimes, CancelDirectManipulations for the managed one).
		_touchCaretDrag = PointerRoutedEventArgs.LastPointerEvent?.Pointer is { } pointer
			&& (_host.Owner.CapturePointer(pointer, options: PointerCaptureOptions.PreventDirectManipulation) is PointerCaptureResult.Added or PointerCaptureResult.AlreadyCaptured
				|| HasPointerCapture);

		if (_touchCaretDrag)
		{
			_host.Owner.CancelDirectManipulations();
			UpdateCaretLoupe(index);
		}
	}

	partial void OnPointerCaptureLostPartial(PointerRoutedEventArgs e)
	{
		_isPressed = false;
		_mouseMultiTapChunk = null;
		_touchCaretDrag = false;
		_iosHoldTimer?.Stop();
		EndCaretLoupe();
	}

	internal void OnDoubleTapped(DoubleTappedRoutedEventArgs args)
	{
		args.Handled = true;
	}
}
