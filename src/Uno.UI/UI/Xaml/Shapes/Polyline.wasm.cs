using System;
using System.Linq;
using Windows.Foundation;
using Microsoft.UI.Xaml.Wasm;
using Uno;
using Uno.Extensions;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Shapes
{
	partial class Polyline
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
