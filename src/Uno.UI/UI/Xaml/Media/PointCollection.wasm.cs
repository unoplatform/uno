using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Windows.Foundation;
using Uno.Extensions;

namespace Windows.UI.Xaml.Media
{
	public partial class PointCollection : IEnumerable<Point>, IList<Point>
	{
		internal string ToCssString()
		{
			var sb = new StringBuilder();
			foreach (var p in _points)
			{
				sb.Append(p.X.ToStringInvariant());
				sb.Append(',');
				sb.Append(p.Y.ToStringInvariant());
				sb.Append(' ');
			}
			return sb.ToString();
		}
	}
}
