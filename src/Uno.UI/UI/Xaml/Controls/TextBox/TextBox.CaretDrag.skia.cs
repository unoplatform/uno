#nullable enable

using System;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

public partial class TextBox
{
	/// <summary>
	/// The phases of a platform caret-drag gesture, mirroring the iOS floating cursor
	/// (begin/update/endFloatingCursor).
	/// </summary>
	internal enum CaretDragPhase
	{
		Begin,
		Update,
		End,
		Cancel,
	}

	// Centre of the caret rect when the gesture began, in DisplayBlock coordinates.
	private Point? _caretDragAnchor;

	// Caret position previewed during the gesture. The selection itself stays untouched until End,
	// so a drag raises a single SelectionChanged instead of one per callback.
	private int? _caretDragPreviewIndex;

	// The gesture needs a thumbless, non-blinking caret, so the mode in effect beforehand — which on
	// touch platforms carries the selection thumbs — is put back when it ends.
	private CaretDisplayMode? _caretModeBeforeCaretDrag;

	internal bool IsCaretDragActive => _caretDragAnchor is not null;

	/// <summary>
	/// Drives a platform caret-drag gesture, such as the iOS space-bar trackpad gesture.
	/// </summary>
	/// <param name="cumulativeOffset">
	/// Offset from the point captured at <see cref="CaretDragPhase.Begin"/>, in DIPs. This is always
	/// measured against the gesture origin, never against the previous callback — accumulating
	/// per-callback increments drifts.
	/// </param>
	/// <returns>False when the gesture is declined, so the caller can stop forwarding it.</returns>
	internal bool ProcessCaretDragGesture(CaretDragPhase phase, Point cumulativeOffset)
	{
		switch (phase)
		{
			case CaretDragPhase.Begin:
				return BeginCaretDrag();
			case CaretDragPhase.Update:
				return UpdateCaretDrag(cumulativeOffset);
			case CaretDragPhase.End:
				return EndCaretDrag(commit: true);
			case CaretDragPhase.Cancel:
				return EndCaretDrag(commit: false);
			default:
				return false;
		}
	}

	internal void CancelCaretDrag()
	{
		if (IsCaretDragActive)
		{
			EndCaretDrag(commit: false);
		}
	}

	private bool BeginCaretDrag()
	{
		if (!_isSkiaTextBox ||
			IsReadOnly ||
			FocusState == FocusState.Unfocused ||
			GetParsedTextForCaretDrag() is not { } parsedText)
		{
			return false;
		}

		var caretIndex = IsBackwardSelection ? SelectionStart : SelectionStart + SelectionLength;
		var caretRect = parsedText.GetRectForIndex(caretIndex);
		_caretDragAnchor = new Point(caretRect.Left, caretRect.Top + (caretRect.Height / 2));

		_caretModeBeforeCaretDrag ??= CaretMode;

		// The caret must not blink away mid-drag. CaretMode restarts the timer when it changes,
		// so stop it afterwards.
		CaretMode = CaretDisplayMode.ThumblessCaretShowing;
		_timer.Stop();

		// Left null on purpose: an End with no intervening Update must not move the caret.
		_caretDragPreviewIndex = null;
		UpdateDisplaySelection();
		return true;
	}

	private bool UpdateCaretDrag(Point cumulativeOffset)
	{
		if (_caretDragAnchor is not { } anchor)
		{
			return false;
		}

		if (GetParsedTextForCaretDrag() is not { } parsedText)
		{
			EndCaretDrag(commit: false);
			return false;
		}

		var textLength = Text.Length;

		// Clamping to the first and last line centres keeps an over-shooting drag on the text
		// instead of returning a miss.
		var firstLine = parsedText.GetRectForIndex(0);
		var lastLine = parsedText.GetRectForIndex(textLength);
		var minY = firstLine.Top + (firstLine.Height / 2);
		var maxY = lastLine.Top + (lastLine.Height / 2);

		var x = anchor.X + cumulativeOffset.X;
		var y = Math.Clamp(anchor.Y + cumulativeOffset.Y, minY, maxY);

		// GetIndexAt returns -1 on a miss, hence the Math.Max, matching every other call site.
		var index = Math.Max(0, parsedText.GetIndexAt(new Point(x, y), true, true));
		_caretDragPreviewIndex = Math.Clamp(index, 0, textLength);

		// Both the anchor and the hit-test work in text coordinates, which are independent of the
		// scroll offset, so scrolling mid-gesture cannot skew the mapping.
		UpdateScrolling();
		UpdateDisplaySelection();
		return true;
	}

	private bool EndCaretDrag(bool commit)
	{
		if (!IsCaretDragActive)
		{
			return false;
		}

		var previewIndex = _caretDragPreviewIndex;
		var previousMode = _caretModeBeforeCaretDrag;

		_caretDragAnchor = null;
		_caretDragPreviewIndex = null;
		_caretModeBeforeCaretDrag = null;

		// Restored before committing so the selection transitions in SelectPartial see the real mode.
		if (previousMode is { } mode)
		{
			CaretMode = mode;
		}

		if (CaretMode is CaretDisplayMode.ThumblessCaretShowing)
		{
			_timer.Start(); // resume blinking
		}

		if (commit && previewIndex is { } index)
		{
			SelectInternal(Math.Clamp(index, 0, Text.Length), 0);
		}

		UpdateDisplaySelection();
		return true;
	}

	private Documents.IParsedText? GetParsedTextForCaretDrag()
		=> DisplayBlockInlines is null ? null : TextBoxView?.DisplayBlock?.ParsedText;
}
