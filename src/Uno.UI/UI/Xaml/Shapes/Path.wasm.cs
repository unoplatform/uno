using System;
using Windows.Foundation;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Wasm;

namespace Windows.UI.Xaml.Shapes
{
	partial class Path
	{
		private readonly SvgElement _path = new SvgElement("path");

		public Path()
		{
			SvgChildren.Add(_path);

			InitCommonShapeProperties();
		}

		protected override SvgElement GetMainSvgElement()
		{
			return _path;
		}

		partial void OnDataChanged()
		{
			switch (Data)
			{
				case GeometryData gd:
					_path.SetAttribute(
						("d", gd.Data),
						("fill-rule", gd.FillRule == FillRule.EvenOdd ? "evenodd" : "nonzero"));
					break;
			}
		}
	}
}
