using System;
using Windows.Foundation;

namespace Windows.Graphics.Interop.Direct2D;

internal struct D2D1ArcSegment
{
	public Point Point;
	public Size Size;
	public float RotationAngle;
	public D2D1SweepDirection SweepDirection;
	public D2D1ArcSize ArcSize;
}
