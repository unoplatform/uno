#nullable disable

using System;
using SkiaSharp;

namespace Windows.UI;

public partial struct Color : IFormattable
{
	public static implicit operator SKColor(Color color) => new SKColor(color.R, color.G, color.B, color.A);

	public static implicit operator Color(SKColor color) => FromArgb(color.Alpha, color.Red, color.Green, color.Blue);
}
