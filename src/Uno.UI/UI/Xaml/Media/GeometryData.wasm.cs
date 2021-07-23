using System;
using Windows.UI.Xaml.Wasm;

namespace Windows.UI.Xaml.Media
{
	public class GeometryData : Geometry
	{
		private readonly SvgElement _svgElement = new SvgElement("path");

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

			_svgElement.SetAttribute("d", data);
		}

		internal override SvgElement GetSvgElement() => _svgElement;
	}
}
