#nullable enable

using System;

namespace Uno.UI.Runtime.Wayland;

// Fills the drag icon's shared-memory buffer (premultiplied BGRA8, tightly packed) on the CPU.
internal static class WaylandDragIconPixels
{
	internal const int FallbackLogicalSize = 32;

	/// <summary>Resamples <paramref name="image"/> into the buffer, or draws a generic page when there is none.</summary>
	internal static void Render(Span<byte> buffer, int width, int height, WaylandDragIconImage? image)
	{
		buffer.Slice(0, width * height * 4).Clear();
		if (image is not null && image.Width > 0 && image.Height > 0 && image.Pixels.Length >= image.Width * image.Height * 4)
		{
			Resample(image.Pixels, image.Width, image.Height, buffer, width, height);
		}
		else
		{
			DrawPage(buffer, width, height);
		}
	}

	// Bilinear on premultiplied channels, sampling pixel centres.
	private static void Resample(ReadOnlySpan<byte> source, int sourceWidth, int sourceHeight, Span<byte> target, int width, int height)
	{
		if (sourceWidth == width && sourceHeight == height)
		{
			source.Slice(0, width * height * 4).CopyTo(target);
			return;
		}
		var scaleX = sourceWidth / (double)width;
		var scaleY = sourceHeight / (double)height;
		for (var y = 0; y < height; y++)
		{
			var sy = Math.Clamp((y + 0.5) * scaleY - 0.5, 0, sourceHeight - 1);
			var y0 = (int)sy;
			var y1 = Math.Min(y0 + 1, sourceHeight - 1);
			var fy = sy - y0;
			for (var x = 0; x < width; x++)
			{
				var sx = Math.Clamp((x + 0.5) * scaleX - 0.5, 0, sourceWidth - 1);
				var x0 = (int)sx;
				var x1 = Math.Min(x0 + 1, sourceWidth - 1);
				var fx = sx - x0;
				var i00 = (y0 * sourceWidth + x0) * 4;
				var i01 = (y0 * sourceWidth + x1) * 4;
				var i10 = (y1 * sourceWidth + x0) * 4;
				var i11 = (y1 * sourceWidth + x1) * 4;
				var o = (y * width + x) * 4;
				for (var c = 0; c < 4; c++)
				{
					var top = source[i00 + c] + (source[i01 + c] - source[i00 + c]) * fx;
					var bottom = source[i10 + c] + (source[i11 + c] - source[i10 + c]) * fx;
					target[o + c] = (byte)Math.Clamp((int)Math.Round(top + (bottom - top) * fy), 0, 255);
				}
			}
		}
	}

	// A page outline with a folded corner and three text lines: the drag started before anything was rendered for it.
	private static void DrawPage(Span<byte> target, int width, int height)
	{
		var unit = width / (double)FallbackLogicalSize;
		var stroke = Math.Max(1, (int)Math.Round(1.5 * unit));
		int left = (int)Math.Round(6 * unit), top = (int)Math.Round(3 * unit);
		int right = Math.Min(width, (int)Math.Round(26 * unit)), bottom = Math.Min(height, (int)Math.Round(29 * unit));
		var fold = (int)Math.Round(7 * unit);
		var foldX = right - fold;
		for (var y = top; y < bottom; y++)
		{
			for (var x = left; x < right; x++)
			{
				// Distance past the fold diagonal; positive pixels are cut away.
				var cut = x - foldX - (y - top);
				if (cut > 0)
				{
					continue;
				}
				var edge = x - left < stroke || right - 1 - x < stroke || y - top < stroke || bottom - 1 - y < stroke
					|| cut > -stroke * 1.5
					|| (y < top + fold && Math.Abs(x - foldX) < stroke / 2.0 + 0.5)
					|| (x >= foldX && Math.Abs(y - (top + fold)) < stroke / 2.0 + 0.5);
				Set(target, width, x, y, edge ? (byte)0x40 : (byte)0xE6, edge ? (byte)0xFF : (byte)0xE6);
			}
		}
		for (var line = 0; line < 3; line++)
		{
			var y0 = top + (int)Math.Round((12 + line * 4) * unit);
			for (var y = y0; y < Math.Min(bottom, y0 + stroke); y++)
			{
				for (var x = left + (int)Math.Round(4 * unit); x < right - (int)Math.Round(4 * unit); x++)
				{
					Set(target, width, x, y, 0x40, 0xFF);
				}
			}
		}
	}

	private static void Set(Span<byte> target, int width, int x, int y, byte gray, byte alpha)
	{
		var o = (y * width + x) * 4;
		target[o] = target[o + 1] = target[o + 2] = gray;
		target[o + 3] = alpha;
	}
}
