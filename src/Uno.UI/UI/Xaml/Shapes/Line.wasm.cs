using System;
using Uno.Extensions;
using Windows.Foundation;
using Microsoft.UI.Xaml.Wasm;

namespace Microsoft.UI.Xaml.Shapes
{
	partial class Line
	{
		private readonly SvgElement _line = new SvgElement("line");

		partial void InitializePartial()
		{
			SvgChildren.Add(_line);

			InitCommonShapeProperties();
		}

		protected override SvgElement GetMainSvgElement()
		{
			return _line;
		}

		partial void OnX1PropertyChangedPartial(double oldValue, double newValue)
		{
			_line.SetAttribute("x1", newValue.ToStringInvariant());
		}

		partial void OnX2PropertyChangedPartial(double oldValue, double newValue)
		{
			_line.SetAttribute("x2", newValue.ToStringInvariant());
		}

		partial void OnY1PropertyChangedPartial(double oldValue, double newValue)
		{
			_line.SetAttribute("y1", newValue.ToStringInvariant());
		}

		partial void OnY2PropertyChangedPartial(double oldValue, double newValue)
		{
			_line.SetAttribute("y2", newValue.ToStringInvariant());
		}
	}
}
