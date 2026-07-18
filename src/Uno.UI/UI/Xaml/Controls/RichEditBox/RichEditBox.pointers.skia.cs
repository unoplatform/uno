#nullable enable

using System;
using Windows.Foundation;
using Windows.System;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Internal;
using PointerDeviceType = Microsoft.UI.Input.PointerDeviceType;

namespace Microsoft.UI.Xaml.Controls
{
	// Pointer-driven editing for RichEditBox on Skia.
	//
	// Covers the common mouse/pen/touch cases: click-to-place-caret, Shift+click to extend, press-and-
	// drag to select, and double-click/tap to select a word. It reuses the same hit-testing
	// (ParsedText.GetIndexAt / GetWordAt) and selection plumbing (SetInteractiveSelection) as the
	// keyboard layer, so caret/selection stay consistent across input modalities.
	//
	partial class RichEditBox
	{
		private bool _hasPointerCapture;
		private int _pointerSelectionAnchor;
		private int _pressedLinkIndex = -1;
		private PointerRoutedEventArgs? _processedPointerPressedArgs;
		private (PointerPoint? point, int repeatedPresses) _lastPointerDown;
		private (int start, int length, bool tripleTap)? _mouseMultiTapChunk;
		private bool _isPressed;
		private bool _wasFocusedOnPointerPressed;

		protected override void OnPointerEntered(PointerRoutedEventArgs e)
		{
			base.OnPointerEntered(e);
			_isPointerOver = true;
			UpdateVisualState();
		}

		protected override void OnPointerExited(PointerRoutedEventArgs e)
		{
			base.OnPointerExited(e);
			_isPointerOver = false;
			UpdateVisualState();
		}

		protected override void OnPointerPressed(PointerRoutedEventArgs e)
		{
			base.OnPointerPressed(e);
			if (!ReferenceEquals(_processedPointerPressedArgs, e))
			{
				ProcessPointerPressed(e);
			}
		}

		private void OnPointerPressedHandledEventsToo(object sender, PointerRoutedEventArgs e)
		{
			if (!ReferenceEquals(_processedPointerPressedArgs, e))
			{
				ProcessPointerPressed(e);
			}
		}

		private void ProcessPointerPressed(PointerRoutedEventArgs e)
		{
			_processedPointerPressedArgs = e;

			if (_textBoxView?.DisplayBlock is not { } displayBlock)
			{
				return;
			}

			_isPressed = true;
			_wasFocusedOnPointerPressed = FocusState != FocusState.Unfocused;
			var currentPoint = e.GetCurrentPoint(null);
			if (e.Pointer.PointerDeviceType == PointerDeviceType.Touch)
			{
				_pressedLinkIndex = Math.Max(0, displayBlock.ParsedText.GetIndexAt(e.GetCurrentPoint(displayBlock).Position, true, true));
				_lastPointerDown = (currentPoint, 0);
				DismissSelectionFlyoutForPointerPress();
				e.Handled = true;
				_hasPointerCapture = CapturePointer(e.Pointer);
				return;
			}

			// Right button is reserved for the context menu; let the base handling deal with it.
			if (currentPoint.Properties.IsRightButtonPressed)
			{
				_isPressed = false;
				return;
			}

			DismissSelectionFlyoutForPointerPress();
			e.Handled = true;

			var index = Math.Max(0, displayBlock.ParsedText.GetIndexAt(e.GetCurrentPoint(displayBlock).Position, true, true));
			_pressedLinkIndex = index;
			if (currentPoint.Properties.IsLeftButtonPressed
				&& _lastPointerDown.point is { } previous
				&& IsMultiTapGesture((previous.PointerId, previous.Timestamp, previous.Position), currentPoint))
			{
				if (_lastPointerDown.repeatedPresses == 1)
				{
					var chunk = GetLogicalLineChunk(index);
					SetInteractiveSelection(chunk.start, chunk.length);
					_mouseMultiTapChunk = (chunk.start, chunk.length, true);
					_lastPointerDown = (currentPoint, 2);
				}
				else
				{
					var chunk = displayBlock.ParsedText.GetWordAt(index, true);
					SetInteractiveSelection(chunk.start, chunk.length);
					_mouseMultiTapChunk = (chunk.start, chunk.length, false);
					_lastPointerDown = (currentPoint, 1);
				}
			}
			else if ((e.KeyModifiers & VirtualKeyModifiers.Shift) != 0)
			{
				// Extend the current selection from its fixed (non-caret) end to the pressed index.
				var anchor = _selection.selectionEndsAtTheStart ? _selection.start + _selection.length : _selection.start;
				_pointerSelectionAnchor = anchor;
				SetInteractiveSelection(anchor, index - anchor);
			}
			else
			{
				CaretMode = RichEditCaretDisplayMode.ThumblessCaretShowing;
				_pointerSelectionAnchor = index;
				SetInteractiveSelection(index, 0);
				_lastPointerDown = (currentPoint, 0);
			}

			if (FocusState == FocusState.Unfocused)
			{
				Focus(FocusState.Pointer);
			}

			_hasPointerCapture = CapturePointer(e.Pointer);
		}

		protected override void OnPointerMoved(PointerRoutedEventArgs e)
		{
			base.OnPointerMoved(e);

			if (!_hasPointerCapture || _textBoxView?.DisplayBlock is not { } displayBlock)
			{
				return;
			}

			e.Handled = true;
			if (e.Pointer.PointerDeviceType == PointerDeviceType.Touch)
			{
				return;
			}

			var index = Math.Max(0, displayBlock.ParsedText.GetIndexAt(e.GetCurrentPoint(displayBlock).Position, false, true));
			if (_mouseMultiTapChunk is { } multiTap)
			{
				var chunk = multiTap.tripleTap
					? GetLogicalLineChunk(index)
					: displayBlock.ParsedText.GetWordAt(index, true);
				if (chunk.start < multiTap.start)
				{
					SetInteractiveSelection(multiTap.start + multiTap.length, chunk.start - multiTap.start - multiTap.length);
				}
				else if (chunk.start + chunk.length >= multiTap.start + multiTap.length)
				{
					SetInteractiveSelection(multiTap.start, chunk.start + chunk.length - multiTap.start);
				}
			}
			else
			{
				SetInteractiveSelection(_pointerSelectionAnchor, index - _pointerSelectionAnchor);
			}
		}

		protected override void OnPointerReleased(PointerRoutedEventArgs e)
		{
			base.OnPointerReleased(e);
			_processedPointerPressedArgs = null;
			_mouseMultiTapChunk = null;

			if (!_isPressed)
			{
				return;
			}
			_isPressed = false;

			if (e.Pointer.PointerDeviceType == PointerDeviceType.Touch)
			{
				if (!TextControlFlyoutHelper.IsOpen(ContextFlyout))
				{
					Focus(FocusState.Pointer);
				}

				var currentPoint = e.GetCurrentPoint(null);
				var touchHoldTime = _lastPointerDown.point is { } down
					? currentPoint.Timestamp - down.Timestamp
					: 0;
				if (touchHoldTime < GestureRecognizer.HoldMinDelayMicroseconds && !string.IsNullOrEmpty(GetPlainTextContent()))
				{
					var displayBlock = _textBoxView!.DisplayBlock;
					var index = Math.Max(0, displayBlock.ParsedText.GetIndexAt(e.GetCurrentPoint(displayBlock).Position, true, true));
					if (!(IsReadOnly && index == _pressedLinkIndex && TryNavigateLinkAt(index)))
					{
						TouchTap(e.GetCurrentPoint(displayBlock).Position, _wasFocusedOnPointerPressed);
						QueueUpdateSelectionFlyoutVisibility(PointerDeviceType.Touch, e.GetCurrentPoint(this).Position);
					}
				}

				if (_hasPointerCapture)
				{
					ReleasePointerCapture(e.Pointer);
					_hasPointerCapture = false;
				}
				_pressedLinkIndex = -1;
				return;
			}

			if (_hasPointerCapture)
			{
				e.Handled = true;
				if (_textBoxView?.DisplayBlock is { } displayBlock)
				{
					var index = Math.Max(0, displayBlock.ParsedText.GetIndexAt(e.GetCurrentPoint(displayBlock).Position, true, true));
					var commandModifier = (e.KeyModifiers & _platformCtrlKey) != 0;
					if (index == _pressedLinkIndex && (IsReadOnly || commandModifier))
					{
						TryNavigateLinkAt(index);
					}
				}

				ReleasePointerCapture(e.Pointer);
				_hasPointerCapture = false;
				_pressedLinkIndex = -1;
				QueueUpdateSelectionFlyoutVisibility(e.Pointer.PointerDeviceType, e.GetCurrentPoint(this).Position);
			}
		}

		protected override void OnPointerCaptureLost(PointerRoutedEventArgs e)
		{
			base.OnPointerCaptureLost(e);
			_processedPointerPressedArgs = null;
			_isPressed = false;
			_mouseMultiTapChunk = null;
			_hasPointerCapture = false;
			_pressedLinkIndex = -1;
		}

		protected override void OnDoubleTapped(DoubleTappedRoutedEventArgs e)
		{
			base.OnDoubleTapped(e);
			e.Handled = true;
		}

		protected override void OnRightTapped(RightTappedRoutedEventArgs e)
		{
			base.OnRightTapped(e);
			if (_textBoxView?.DisplayBlock is not { } displayBlock)
			{
				return;
			}

			var index = Math.Max(0, displayBlock.ParsedText.GetIndexAt(e.GetPosition(displayBlock), true, true));
			if (index < _selection.start || index >= _selection.start + _selection.length)
			{
				SetInteractiveSelection(index, 0);
			}
		}

		private void TouchTap(Point point, bool wasFocused)
		{
			var displayBlock = _textBoxView!.DisplayBlock;
			var index = Math.Max(0, displayBlock.ParsedText.GetIndexAt(point, true, true));
			var tappedChunk = displayBlock.ParsedText.GetWordAt(index, true);
			var tappedInsideSelection = _selection.start <= index && index < _selection.start + _selection.length;
			if (tappedInsideSelection)
			{
				CaretMode = RichEditCaretDisplayMode.CaretWithThumbsBothEndsShowing;
			}
			else if (_selection.length == 0)
			{
				SetInteractiveSelection(tappedChunk.start, tappedChunk.length);
				CaretMode = RichEditCaretDisplayMode.CaretWithThumbsBothEndsShowing;
			}
			else
			{
				SetInteractiveSelection(tappedChunk.start, 0);
				CaretMode = RichEditCaretDisplayMode.CaretWithThumbsOnlyEndShowing;
			}
		}

		private (int start, int length) GetLogicalLineChunk(int index)
			=> global::Microsoft.UI.Text.TextUnitNavigation.GetLogicalLineChunk(GetPlainTextContent(), index);

		private static bool IsMultiTapGesture((ulong id, ulong ts, Point position) previousTap, PointerPoint down)
		{
			return previousTap.id == down.PointerId
				&& down.Timestamp - previousTap.ts <= GestureRecognizer.MultiTapMaxDelayMicroseconds
				&& !GestureRecognizer.IsOutOfTapRange(previousTap.position, down.Position);
		}
	}
}
