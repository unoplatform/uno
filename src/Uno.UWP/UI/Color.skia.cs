using System;
using SkiaSharp;

namespace Windows.UI;

public partial struct Color : IFormattable
{
	internal static implicit operator SKColor(Color color) => new SKColor(color.R, color.G, color.B, color.A);

	internal static implicit operator Color(SKColor color) => FromArgb(color.Alpha, color.Red, color.Green, color.Blue);
}
