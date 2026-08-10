using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;
using Windows.Foundation;

namespace Windows.Graphics.Interop.Direct2D;

internal static partial class D2D1Helpers
{
	public static SKPathFillType ToSkia(this D2D1FillMode mode) => mode is D2D1FillMode.Alternate ? SKPathFillType.EvenOdd : SKPathFillType.Winding;

	public static SKPoint ToSkia(this Point point) => new((float)point.X, (float)point.Y);

	// Enum members are in the reverse order so we can't cast directly
	public static SKPathDirection ToSkia(this D2D1SweepDirection direction) => direction is D2D1SweepDirection.CounterClockwise ? SKPathDirection.CounterClockwise : SKPathDirection.Clockwise;

	public static SKPathArcSize ToSkia(this D2D1ArcSize size) => (SKPathArcSize)size;
}
