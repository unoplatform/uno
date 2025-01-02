using System;
using Uno.Extensions;
using Windows.Foundation;
using Microsoft.UI.Xaml.Wasm;
using Uno.UI.Xaml;

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

			WindowManagerInterop.SetSvgEllipseAttributes(_mainSvgElement.HtmlId, cx, cy, cx - halfStrokeThickness, cy - halfStrokeThickness);

			return finalSize;
		}

		private protected override string GetBBoxCacheKeyImpl() =>
#if DEBUG
			throw new InvalidOperationException("Elipse doesnt use GetBBox. Should the impl change in the future, add key-gen and invalidation mechanism.");
#else
			null;
#endif
	}
}
