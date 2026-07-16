#nullable enable

using System;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
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

			var text = GetPlainTextContent();
			index = Math.Clamp(index, 0, text.Length);
			var local = displayBlock.ParsedText.GetRectForIndex(index);
			return TryTransformRectFromDisplaySpace(displayBlock, local, options, out rect);
		}

		internal bool TryGetIndexBaseline(int index, PointOptions options, out double baseline)
		{
			baseline = 0;
			if (_textBoxView?.DisplayBlock is not { } displayBlock)
			{
				return false;
			}

			var text = GetPlainTextContent();
			index = Math.Clamp(index, 0, text.Length);
			var rect = displayBlock.ParsedText.GetRectForIndex(index);
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
			return TryTransformRectFromDisplaySpace(displayBlock, local, options, out rect);
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

			if (!TryTransformPointToDisplaySpace(displayBlock, point, options, out var local))
			{
				return false;
			}
			index = Math.Clamp(
				displayBlock.ParsedText.GetIndexAt(local, ignoreEndingNewLine: true, extendedSelection: true),
				0,
				GetPlainTextContent().Length);
			return true;
		}

		// Scrolls the range [start,end) into view through the hosting ScrollViewer.
		internal bool TryScrollRangeIntoView(int start, int end, PointOptions options)
		{
			if (_textBoxView?.DisplayBlock is not { } displayBlock || _contentElement is not ScrollViewer scrollViewer)
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

			var index = options.HasFlag(PointOptions.Start) ? start : end;
			var caretRect = displayBlock.ParsedText.GetRectForIndex(index) with { Width = TextBlock.CaretThickness };
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

			var left = double.PositiveInfinity;
			var right = double.NegativeInfinity;
			var top = double.PositiveInfinity;
			var bottom = double.NegativeInfinity;
			for (var i = start; i <= end; i++)
			{
				var current = parsed.GetRectForIndex(i);
				left = Math.Min(left, current.X);
				right = Math.Max(right, current.X + current.Width);
				top = Math.Min(top, current.Y);
				bottom = Math.Max(bottom, current.Y + current.Height);
			}

			return new Rect(left, top, right - left, bottom - top);
		}

		private bool TryTransformRectFromDisplaySpace(TextBlock displayBlock, Rect rect, PointOptions options, out Rect transformed)
		{
			try
			{
				if (options.HasFlag(PointOptions.ClientCoordinates))
				{
					transformed = displayBlock.TransformToVisual(this).TransformBounds(rect);
					return true;
				}

				var rootRect = displayBlock.TransformToVisual(null).TransformBounds(rect);
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
					return true;
				}

				if (!TryConvertScreenToRoot(point, out var rootPoint))
				{
					transformed = default;
					return false;
				}
				var root = (XamlRoot?.Content as UIElement) ?? this;
				transformed = root.TransformToVisual(displayBlock).TransformPoint(rootPoint);
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
				if (options.HasFlag(PointOptions.ClientCoordinates))
				{
					transformed = displayBlock.TransformToVisual(this).TransformPoint(point);
					return true;
				}

				return TryConvertRootToScreen(displayBlock.TransformToVisual(null).TransformPoint(point), out transformed);
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
