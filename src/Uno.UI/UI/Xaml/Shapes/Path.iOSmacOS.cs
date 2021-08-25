#nullable enable
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
			=> MeasureAbsoluteShape(availableSize, GetPathAndFillRule(out _));

		/// <inheritdoc />
		protected override Size ArrangeOverride(Size finalSize)
			=> ArrangeAbsoluteShape(finalSize, GetPathAndFillRule(out var fillRule), fillRule);

		private CGPath? GetPathAndFillRule(out FillRule fillRule)
		{
			var streamGeometry = Data?.ToStreamGeometry();
			fillRule = streamGeometry?.FillRule ?? FillRule.EvenOdd;
			return streamGeometry?.ToCGPath();
		}
	}
}
