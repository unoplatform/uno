#nullable enable

using System;
using SkiaSharp;
using Uno.UI.Runtime.Skia;

namespace Uno.UI.Runtime.Skia.Headless;

internal static class HeadlessPixelFormatExtensions
{
	public static SKColorType ToSkColorType(this HeadlessPixelFormat format)
		=> format switch
		{
			HeadlessPixelFormat.Bgra8888 => SKColorType.Bgra8888,
			HeadlessPixelFormat.Rgba8888 => SKColorType.Rgba8888,
			_ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported headless pixel format."),
		};
}
