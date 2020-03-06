using Windows.Foundation;
using Windows.UI.Xaml.Wasm;
using Uno.Extensions;
using Uno.UI;

namespace Windows.UI.Xaml.Shapes
{
	partial class Ellipse
	{
		private readonly SvgElement _ellipse = new SvgElement("ellipse");

		public Ellipse()
		{
			SvgChildren.Add(_ellipse);

			InitCommonShapeProperties();
		}

		protected override SvgElement GetMainSvgElement()
		{
			return _ellipse;
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			var minMax = this.GetMinMax();

			var arrangeSize = finalSize
				.AtLeast(minMax.min)
				.AtMost(minMax.max);

			var cx = arrangeSize.Width / 2;
			var cy = arrangeSize.Height / 2;

			var strokeThickness = ActualStrokeThickness;

			_ellipse.SetAttribute(
				("cx", cx.ToStringInvariant()),
				("cy", cy.ToStringInvariant()),
				("rx", (cx - strokeThickness).ToStringInvariant()),
				("ry", (cy - strokeThickness).ToStringInvariant()));

			return base.ArrangeOverride(finalSize);
		}
	}
}
