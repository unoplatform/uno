using CoreGraphics;
using System;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Shapes
{
	public partial class Path : Shape
	{

		/// <inheritdoc />
		protected override Size MeasureOverride(Size availableSize)
			=> MeasureAbsoluteShape(availableSize, GetPath());

		/// <inheritdoc />
		protected override Size ArrangeOverride(Size finalSize)
			=> ArrangeAbsoluteShape(finalSize, GetPath());

		private CGPath GetPath()
		{
			var streamGeometry = Data?.ToStreamGeometry();
			return streamGeometry?.ToCGPath();
		}
	}
}
