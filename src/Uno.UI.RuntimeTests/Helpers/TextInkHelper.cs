#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Helpers;

// Finds where text is drawn from the colour of its ink, which tells the visual order of bidi text apart
// without trusting the control's own geometry (character rects can disagree with what is drawn).
internal static class TextInkHelper
{
	public readonly record struct InkSpan(int Min, int Max)
	{
		public bool IsEmpty => Max < 0;

		public override string ToString() => IsEmpty ? "none" : $"{Min}..{Max}";
	}

	// Shows the element at the top left of a white, left-to-right host, so screenshot x is physical.
	public static async Task<(Grid Host, RawBitmap Bitmap)> Show(FrameworkElement element, double width = 340, double height = 120)
	{
		var host = new Grid
		{
			Width = width,
			Height = height,
			Background = new SolidColorBrush(Colors.White),
			FlowDirection = FlowDirection.LeftToRight,
		};
		element.HorizontalAlignment = HorizontalAlignment.Left;
		element.VerticalAlignment = VerticalAlignment.Top;
		host.Children.Add(element);
		await UITestHelper.Load(host);
		await WindowHelper.WaitForIdle();
		return (host, await UITestHelper.ScreenShot(host));
	}

	public static async Task<RawBitmap> Capture(Grid host)
	{
		await WindowHelper.WaitForIdle();
		return await UITestHelper.ScreenShot(host);
	}

	// Anti-aliased edges blend towards the white background, so only pixels close to the colour count.
	private static bool IsInk(Color pixel, Color color)
		=> pixel.A > 128 && Math.Abs(pixel.R - color.R) + Math.Abs(pixel.G - color.G) + Math.Abs(pixel.B - color.B) < 90;

	public static InkSpan InkX(RawBitmap bitmap, Color color, int top = 0, int bottom = int.MaxValue)
	{
		int min = int.MaxValue, max = -1;
		bottom = Math.Min(bottom, bitmap.Height - 1);
		for (var y = Math.Max(0, top); y <= bottom; y++)
		{
			for (var x = 0; x < bitmap.Width; x++)
			{
				if (IsInk(bitmap.GetPixel(x, y), color))
				{
					min = Math.Min(min, x);
					max = Math.Max(max, x);
				}
			}
		}
		return new InkSpan(min, max);
	}

	public static (int Top, int Bottom) InkY(RawBitmap bitmap, Color color)
	{
		int top = int.MaxValue, bottom = -1;
		for (var y = 0; y < bitmap.Height; y++)
		{
			for (var x = 0; x < bitmap.Width; x++)
			{
				if (IsInk(bitmap.GetPixel(x, y), color))
				{
					top = Math.Min(top, y);
					bottom = Math.Max(bottom, y);
					break;
				}
			}
		}
		return (top, bottom);
	}

	// Splits one colour's ink into letters (columns with ink in the upper half of the band) and the trailing
	// dots (columns with ink only in the lower half), for text such as "Hello world......".
	public static (InkSpan Ink, InkSpan Letters) Letters(RawBitmap bitmap, Color color, int top = 0, int bottom = int.MaxValue)
	{
		bottom = Math.Min(bottom, bitmap.Height - 1);
		int inkTop = int.MaxValue, inkBottom = -1;
		for (var y = Math.Max(0, top); y <= bottom; y++)
		{
			for (var x = 0; x < bitmap.Width; x++)
			{
				if (IsInk(bitmap.GetPixel(x, y), color))
				{
					inkTop = Math.Min(inkTop, y);
					inkBottom = Math.Max(inkBottom, y);
				}
			}
		}
		if (inkBottom < 0)
		{
			return (new InkSpan(int.MaxValue, -1), new InkSpan(int.MaxValue, -1));
		}

		var middle = inkTop + (inkBottom - inkTop) / 2;
		var tall = InkX(bitmap, color, inkTop, middle - 1);
		return (InkX(bitmap, color, inkTop, inkBottom), tall);
	}

	// The extent of a colour's ink, the mean x of its pixels from the extent's left edge, and the number of pieces it
	// makes between columns without ink. A word drawn twice with the same glyphs gives the same profile, give or take
	// anti-aliasing; a letter that takes another form, as an Arabic letter that no longer joins, changes it.
	public readonly record struct InkProfile(InkSpan Span, double Center, int Pieces)
	{
		public int Width => Span.IsEmpty ? 0 : Span.Max - Span.Min + 1;

		public override string ToString() => $"{Span} (width {Width}, center {Center:0.0}, {Pieces} pieces)";
	}

	// The profile of the ink between two columns, both included.
	public static InkProfile Profile(RawBitmap bitmap, Color color, int left = 0, int right = int.MaxValue)
	{
		int min = int.MaxValue, max = -1;
		right = Math.Min(right, bitmap.Width - 1);
		for (var x = Math.Max(0, left); x <= right; x++)
		{
			if (HasInk(bitmap, color, x))
			{
				min = Math.Min(min, x);
				max = Math.Max(max, x);
			}
		}
		var span = new InkSpan(min, max);
		if (span.IsEmpty)
		{
			return new InkProfile(span, 0, 0);
		}

		long sum = 0, count = 0;
		var pieces = 0;
		var previousHasInk = false;
		for (var x = span.Min; x <= span.Max; x++)
		{
			var hasInk = false;
			for (var y = 0; y < bitmap.Height; y++)
			{
				if (IsInk(bitmap.GetPixel(x, y), color))
				{
					sum += x - span.Min;
					count++;
					hasInk = true;
				}
			}
			if (hasInk && !previousHasInk)
			{
				pieces++;
			}
			previousHasInk = hasInk;
		}
		return new InkProfile(span, (double)sum / count, pieces);
	}

	// The pieces of a colour's ink between columns without any, from left to right.
	public static List<InkSpan> Pieces(RawBitmap bitmap, Color color)
	{
		var pieces = new List<InkSpan>();
		var start = -1;
		for (var x = 0; x <= bitmap.Width; x++)
		{
			var hasInk = x < bitmap.Width && HasInk(bitmap, color, x);
			if (hasInk && start < 0)
			{
				start = x;
			}
			else if (!hasInk && start >= 0)
			{
				pieces.Add(new InkSpan(start, x - 1));
				start = -1;
			}
		}
		return pieces;
	}

	// True when the ink between two columns is drawn further right at its middle height than its extent's centre, as
	// ")" is, and false for "(".
	public static bool BulgesRight(RawBitmap bitmap, Color color, InkSpan columns)
	{
		int top = int.MaxValue, bottom = -1;
		for (var y = 0; y < bitmap.Height; y++)
		{
			for (var x = columns.Min; x <= columns.Max; x++)
			{
				if (IsInk(bitmap.GetPixel(x, y), color))
				{
					top = Math.Min(top, y);
					bottom = Math.Max(bottom, y);
					break;
				}
			}
		}
		var middle = top + (bottom - top) / 2;
		long sum = 0, count = 0;
		for (var y = middle - 1; y <= middle + 1; y++)
		{
			for (var x = columns.Min; x <= columns.Max; x++)
			{
				if (IsInk(bitmap.GetPixel(x, y), color))
				{
					sum += x;
					count++;
				}
			}
		}
		return count > 0 && (double)sum / count > (columns.Min + columns.Max) / 2.0;
	}

	private static bool HasInk(RawBitmap bitmap, Color color, int x)
	{
		for (var y = 0; y < bitmap.Height; y++)
		{
			if (IsInk(bitmap.GetPixel(x, y), color))
			{
				return true;
			}
		}
		return false;
	}

	// True when the dots after the letters are drawn on their right, as in "Hello world......".
	public static bool DotsOnTheRight((InkSpan Ink, InkSpan Letters) ink)
		=> ink.Ink.Max - ink.Letters.Max > ink.Letters.Min - ink.Ink.Min;
}
