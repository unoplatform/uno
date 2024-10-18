using System;
using Uno.Extensions;
using Windows.Foundation;
using Windows.UI.Xaml.Wasm;

namespace Windows.UI.Xaml.Shapes
{
	partial class Line
	{
		public Line() : base("line")
		{
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			_mainSvgElement.SetAttribute(
				("x1", X1.ToStringInvariant()),
				("x2", X2.ToStringInvariant()),
				("y1", Y1.ToStringInvariant()),
				("y2", Y2.ToStringInvariant())
			);
			return MeasureAbsoluteShape(availableSize, this);
		}

		/// <inheritdoc />
		protected override Size ArrangeOverride(Size finalSize)
		{
			UpdateRender();
			return ArrangeAbsoluteShape(finalSize, this);
		}
	}
}
