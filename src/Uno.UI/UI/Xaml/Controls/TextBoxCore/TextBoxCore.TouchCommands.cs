#nullable enable

using System;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

// Helpers for the touch selection flyout commands (TextCommandBarFlyout "Select", and platform commands that
// need to anchor UI at the selection).
internal sealed partial class TextBoxCore
{
	/// <summary>
	/// Selects the word at the caret, with both selection handles, as the iOS "Select" command does.
	/// </summary>
	internal void SelectWordAtCaretForTouch()
	{
		var text = Text;
		if (text.Length == 0)
		{
			return;
		}

		// A caret right after a word (the usual place after typing or a tap) selects that word, not the space or
		// end of text that follows it.
		var caret = IsBackwardSelection ? SelectionStart : SelectionStart + SelectionLength;
		if (caret > 0 && (caret >= text.Length || char.IsWhiteSpace(text[caret])))
		{
			caret--;
		}

		TouchSelectWordAt(caret);

		// As for Select All: the flyout comes back for the new selection (see OnTouchSelectAllFromFlyout).
		QueueUpdateSelectionFlyoutVisibility(Microsoft.UI.Input.PointerDeviceType.Touch, new Point(Owner.ActualWidth / 2, 0));
	}

	/// <summary>
	/// After Select All from a touch flyout (iOS/Android conventions): show the selection's handles and bring the flyout
	/// back with the selection's commands. Invoking the command can close it (an overflow item closes the flyout, and
	/// showing the handles dismisses a transient one), so it is always re-queued; if still open it only repositions.
	/// </summary>
	internal void OnTouchSelectAllFromFlyout()
	{
		if (SelectionLength == 0 || TouchSelectionConvention == TouchTextSelectionConvention.Desktop)
		{
			return;
		}

		CaretMode = CaretDisplayMode.CaretWithThumbsBothEndsShowing;
		QueueUpdateSelectionFlyoutVisibility(Microsoft.UI.Input.PointerDeviceType.Touch, new Point(Owner.ActualWidth / 2, 0));
	}

	/// <summary>
	/// The selection (or caret) bounds in XamlRoot coordinates, clipped to the visible part of the TextBox.
	/// </summary>
	internal bool TryGetSelectionBoundsInRoot(out Rect bounds)
	{
		bounds = default;

		if (TextBoxView?.DisplayBlock is not { } displayBlock)
		{
			return false;
		}

		var parsedText = displayBlock.ParsedText;
		var startRect = parsedText.GetRectForIndex(SelectionStart);
		var endRect = parsedText.GetRectForIndex(SelectionStart + SelectionLength);

		Rect local;
		if (Math.Abs(startRect.Top - endRect.Top) < 0.5)
		{
			var left = Math.Min(startRect.Left, endRect.Left);
			var right = Math.Max(Math.Max(startRect.Left, endRect.Left), left + TextBlock.CaretThickness);
			local = new Rect(left, startRect.Top, right - left, Math.Max(startRect.Height, endRect.Height));
		}
		else
		{
			// Several lines: the selection spans the full width between the first and the last line.
			var top = Math.Min(startRect.Top, endRect.Top);
			var bottom = Math.Max(startRect.Bottom, endRect.Bottom);
			local = new Rect(0, top, Math.Max(displayBlock.ActualWidth, 1), bottom - top);
		}

		var selection = displayBlock.TransformToVisual(null).TransformBounds(local);
		var owner = Owner.TransformToVisual(null).TransformBounds(new Rect(0, 0, Owner.ActualWidth, Owner.ActualHeight));

		// The text can be scrolled inside the TextBox; never anchor at text that is out of view.
		var visible = selection;
		visible.Intersect(owner);
		bounds = visible.IsEmpty ? owner : visible;
		return true;
	}
}
