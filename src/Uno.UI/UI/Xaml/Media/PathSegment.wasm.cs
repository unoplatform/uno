using System;
using System.Collections.Generic;

namespace Windows.UI.Xaml.Media
{
	partial class PathSegment
	{
		internal virtual IEnumerable<IFormattable> ToDataStream()
		{
			throw new NotSupportedException($"ToDataStream() not supported on {GetType().Name}");
		}
	}
}
