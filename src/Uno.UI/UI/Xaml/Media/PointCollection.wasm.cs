using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Windows.Foundation;
using Uno.Extensions;

namespace Microsoft.UI.Xaml.Media
{
	public partial class PointCollection : IEnumerable<Point>, IList<Point>
	{
		internal double[] Flatten()
		{
			if (_points.Count == 0)
			{
				return Array.Empty<double>();
			}

			var buffer = new double[_points.Count * 2];
			for (int i = 0; i < _points.Count; i++)
			{
				buffer[i * 2] = _points[i].X;
				buffer[i * 2 + 1] = _points[i].Y;
			}

			return buffer;
		}
	}
}
