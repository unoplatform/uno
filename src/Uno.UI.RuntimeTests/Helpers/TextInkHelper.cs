#nullable enable

using System;
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

	// True when the dots after the letters are drawn on their right, as in "Hello world......".
	public static bool DotsOnTheRight((InkSpan Ink, InkSpan Letters) ink)
		=> ink.Ink.Max - ink.Letters.Max > ink.Letters.Min - ink.Ink.Min;
}
