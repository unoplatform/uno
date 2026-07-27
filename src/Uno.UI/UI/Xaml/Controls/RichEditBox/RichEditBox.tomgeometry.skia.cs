#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Documents;
using Uno.Foundation.Logging;
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

			index = Math.Clamp(index, 0, GetPlainTextLength());
			var local = displayBlock.ParsedText.GetGeometryPosition(index).CaretRect;
			return TryTransformRectFromDisplaySpace(displayBlock, local, options, out rect);
		}

		internal bool TryGetIndexBaseline(int index, PointOptions options, out double baseline)
		{
			baseline = 0;
			if (_textBoxView?.DisplayBlock is not { } displayBlock)
			{
				return false;
			}

			index = Math.Clamp(index, 0, GetPlainTextLength());
			var rect = displayBlock.ParsedText.GetGeometryPosition(index).CaretRect;
			if (!TryTransformPointFromDisplaySpace(
				displayBlock,
				new Point(rect.X, displayBlock.ParsedText.GetBaselineForIndex(index)),
				options,
				out var point))
			{
				return false;
			}
			baseline = point.Y;
			return true;
		}

		// The bounding rect of the range [start,end) in the requested coordinate space.
		internal bool TryGetRangeRect(int start, int end, PointOptions options, out Rect rect)
		{
			var success = TryGetRangeGeometry(start, end, options, isSelection: false, out var result);
			rect = result.Rect;
			return success;
		}

		internal bool TryGetRangeGeometry(
			int start,
			int end,
			PointOptions options,
			bool isSelection,
			out RichEditTextGeometryHitResult result)
		{
			if (_textBoxView?.DisplayBlock is not { } displayBlock)
			{
				result = new RichEditTextGeometryHitResult(
					default,
					RichEditTextGeometryHitKind.Unloaded
						| (isSelection ? RichEditTextGeometryHitKind.Selection : RichEditTextGeometryHitKind.None));
				return false;
			}

			var textLength = GetPlainTextLength();
			start = Math.Clamp(start, 0, textLength);
			var includesFinalEndOfParagraph = end > textLength;
			end = Math.Clamp(end, 0, textLength);
			if (end < start)
			{
				(start, end) = (end, start);
			}

			var local = GetRangeRectInDisplaySpace(displayBlock, start, end);
			var kind = GetRangeGeometryKind(
				displayBlock.ParsedText,
				start,
				end,
				includesFinalEndOfParagraph,
				isSelection);
			kind |= GetViewportClipping(local);
			if (!TryTransformRectFromDisplaySpace(displayBlock, local, options, out var rect))
			{
				result = new RichEditTextGeometryHitResult(default, kind);
				return false;
			}

			result = new RichEditTextGeometryHitResult(rect, kind);
			return true;
		}

		internal bool TryGetRangeRectangles(int start, int end, PointOptions options, out Rect[] rectangles)
		{
			rectangles = Array.Empty<Rect>();
			if (_textBoxView?.DisplayBlock is not { } displayBlock)
			{
				return false;
			}

			var textLength = GetPlainTextLength();
			start = Math.Clamp(start, 0, textLength);
			end = Math.Clamp(end, 0, textLength);
			if (end < start)
			{
				(start, end) = (end, start);
			}

			if (start == end)
			{
				var caret = displayBlock.ParsedText.GetGeometryPosition(start).CaretRect;
				if (!TryTransformRectFromDisplaySpace(displayBlock, caret, options, out var transformedCaret))
				{
					return false;
				}

				rectangles = new[] { transformedCaret };
				return true;
			}

			var parsed = displayBlock.ParsedText;
			var localRectangles = new List<Rect>();
			var position = start;
			while (position < end)
			{
				if (!TryGetLineBounds(position, out _, out var contentEnd, out _, out _))
				{
					rectangles = Array.Empty<Rect>();
					return false;
				}
				var lineEnd = Math.Min(
					textLength,
					contentEnd + Document.GetHardLineBreakLengthAt(contentEnd));
				if (lineEnd <= position)
				{
					lineEnd = Math.Min(textLength, position + 1);
				}

				var segmentEnd = Math.Min(end, lineEnd);
				var startRect = parsed.GetRectForIndex(position);
				var endRect = parsed.GetRectForIndex(segmentEnd);
				double right;
				if (Math.Abs(startRect.Y - endRect.Y) < 0.5)
				{
					right = endRect.X;
				}
				else
				{
					var lastRect = parsed.GetRectForIndex(Math.Max(position, segmentEnd - 1));
					right = lastRect.X + lastRect.Width;
				}

				var left = Math.Min(startRect.X, right);
				localRectangles.Add(new Rect(
					left,
					startRect.Y,
					Math.Max(0, Math.Max(startRect.X, right) - left),
					startRect.Height));
				position = segmentEnd;
			}

			rectangles = new Rect[localRectangles.Count];
			for (var i = 0; i < localRectangles.Count; i++)
			{
				if (!TryTransformRectFromDisplaySpace(displayBlock, localRectangles[i], options, out rectangles[i]))
				{
					rectangles = Array.Empty<Rect>();
					return false;
				}
			}

			return true;
		}

		// The character index nearest <paramref name="point"/> (given in the coordinate space
		// described by <paramref name="options"/>).
		internal bool TryGetIndexFromPoint(Point point, PointOptions options, out int index)
			=> TryGetIndexFromPoint(point, options, out index, out _);

		internal bool TryGetIndexFromPoint(
			Point point,
			PointOptions options,
			out int index,
			out RichEditTextGeometryHitResult hitResult)
		{
			index = 0;
			if (_textBoxView?.DisplayBlock is not { } displayBlock)
			{
				hitResult = new RichEditTextGeometryHitResult(default, RichEditTextGeometryHitKind.Unloaded);
				return false;
			}

			if (!TryTransformPointToDisplaySpace(displayBlock, point, options, out var local))
			{
				hitResult = default;
				return false;
			}
			index = Math.Clamp(
				displayBlock.ParsedText.GetIndexAt(local, ignoreEndingNewLine: false, extendedSelection: true),
				0,
				GetPlainTextLength());
			var position = displayBlock.ParsedText.GetGeometryPosition(index);
			var kind = MapGeometryKind(position.Kind);
			kind |= GetPointViewportClipping(local);
			hitResult = new RichEditTextGeometryHitResult(position.CaretRect, kind);
			return true;
		}

		// Scrolls the range [start,end) into view through the hosting ScrollViewer.
		internal bool TryScrollRangeIntoView(int start, int end, PointOptions options)
		{
			if (_textBoxView?.DisplayBlock is not { } displayBlock || _contentElement is not ScrollViewer scrollViewer)
			{
				return false;
			}

			var textLength = GetPlainTextLength();
			start = Math.Clamp(start, 0, textLength);
			end = Math.Clamp(end, 0, textLength);
			if (end < start)
			{
				(start, end) = (end, start);
			}

			var index = options.HasFlag(PointOptions.Start) ? start : end;
			var caretRect = displayBlock.ParsedText.GetGeometryPosition(index).CaretRect with { Width = TextBlock.CaretThickness };
			double? horizontalOffset = null;
			double? verticalOffset = null;
			if (!options.HasFlag(PointOptions.NoHorizontalScroll))
			{
				horizontalOffset = Math.Max(
					Math.Min(scrollViewer.HorizontalOffset, caretRect.Left),
					Math.Ceiling(caretRect.Right - scrollViewer.ViewportWidth + TextBlock.CaretThickness));
			}

			if (!options.HasFlag(PointOptions.NoVerticalScroll))
			{
				verticalOffset = Math.Max(
					Math.Min(scrollViewer.VerticalOffset, caretRect.Top),
					caretRect.Bottom - scrollViewer.ViewportHeight);
			}

			scrollViewer.ChangeView(horizontalOffset, verticalOffset, null, disableAnimation: true);
			return true;
		}

		internal bool TryScrollRangeIntoView(int start, int end, bool alignToTop)
		{
			if (_textBoxView?.DisplayBlock is not { } displayBlock || _contentElement is not ScrollViewer scrollViewer)
			{
				return false;
			}

			var textLength = GetPlainTextLength();
			start = Math.Clamp(start, 0, textLength);
			end = Math.Clamp(end, 0, textLength);
			if (end < start)
			{
				(start, end) = (end, start);
			}

			var index = alignToTop ? start : end;
			var caretRect = displayBlock.ParsedText.GetGeometryPosition(index).CaretRect with { Width = TextBlock.CaretThickness };
			var horizontalOffset = Math.Max(
				Math.Min(scrollViewer.HorizontalOffset, caretRect.Left),
				Math.Ceiling(caretRect.Right - scrollViewer.ViewportWidth + TextBlock.CaretThickness));
			var verticalOffset = alignToTop
				? caretRect.Top
				: caretRect.Bottom - scrollViewer.ViewportHeight;

			scrollViewer.ChangeView(
				Math.Clamp(horizontalOffset, 0, scrollViewer.ScrollableWidth),
				Math.Clamp(verticalOffset, 0, scrollViewer.ScrollableHeight),
				null,
				disableAnimation: true);
			return true;
		}

		private static Rect GetRangeRectInDisplaySpace(TextBlock displayBlock, int start, int end)
		{
			var parsed = displayBlock.ParsedText;
			if (start == end)
			{
				return parsed.GetGeometryPosition(start).CaretRect;
			}

			var left = double.PositiveInfinity;
			var right = double.NegativeInfinity;
			var top = double.PositiveInfinity;
			var bottom = double.NegativeInfinity;
			for (var index = start; index < end; index++)
			{
				var current = parsed.GetGeometryPosition(index).CharacterRect;
				left = Math.Min(left, current.Left);
				right = Math.Max(right, current.Right);
				top = Math.Min(top, current.Top);
				bottom = Math.Max(bottom, current.Bottom);
			}

			if (double.IsPositiveInfinity(left))
			{
				return parsed.GetGeometryPosition(start).CaretRect;
			}

			return new Rect(left, top, right - left, bottom - top);
		}

		private RichEditTextGeometryHitKind GetRangeGeometryKind(
			IParsedText parsed,
			int start,
			int end,
			bool includesFinalEndOfParagraph,
			bool isSelection)
		{
			var kind = isSelection ? RichEditTextGeometryHitKind.Selection : RichEditTextGeometryHitKind.None;
			if (start == end)
			{
				kind |= MapGeometryKind(parsed.GetGeometryPosition(start).Kind);
			}
			else
			{
				kind |= RichEditTextGeometryHitKind.Text;
				for (var index = start; index < end; index++)
				{
					kind |= MapGeometryKind(parsed.GetGeometryPosition(index).Kind)
						& (RichEditTextGeometryHitKind.InlineObject
							| RichEditTextGeometryHitKind.StructuredMath
							| RichEditTextGeometryHitKind.RightToLeft
							| RichEditTextGeometryHitKind.LeadingEdge
							| RichEditTextGeometryHitKind.TrailingEdge);
				}
			}

			if (includesFinalEndOfParagraph || start == GetPlainTextLength())
			{
				kind |= RichEditTextGeometryHitKind.FinalEndOfParagraph;
			}

			return kind;
		}

		private RichEditTextGeometryHitKind GetViewportClipping(Rect rect)
		{
			if (_contentElement is not ScrollViewer scrollViewer)
			{
				return RichEditTextGeometryHitKind.None;
			}

			var viewport = new Rect(
				scrollViewer.HorizontalOffset,
				scrollViewer.VerticalOffset,
				scrollViewer.ViewportWidth,
				scrollViewer.ViewportHeight);
			var kind = RichEditTextGeometryHitKind.None;
			if (rect.Top < viewport.Top)
			{
				kind |= RichEditTextGeometryHitKind.ClippedAbove;
			}
			if (rect.Bottom > viewport.Bottom)
			{
				kind |= RichEditTextGeometryHitKind.ClippedBelow;
			}
			if (rect.Left < viewport.Left)
			{
				kind |= RichEditTextGeometryHitKind.ClippedLeft;
			}
			if (rect.Right > viewport.Right)
			{
				kind |= RichEditTextGeometryHitKind.ClippedRight;
			}

			return kind;
		}

		private RichEditTextGeometryHitKind GetPointViewportClipping(Point point)
		{
			if (_contentElement is not ScrollViewer scrollViewer)
			{
				return RichEditTextGeometryHitKind.None;
			}

			var kind = RichEditTextGeometryHitKind.None;
			if (point.Y < scrollViewer.VerticalOffset)
			{
				kind |= RichEditTextGeometryHitKind.ClippedAbove;
			}
			if (point.Y > scrollViewer.VerticalOffset + scrollViewer.ViewportHeight)
			{
				kind |= RichEditTextGeometryHitKind.ClippedBelow;
			}
			if (point.X < scrollViewer.HorizontalOffset)
			{
				kind |= RichEditTextGeometryHitKind.ClippedLeft;
			}
			if (point.X > scrollViewer.HorizontalOffset + scrollViewer.ViewportWidth)
			{
				kind |= RichEditTextGeometryHitKind.ClippedRight;
			}

			return kind;
		}

		private static RichEditTextGeometryHitKind MapGeometryKind(TextGeometryPositionKind kind)
		{
			var result = RichEditTextGeometryHitKind.None;
			if (kind.HasFlag(TextGeometryPositionKind.Text))
			{
				result |= RichEditTextGeometryHitKind.Text;
			}
			if (kind.HasFlag(TextGeometryPositionKind.Caret))
			{
				result |= RichEditTextGeometryHitKind.Caret;
			}
			if (kind.HasFlag(TextGeometryPositionKind.FinalEndOfParagraph))
			{
				result |= RichEditTextGeometryHitKind.FinalEndOfParagraph;
			}
			if (kind.HasFlag(TextGeometryPositionKind.InlineObject))
			{
				result |= RichEditTextGeometryHitKind.InlineObject;
			}
			if (kind.HasFlag(TextGeometryPositionKind.StructuredMath))
			{
				result |= RichEditTextGeometryHitKind.StructuredMath;
			}
			if (kind.HasFlag(TextGeometryPositionKind.RightToLeft))
			{
				result |= RichEditTextGeometryHitKind.RightToLeft;
			}
			if (kind.HasFlag(TextGeometryPositionKind.LeadingEdge))
			{
				result |= RichEditTextGeometryHitKind.LeadingEdge;
			}
			if (kind.HasFlag(TextGeometryPositionKind.TrailingEdge))
			{
				result |= RichEditTextGeometryHitKind.TrailingEdge;
			}

			return result;
		}

		private bool TryTransformRectFromDisplaySpace(TextBlock displayBlock, Rect rect, PointOptions options, out Rect transformed)
		{
			try
			{
				var clientRect = displayBlock.TransformToVisual(this).TransformBounds(rect);
				if (_contentElement is ScrollViewer scrollViewer)
				{
					clientRect = new Rect(
						clientRect.X + scrollViewer.HorizontalOffset,
						clientRect.Y + scrollViewer.VerticalOffset,
						clientRect.Width,
						clientRect.Height);
				}

				if (options.HasFlag(PointOptions.ClientCoordinates))
				{
					transformed = clientRect;
					return true;
				}

				var rootRect = TransformToVisual(null).TransformBounds(clientRect);
				return TryConvertRootToScreen(rootRect, out transformed);
			}
			catch (Exception error) when (error is InvalidOperationException or ArgumentException)
			{
				typeof(RichEditBox).LogError()?.Error("Failed to transform RichEditBox range geometry.", error);
				transformed = default;
				return false;
			}
		}

		private bool TryTransformPointToDisplaySpace(TextBlock displayBlock, Point point, PointOptions options, out Point transformed)
		{
			try
			{
				if (options.HasFlag(PointOptions.ClientCoordinates))
				{
					transformed = TransformToVisual(displayBlock).TransformPoint(point);
					if (_contentElement is ScrollViewer screenScrollViewer)
					{
						transformed = new Point(
							transformed.X - screenScrollViewer.HorizontalOffset,
							transformed.Y - screenScrollViewer.VerticalOffset);
					}
					return true;
				}

				if (!TryConvertScreenToRoot(point, out var rootPoint))
				{
					transformed = default;
					return false;
				}
				var root = (XamlRoot?.Content as UIElement) ?? this;
				transformed = root.TransformToVisual(displayBlock).TransformPoint(rootPoint);
				if (_contentElement is ScrollViewer scrollViewer)
				{
					transformed = new Point(
						transformed.X - scrollViewer.HorizontalOffset,
						transformed.Y - scrollViewer.VerticalOffset);
				}
				return true;
			}
			catch (Exception error) when (error is InvalidOperationException or ArgumentException)
			{
				typeof(RichEditBox).LogError()?.Error("Failed to transform a point into RichEditBox display space.", error);
				transformed = default;
				return false;
			}
		}

		private bool TryTransformPointFromDisplaySpace(TextBlock displayBlock, Point point, PointOptions options, out Point transformed)
		{
			try
			{
				var clientPoint = displayBlock.TransformToVisual(this).TransformPoint(point);
				if (_contentElement is ScrollViewer scrollViewer)
				{
					clientPoint = new Point(
						clientPoint.X + scrollViewer.HorizontalOffset,
						clientPoint.Y + scrollViewer.VerticalOffset);
				}

				if (options.HasFlag(PointOptions.ClientCoordinates))
				{
					transformed = clientPoint;
					return true;
				}

				return TryConvertRootToScreen(TransformToVisual(null).TransformPoint(clientPoint), out transformed);
			}
			catch (Exception error) when (error is InvalidOperationException or ArgumentException)
			{
				typeof(RichEditBox).LogError()?.Error("Failed to transform a point from RichEditBox display space.", error);
				transformed = default;
				return false;
			}
		}

		private bool TryConvertRootToScreen(Rect rect, out Rect screenRect)
		{
			var root = XamlRoot;
			var wrapper = root?.VisualTree.ContentRoot.GetOwnerWindow()?.NativeWrapper;
			if (wrapper is null
				|| !wrapper.TryConvertLocalToScreen(new Point(rect.X, rect.Y), out var topLeft)
				|| !wrapper.TryConvertLocalToScreen(new Point(rect.Right, rect.Bottom), out var bottomRight))
			{
				screenRect = default;
				return false;
			}

			screenRect = new Rect(
				Math.Min(topLeft.X, bottomRight.X),
				Math.Min(topLeft.Y, bottomRight.Y),
				Math.Abs(bottomRight.X - topLeft.X),
				Math.Abs(bottomRight.Y - topLeft.Y));
			return true;
		}

		private bool TryConvertRootToScreen(Point point, out Point screenPoint)
		{
			var wrapper = XamlRoot?.VisualTree.ContentRoot.GetOwnerWindow()?.NativeWrapper;
			if (wrapper is null || !wrapper.TryConvertLocalToScreen(point, out var converted))
			{
				screenPoint = default;
				return false;
			}

			screenPoint = new Point(converted.X, converted.Y);
			return true;
		}

		private bool TryConvertScreenToRoot(Point point, out Point rootPoint)
		{
			var root = XamlRoot;
			var wrapper = root?.VisualTree.ContentRoot.GetOwnerWindow()?.NativeWrapper;
			if (wrapper is null
				|| !wrapper.TryConvertScreenToLocal(
					new global::Windows.Graphics.PointInt32((int)Math.Round(point.X), (int)Math.Round(point.Y)),
					out rootPoint))
			{
				rootPoint = default;
				return false;
			}

			return true;
		}
	}
}
