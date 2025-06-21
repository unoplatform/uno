using System;
using System.Diagnostics;
using Windows.Foundation;
using Windows.System;
using Microsoft.UI.Xaml.Input;
using Uno.Extensions;
using Uno.UI.Helpers.WinUI;

#if HAS_UNO_WINUI
using Microsoft.UI.Input;
using PointerDeviceType = Microsoft.UI.Input.PointerDeviceType;
#else
using Windows.UI.Input;
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;
#endif

namespace Microsoft.UI.Xaml.Controls;

public partial class TextBox
{
	/// <summary>
	/// point is null before first press. repeatedPresses is only valid if point.Pointer.PointerDeviceType
	/// is Mouse.
	/// </summary>
	private (PointerPoint point, int repeatedPresses) _lastPointerDown;
	private (int start, int length, bool tripleTap)? _mouseMultiTapChunk;
	// this is necessary because we can receive a PointerReleased without a PointerPressed (e.g. clicking on the
	// TextBox while the context menu is open to dismiss it). We want to ignore such PointerPressed's.
	private bool _isPressed;

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
			// do nothing whether the touch pointer is pressed on not.
			// Moving while pressing the caret thumb or stem will move it. Anything else won't do anything.
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
					chunk = FindChunkAt(index, true);
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

	// TODO: remove this context menu when TextCommandBarFlyout is implemented
	protected override void OnRightTapped(RightTappedRoutedEventArgs e)
	{
		base.OnRightTapped(e);
		e.Handled = true;

		var displayBlock = TextBoxView.DisplayBlock;
		var position = e.GetPosition(displayBlock);

		var index = Math.Max(0, displayBlock.ParsedText.GetIndexAt(position, true, true));
		if (index < SelectionStart || index >= SelectionStart + SelectionLength)
		{
			// Right tapping should move the caret to the current pointer location if outside the selection
			Select(index, 0);
		}

		OpenContextMenu(position);
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

	partial void OnPointerPressedPartial(PointerRoutedEventArgs args)
	{
		_isPressed = true;
		TrySetCurrentlyTyping(false);
		_contextMenu?.Close();

		if (!_isSkiaTextBox)
		{
			return;
		}

		var currentPoint = args.GetCurrentPoint(null);
		if (args.Pointer.PointerDeviceType == PointerDeviceType.Touch)
		{
			// we handle touch on the PointerReleased end
			_lastPointerDown = (currentPoint, 0);
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
					var chunk = FindChunkAt(index, true);
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

		if (!_isPressed || args.Pointer.PointerDeviceType is not PointerDeviceType.Touch)
		{
			// Mouse is handled on the PointerPressed side
			return;
		}

		_isPressed = false;

		if ((args.GetCurrentPoint(null).Timestamp - _lastPointerDown.point.Timestamp) >= GestureRecognizer.HoldMinDelayMicroseconds)
		{
			// Touch holding
			OpenContextMenu(args.GetCurrentPoint(this).Position);
		}
		else if (!Text.IsNullOrEmpty()) // Touch tap
		{
			TouchTap(args.GetCurrentPoint(TextBoxView.DisplayBlock).Position, wasFocused);
		}
	}

	private void TouchTap(Point point, bool wasFocused)
	{
		var index = Math.Max(0, TextBoxView.DisplayBlock.ParsedText.GetIndexAt(point, true, true));

		var tappedChunk = FindChunkAt(index, true);

		var tappedInsideSelection = _selection.start <= index && index < _selection.start + _selection.length;
		if (tappedInsideSelection && CaretMode != CaretDisplayMode.CaretWithThumbsBothEndsShowing)
		{
			CaretMode = CaretDisplayMode.CaretWithThumbsBothEndsShowing;
		}
		else if (_selection.length == 0 && FindChunkAt(_selection.start, true) is var currentChunk && currentChunk.start <= index && index < currentChunk.start + currentChunk.length)
		{
			Select(tappedChunk.start, tappedChunk.length); // touch selection doesn't go backwards (no "negative length")
			CaretMode = CaretDisplayMode.CaretWithThumbsBothEndsShowing;
		}
		else
		{
			var lastNonSpanCharIndex = Text[tappedChunk.start..(tappedChunk.start + tappedChunk.length)].IndexOf(' ');
			var rightEndIndex = lastNonSpanCharIndex == -1 ? tappedChunk.start + tappedChunk.length - 1 : tappedChunk.start + lastNonSpanCharIndex;
			var leftEndIndex = tappedChunk.start;
			var leftEnd = TextBoxView.DisplayBlock.ParsedText.GetRectForIndex(leftEndIndex);
			var rightEnd = TextBoxView.DisplayBlock.ParsedText.GetRectForIndex(rightEndIndex);

			var closerEnd = Math.Abs(point.X - leftEnd.Left) < Math.Abs(point.X - rightEnd.Right) ? leftEndIndex : rightEndIndex + 1;

			if (wasFocused) // If we were not focused before, caret should be initially thumbless.
			{
				CaretMode = CaretDisplayMode.CaretWithThumbsOnlyEndShowing;
			}

			Select(closerEnd, 0);
		}
	}

	partial void OnPointerCaptureLostPartial(PointerRoutedEventArgs e)
	{
		_isPressed = false;
		_mouseMultiTapChunk = null;
	}

	protected override void OnDoubleTapped(DoubleTappedRoutedEventArgs args)
	{
		base.OnDoubleTapped(args);
		args.Handled = true;
	}

	private void CaretOnPointerPressed(object sender, PointerRoutedEventArgs args)
	{
		args.Handled = true;

		var caret = (CaretWithStemAndThumb)sender;
		if (caret.CapturePointer(args.Pointer))
		{
			caret.SetStemVisible(true);
		}

		caret.LastPointerDown = args.GetCurrentPoint(null);
	}

	private void CaretOnPointerMoved(object sender, PointerRoutedEventArgs args)
	{
		var caret = (CaretWithStemAndThumb)sender;
		if (!caret.HasPointerCapture)
		{
			return;
		}
		args.Handled = true;

		var displayBlock = TextBoxView.DisplayBlock;
		var point = args.GetCurrentPoint(displayBlock).Position - new Point(0, (caret.Height - 16) / 2);
		var index = Math.Max(0, TextBoxView.DisplayBlock.ParsedText.GetIndexAt(point, false, true));

		if (_selection.length == 0)
		{
			Debug.Assert(caret == _selectionEndThumbfulCaret);
			Select(index, 0);
		}
		else
		{
			Debug.Assert(CaretMode == CaretDisplayMode.CaretWithThumbsBothEndsShowing);
			var (start, end) = (_selection.start, _selection.start + _selection.length);
			if (sender == _selectionStartThumbfulCaret)
			{
				start = index;
			}
			else
			{
				end = index;
			}

			if (start != end) // if start == end, we do nothing like WinUI. This means that the 2 carets won't be on top of one another
			{
				SelectInternal(start, end - start);

				if (end < start)
				{
					// If we're here this means that the "selection end caret" was dragging "behind" the "selection start caret".
					// We swap which caret we consider the "selection start caret" now that the "end caret" is actually before the
					// "start caret".
					(_selectionStartThumbfulCaret, _selectionEndThumbfulCaret) = (_selectionEndThumbfulCaret, _selectionStartThumbfulCaret);
				}
			}
		}

		UpdateScrolling(caret == _selectionEndThumbfulCaret);
	}

	private void CaretOnPointerReleased(object sender, PointerRoutedEventArgs e)
	{
		ClearCaretPointerState(sender, e);

		var caret = (CaretWithStemAndThumb)sender;

		var previous = caret.LastPointerDown;
		if (IsMultiTapGesture((previous.PointerId, previous.Timestamp, previous.Position), e.GetCurrentPoint(null)))
		{
			e.Handled = true;
			TouchTap(e.GetCurrentPoint(TextBoxView.DisplayBlock).Position, true);
		}
	}

	private void ClearCaretPointerState(object sender, PointerRoutedEventArgs args)
	{
		args.Handled = true;
		var caret = (CaretWithStemAndThumb)sender;
		caret.SetStemVisible(false);
		caret.ReleasePointerCaptures();
	}

	private void OpenContextMenu(Point p)
	{
		if (_isSkiaTextBox)
		{
			if (_contextMenu is null)
			{
				_contextMenu = new MenuFlyout();
				_contextMenu.Opened += (_, _) => UpdateDisplaySelection();

				_flyoutItems.Add(ContextMenuItem.Cut, new MenuFlyoutItem { Text = ResourceAccessor.GetLocalizedStringResource("TEXT_CONTEXT_MENU_CUT"), Command = new StandardUICommand(StandardUICommandKind.Cut) { Command = new TextBoxCommand(CutSelectionToClipboard) } });
				_flyoutItems.Add(ContextMenuItem.Copy, new MenuFlyoutItem { Text = ResourceAccessor.GetLocalizedStringResource("TEXT_CONTEXT_MENU_COPY"), Command = new StandardUICommand(StandardUICommandKind.Copy) { Command = new TextBoxCommand(CopySelectionToClipboard) } });
				_flyoutItems.Add(ContextMenuItem.Paste, new MenuFlyoutItem { Text = ResourceAccessor.GetLocalizedStringResource("TEXT_CONTEXT_MENU_PASTE"), Command = new StandardUICommand(StandardUICommandKind.Paste) { Command = new TextBoxCommand(PasteFromClipboard) } });
				_flyoutItems.Add(ContextMenuItem.Undo, new MenuFlyoutItem { Text = ResourceAccessor.GetLocalizedStringResource("TEXT_CONTEXT_MENU_UNDO"), Command = new StandardUICommand(StandardUICommandKind.Undo) { Command = new TextBoxCommand(Undo) } });
				_flyoutItems.Add(ContextMenuItem.Redo, new MenuFlyoutItem { Text = ResourceAccessor.GetLocalizedStringResource("TEXT_CONTEXT_MENU_REDO"), Command = new StandardUICommand(StandardUICommandKind.Redo) { Command = new TextBoxCommand(Redo) } });
				_flyoutItems.Add(ContextMenuItem.SelectAll, new MenuFlyoutItem { Text = ResourceAccessor.GetLocalizedStringResource("TEXT_CONTEXT_MENU_SELECT_ALL"), Command = new StandardUICommand(StandardUICommandKind.SelectAll) { Command = new TextBoxCommand(SelectAll) } });
			}

			_contextMenu.Items.Clear();

			var hasSelection = _selection.length > 0;

			if (!IsReadOnly && hasSelection)
			{
				_contextMenu.Items.Add(_flyoutItems[ContextMenuItem.Cut]);
			}

			if (hasSelection)
			{
				_contextMenu.Items.Add(_flyoutItems[ContextMenuItem.Copy]);
			}

			if (!IsReadOnly)
			{
				_contextMenu.Items.Add(_flyoutItems[ContextMenuItem.Paste]);
				if (CanUndo)
				{
					_contextMenu.Items.Add(_flyoutItems[ContextMenuItem.Undo]);
				}
				if (CanRedo)
				{
					_contextMenu.Items.Add(_flyoutItems[ContextMenuItem.Redo]);
				}
			}

			_contextMenu.Items.Add(_flyoutItems[ContextMenuItem.SelectAll]);

			_contextMenu.ShowAt(this, p);
		}
	}
}
