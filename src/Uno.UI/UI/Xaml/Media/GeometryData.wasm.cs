using System;
using Windows.UI.Xaml.Wasm;

namespace Windows.UI.Xaml.Media
{
	public class GeometryData : Geometry
	{
		// This is a special class to support the "Data Path" string on Wasm platform.
		// Since the native underlying system (SVG) is using data paths "as is", there's
		// no need to parse it in managed code, except to extract the FillRule.

		// This special geometry will be created when setting a string data directly in XAML
		// like this:
		//
		// <Path Data="M0,0 L10,10 20,10 20,20, 10,20 Z" />

		private readonly SvgElement _svgElement = new SvgElement("path");

		public string Data { get; }

		public FillRule FillRule { get; } = FillRule.EvenOdd;

		public GeometryData()
		{
		}

		public GeometryData(string data)
		{
			if ((data.StartsWith('F') || data.StartsWith('f')) && data.Length > 2)
			{
				// TODO: support spaces between the F and the 0/1

				FillRule = data[1] == '1' ? FillRule.Nonzero : FillRule.EvenOdd;
				Data = data.Substring(2);
			}
			else
			{
				Data = data;
			}

			_svgElement.SetAttribute("d", Data);
			var rule = FillRule switch
			{
				FillRule.EvenOdd => "evenodd",
				FillRule.Nonzero => "nonzero",
				_ => "evenodd"
			};
			_svgElement.SetAttribute("fill-rule", rule);
		}

		internal override SvgElement GetSvgElement() => _svgElement;

		// There is no .ToPathData() on GeometryData, because it's not possible
		// to have this in a GeometryGroup.
	}
}
