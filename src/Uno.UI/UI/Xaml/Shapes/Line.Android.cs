using Windows.Foundation;
using System;
using Uno.Media;
using Uno.UI;

namespace Windows.UI.Xaml.Shapes
{
	public partial class Line : Shape
	{
		/// <inheritdoc />
		protected override Size MeasureOverride(Size availableSize)
			=> MeasureAbsoluteShape(availableSize, GetPath());

		/// <inheritdoc />
		protected override Size ArrangeOverride(Size finalSize)
			=> ArrangeAbsoluteShape(finalSize, GetPath());

		private Android.Graphics.Path GetPath()
		{
			var output = GetOrCreatePath();

			output.MoveTo((float)X1, (float)Y1);
			output.LineTo((float)X2, (float)Y2);

			return output;
		}
	}
}
