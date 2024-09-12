using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using CoreGraphics;

namespace Windows.Foundation;

public partial struct Point
{
	public static implicit operator Point(CGPoint point) => new Point(point.X, point.Y);

	public static implicit operator CGPoint(Point point) => new CGPoint(point.X, point.Y);
}
