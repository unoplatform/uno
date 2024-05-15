using Uno.Extensions;
using Windows.Foundation;
using Microsoft.UI.Xaml.Wasm;

namespace Microsoft.UI.Xaml.Shapes
{
	partial class Ellipse
	{
		public Ellipse() : base("ellipse")
		{
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			UpdateRender();
			var (shapeSize, _) = ArrangeRelativeShape(finalSize);

			var cx = shapeSize.Width / 2;
			var cy = shapeSize.Height / 2;

			var halfStrokeThickness = ActualStrokeThickness / 2;

			_mainSvgElement.SetAttribute(
				("cx", cx.ToStringInvariant()),
				("cy", cy.ToStringInvariant()),
				("rx", (cx - halfStrokeThickness).ToStringInvariant()),
				("ry", (cy - halfStrokeThickness).ToStringInvariant()));

			return shapeSize;
		}
	}
}
