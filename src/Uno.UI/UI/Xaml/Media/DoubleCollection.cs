using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Windows.UI.Xaml.Media
{
	public partial class DoubleCollection : List<double>, IList<double>, IEnumerable<double>
	{
		public DoubleCollection()
		{

		}
		public DoubleCollection(IEnumerable<double> collection) : base(collection)
		{
		}

		static public implicit operator DoubleCollection(string value)
		{
			return value.Split(',', ' ').Select(str => double.Parse(str, CultureInfo.InvariantCulture)).ToArray();
		}

		static public implicit operator DoubleCollection(double[] value)
		{

			return new DoubleCollection(value);
		}
	}
}
