using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml.Wasm;
using Uno.Extensions;

namespace Windows.UI.Xaml.Shapes
{
	partial class Polygon
	{
		protected override Size MeasureOverride(Size availableSize)
		{
			var points = Points;
			if (points == null)
			{
				_mainSvgElement.RemoveAttribute("points");
			}
			else
			{
				_mainSvgElement.SetAttribute("points", points.ToCssString());
			}

			return MeasureAbsoluteShape(availableSize, this);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			UpdateRender();
			return ArrangeAbsoluteShape(finalSize, this);
		}
	}
}
