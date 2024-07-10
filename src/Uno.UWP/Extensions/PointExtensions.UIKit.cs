using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreGraphics;
using Windows.Foundation;

namespace Uno.UI.Xaml.Extensions;

internal static class PointExtensions
{
	public static Point ToPoint(this CGPoint point) => new(point.X, point.Y);
}
