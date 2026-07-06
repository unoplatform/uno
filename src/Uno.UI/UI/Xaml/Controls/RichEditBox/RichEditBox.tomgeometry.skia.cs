#nullable enable

using System;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	// Geometry-backed helpers for the functional Text Object Model's coordinate APIs
	// (ITextRange.GetPoint/GetRect/SetPoint/ScrollIntoView and ITextDocument.GetRangeFromPoint).
	// These project the shared DisplayBlock layout (ParsedText.GetRectForIndex/GetIndexAt) into the
	// coordinate space requested through PointOptions and back, so programmatic hit-testing and caret
	// geometry match what the control renders. All helpers no-op (return false) when the view is not
	// laid out.
	//
	// TODO Uno: PointOptions screen coordinates are approximated as XamlRoot-root-relative coordinates
	// (true device/screen coordinates need the window's on-screen placement). Round-trips within a
	// single PointOptions value are exact; only the absolute screen offset differs from WinUI.
	partial class RichEditBox
	{
		// The caret rect of the single character position <paramref name="index"/>, in the coordinate
		// space requested by <paramref name="options"/>.
		internal bool TryGetIndexRect(int index, PointOptions options, out Rect rect)
		{
			rect = default;
			if (_textBoxView?.DisplayBlock is not { } displayBlock)
			{
				return false;
			}

			var text = GetPlainTextContent();
			index = Math.Clamp(index, 0, text.Length);
			var local = displayBlock.ParsedText.GetRectForIndex(index);
			rect = TransformRectFromDisplaySpace(displayBlock, local, options);
			return true;
		}

		// The bounding rect of the range [start,end) in the requested coordinate space.
		internal bool TryGetRangeRect(int start, int end, PointOptions options, out Rect rect)
		{
			rect = default;
			if (_textBoxView?.DisplayBlock is not { } displayBlock)
			{
				return false;
			}

			var text = GetPlainTextContent();
			start = Math.Clamp(start, 0, text.Length);
			end = Math.Clamp(end, 0, text.Length);
			if (end < start)
			{
				(start, end) = (end, start);
			}

			var local = GetRangeRectInDisplaySpace(displayBlock, start, end);
			rect = TransformRectFromDisplaySpace(displayBlock, local, options);
			return true;
		}

		// The character index nearest <paramref name="point"/> (given in the coordinate space
		// described by <paramref name="options"/>).
		internal bool TryGetIndexFromPoint(Point point, PointOptions options, out int index)
		{
			index = 0;
			if (_textBoxView?.DisplayBlock is not { } displayBlock)
			{
				return false;
			}

			var local = TransformPointToDisplaySpace(displayBlock, point, options);
			index = Math.Max(0, displayBlock.ParsedText.GetIndexAt(local, ignoreEndingNewLine: true, extendedSelection: false));
			return true;
		}

		// Scrolls the range [start,end) into view through the hosting ScrollViewer.
		internal bool TryScrollRangeIntoView(int start, int end)
		{
			if (_textBoxView?.DisplayBlock is not { } displayBlock)
			{
				return false;
			}

			var text = GetPlainTextContent();
			start = Math.Clamp(start, 0, text.Length);
			end = Math.Clamp(end, 0, text.Length);
			if (end < start)
			{
				(start, end) = (end, start);
			}

			var local = GetRangeRectInDisplaySpace(displayBlock, start, end);
			displayBlock.StartBringIntoView(new BringIntoViewOptions
			{
				TargetRect = local,
				AnimationDesired = false,
			});
			return true;
		}

		private static Rect GetRangeRectInDisplaySpace(TextBlock displayBlock, int start, int end)
		{
			var parsed = displayBlock.ParsedText;
			var startRect = parsed.GetRectForIndex(start);
			if (start == end)
			{
				return new Rect(startRect.X, startRect.Y, 0, startRect.Height);
			}

			var endRect = parsed.GetRectForIndex(end);
			if (Math.Abs(startRect.Y - endRect.Y) < 0.5)
			{
				// Same visual line: the rect spans from the start caret to the end caret.
				var lineX = Math.Min(startRect.X, endRect.X);
				var lineRight = Math.Max(startRect.X, endRect.X);
				return new Rect(lineX, startRect.Y, lineRight - lineX, Math.Max(startRect.Height, endRect.Height));
			}

			// Multi-line: the bounding box of the two endpoints (a documented approximation of the
			// exact multi-line selection polygon).
			var left = Math.Min(startRect.X, endRect.X);
			var right = Math.Max(startRect.X + startRect.Width, endRect.X + endRect.Width);
			var top = Math.Min(startRect.Y, endRect.Y);
			var bottom = Math.Max(startRect.Y + startRect.Height, endRect.Y + endRect.Height);
			return new Rect(left, top, right - left, bottom - top);
		}

		private Rect TransformRectFromDisplaySpace(TextBlock displayBlock, Rect rect, PointOptions options)
		{
			var target = ResolveCoordinateTarget(options);
			try
			{
				return displayBlock.TransformToVisual(target).TransformBounds(rect);
			}
			catch
			{
				return rect;
			}
		}

		private Point TransformPointToDisplaySpace(TextBlock displayBlock, Point point, PointOptions options)
		{
			var source = ResolveCoordinateTarget(options);
			try
			{
				return source.TransformToVisual(displayBlock).TransformPoint(point);
			}
			catch
			{
				return point;
			}
		}

		// The visual whose coordinate space a PointOptions value refers to: the RichEditBox itself for
		// ClientCoordinates, otherwise the XamlRoot visual root (our approximation of screen space).
		private UIElement ResolveCoordinateTarget(PointOptions options)
			=> options.HasFlag(PointOptions.ClientCoordinates)
				? this
				: (XamlRoot?.Content as UIElement) ?? this;
	}
}
