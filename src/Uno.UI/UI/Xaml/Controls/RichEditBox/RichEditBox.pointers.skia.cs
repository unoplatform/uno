#nullable enable

using System;
using Windows.System;
using Microsoft.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Controls
{
	// Minimal, faithful pointer-driven editing for RichEditBox on Skia.
	//
	// Covers the common mouse/pen/touch cases: click-to-place-caret, Shift+click to extend, press-and-
	// drag to select, and double-click/tap to select a word. It reuses the same hit-testing
	// (ParsedText.GetIndexAt / GetWordAt) and selection plumbing (SetInteractiveSelection) as the
	// keyboard layer, so caret/selection stay consistent across input modalities.
	//
	// TODO Uno: touch selection grippers, triple-tap line selection and the full
	// multi-tap gesture model that TextBox implements are intentionally out of scope for this slice.
	partial class RichEditBox
	{
		private bool _hasPointerCapture;
		private int _pointerSelectionAnchor;
		private int _pressedLinkIndex = -1;
		private PointerRoutedEventArgs? _processedPointerPressedArgs;

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

			DismissSelectionFlyoutForPointerPress();

			var currentPoint = e.GetCurrentPoint(null);

			// Right button is reserved for the context menu; let the base handling deal with it.
			if (currentPoint.Properties.IsRightButtonPressed)
			{
				return;
			}

			e.Handled = true;

			var index = Math.Max(0, displayBlock.ParsedText.GetIndexAt(e.GetCurrentPoint(displayBlock).Position, true, true));
			_pressedLinkIndex = index;

			if ((e.KeyModifiers & VirtualKeyModifiers.Shift) != 0)
			{
				// Extend the current selection from its fixed (non-caret) end to the pressed index.
				var anchor = _selection.selectionEndsAtTheStart ? _selection.start + _selection.length : _selection.start;
				_pointerSelectionAnchor = anchor;
				SetInteractiveSelection(anchor, index - anchor);
			}
			else
			{
				_pointerSelectionAnchor = index;
				SetInteractiveSelection(index, 0);
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

			var index = Math.Max(0, displayBlock.ParsedText.GetIndexAt(e.GetCurrentPoint(displayBlock).Position, false, true));
			SetInteractiveSelection(_pointerSelectionAnchor, index - _pointerSelectionAnchor);
		}

		protected override void OnPointerReleased(PointerRoutedEventArgs e)
		{
			base.OnPointerReleased(e);
			_processedPointerPressedArgs = null;

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
			_hasPointerCapture = false;
			_pressedLinkIndex = -1;
		}

		protected override void OnDoubleTapped(DoubleTappedRoutedEventArgs e)
		{
			base.OnDoubleTapped(e);

			if (_textBoxView?.DisplayBlock is not { } displayBlock)
			{
				return;
			}

			e.Handled = true;

			var index = Math.Max(0, displayBlock.ParsedText.GetIndexAt(e.GetPosition(displayBlock), true, true));
			var word = displayBlock.ParsedText.GetWordAt(index, true);
			_pointerSelectionAnchor = word.start;
			SetInteractiveSelection(word.start, word.length);
			QueueUpdateSelectionFlyoutVisibility(e.PointerDeviceType, e.GetPosition(this));
		}
	}
}
