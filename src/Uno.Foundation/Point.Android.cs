using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.Foundation;

public partial struct Point
{
	internal static Point From(Action<int[]> getter)
	{
		var result = new int[2];
		getter(result);

		return new Point(result[0], result[1]);
	}
}
