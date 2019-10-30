using Windows.Foundation;
using Windows.UI.Xaml.Wasm;
using Uno.Extensions;

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

		protected override Size MeasureOverride(Size availableSize)
		{
			var width = double.IsNaN(Width) ? LimitWithUserSize(availableSize.Width, Width, 100.0d) : Width;
			var height = double.IsNaN(Height) ? LimitWithUserSize(availableSize.Height, Height, 100.0d) : Height;

			var cx = width / 2;
			var cy = height / 2;

			var strokeThickness = ActualStrokeThickness;

			_ellipse.SetAttribute(
				("cx", cx.ToStringInvariant()),
				("cy", cy.ToStringInvariant()),
				("rx", (cx - strokeThickness).ToStringInvariant()),
				("ry", (cy - strokeThickness).ToStringInvariant()));

			return base.MeasureOverride(availableSize);
		}
	}
}
