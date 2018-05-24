using System;

namespace Windows.UI.Xaml.Media
{
	public class GeometryData : Geometry
	{
		public string Data { get; }

		public FillRule FillRule { get; } = FillRule.EvenOdd;

		public GeometryData()
		{
		}

		public GeometryData(string data)
		{
			if (data.StartsWith("F", StringComparison.InvariantCultureIgnoreCase) && data.Length > 2)
			{
				FillRule = data[1] == '1' ? FillRule.Nonzero : FillRule.EvenOdd;
				Data = data.Substring(2);
			}
			else
			{
				Data = data;
			}
		}
	}
}
