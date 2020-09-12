using Windows.Foundation;
using Windows.UI.Xaml.Wasm;
using Uno.Extensions;
using Uno.UI;
using NotImplementedException = System.NotImplementedException;

namespace Windows.UI.Xaml.Shapes
{
	partial class Ellipse
	{
		private readonly SvgElement _ellipse = new SvgElement("ellipse");

		public Ellipse()
		{
			SvgChildren.Add(_ellipse);

			InitCommonShapeProperties();

			RegisterPropertyChangedCallback(StrokeProperty, OnStrokeChanged);
		}

		private void OnStrokeChanged(DependencyObject sender, DependencyProperty dp) => InvalidateArrange();

		protected override SvgElement GetMainSvgElement()
		{
			return _ellipse;
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			var size = this.ApplySizeConstraints(finalSize);

			var cx = size.Width / 2;
			var cy = size.Height / 2;

			var halfStrokeThickness = ActualStrokeThickness / 2;

			_ellipse.SetAttribute(
				("cx", cx.ToStringInvariant()),
				("cy", cy.ToStringInvariant()),
				("rx", (cx - halfStrokeThickness).ToStringInvariant()),
				("ry", (cy - halfStrokeThickness).ToStringInvariant()));

			return base.ArrangeOverride(finalSize);
		}
	}
}
