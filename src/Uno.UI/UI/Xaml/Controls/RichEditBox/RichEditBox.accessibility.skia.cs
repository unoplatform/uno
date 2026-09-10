#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Text;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	partial class RichEditBox
	{
		internal string GetAccessibilityText() => GetPlainTextContent();

		internal void GetAccessibilitySelection(out int selectionStart, out int selectionEnd)
			=> GetAccessibilitySelection(out selectionStart, out selectionEnd, out _);

		internal void GetAccessibilitySelection(
			out int selectionStart,
			out int selectionEnd,
			out bool isBackward)
		{
			selectionStart = Math.Min(Document.Selection.StartPosition, Document.Selection.EndPosition);
			selectionEnd = Math.Max(Document.Selection.StartPosition, Document.Selection.EndPosition);
			isBackward = selectionStart != selectionEnd
				&& Document.Selection.Options.HasFlag(SelectionOptions.StartActive);
		}

		internal bool ApplyAccessibilityTextInput(
			string? value,
			int selectionStart,
			int selectionEnd,
			bool isBackward = false)
		{
			var text = value ?? string.Empty;
			if (!IsEnabled
				|| IsReadOnly
				|| selectionStart < 0
				|| selectionEnd < selectionStart
				|| selectionEnd > text.Length)
			{
				return false;
			}

			var existingLength = GetPlainTextLength();
			var preservedLength = existingLength - Math.Abs(_selection.length);
			var insertedLength = Math.Max(0, text.Length - preservedLength);
			if (insertedLength > GetClipboardPasteSourceLimit())
			{
				return false;
			}

			return TryUpdateTextFromNative(
				text,
				isBackward ? selectionEnd : selectionStart,
				isBackward ? selectionStart - selectionEnd : selectionEnd - selectionStart);
		}

		internal bool ApplyAccessibilitySelection(
			int selectionStart,
			int selectionEnd,
			bool isBackward = false)
		{
			var length = GetPlainTextLength();
			if (!IsEnabled
				|| selectionStart < 0
				|| selectionEnd < selectionStart
				|| selectionEnd > length)
			{
				return false;
			}

			SetInteractiveSelection(
				isBackward ? selectionEnd : selectionStart,
				isBackward ? selectionStart - selectionEnd : selectionEnd - selectionStart);
			GetAccessibilitySelection(out var actualStart, out var actualEnd, out var actualIsBackward);
			return actualStart == selectionStart
				&& actualEnd == selectionEnd
				&& (selectionStart == selectionEnd || actualIsBackward == isBackward);
		}

		internal IReadOnlyList<Documents.RichEditSpellingAnnotationInfo> GetAccessibilitySpellingAnnotations()
		{
			if (!IsSpellCheckEnabled
				|| _textBoxView?.DisplayBlock.ParsedText is not Documents.UnicodeText unicodeText)
			{
				return Array.Empty<Documents.RichEditSpellingAnnotationInfo>();
			}

			return unicodeText.GetSpellingAnnotations();
		}

		internal bool TryGetAccessibilityRangeBounds(int start, int end, out Rect bounds)
		{
			bounds = default;
			if (Visibility != Visibility.Visible
				|| !IsLoaded
				|| !TryGetRangeRectangles(
					start,
					end,
					global::Microsoft.UI.Text.PointOptions.ClientCoordinates,
					out var rectangles)
				|| rectangles.Length == 0)
			{
				return false;
			}

			var transform = TransformToVisual(null);
			var left = double.PositiveInfinity;
			var top = double.PositiveInfinity;
			var right = double.NegativeInfinity;
			var bottom = double.NegativeInfinity;
			foreach (var rectangle in rectangles)
			{
				var viewportRectangle = _contentElement is ScrollViewer scrollViewer
					? new Rect(
						rectangle.X - scrollViewer.HorizontalOffset,
						rectangle.Y - scrollViewer.VerticalOffset,
						rectangle.Width,
						rectangle.Height)
					: rectangle;
				var transformed = transform.TransformBounds(viewportRectangle);
				left = Math.Min(left, transformed.Left);
				top = Math.Min(top, transformed.Top);
				right = Math.Max(right, transformed.Right);
				bottom = Math.Max(bottom, transformed.Bottom);
			}

			bounds = new Rect(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
			if (_contentElement is FrameworkElement viewport)
			{
				var viewportBounds = viewport.TransformToVisual(null).TransformBounds(
					new Rect(0, 0, viewport.ActualWidth, viewport.ActualHeight));
				bounds.Intersect(viewportBounds);
			}

			return bounds.Width > 0 && bounds.Height > 0;
		}

		internal bool IsAccessibilityRangeFocused(int start, int end)
		{
			if (FocusState == FocusState.Unfocused)
			{
				return false;
			}

			var selectionStart = _selection.start;
			var selectionEnd = selectionStart + _selection.length;
			if (selectionStart == selectionEnd)
			{
				return selectionStart >= start && selectionStart <= end;
			}

			return selectionStart < end && selectionEnd > start;
		}

		internal void FocusAccessibilityRange(int start, int end)
		{
			if (Focus(FocusState.Programmatic))
			{
				Document.Selection.SetRange(start, end);
				TryScrollRangeIntoView(start, end, alignToTop: false);
			}
		}
	}
}
