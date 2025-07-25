using Uno.Media;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Shapes
{
	public partial class Polygon : Shape
	{
		/// <inheritdoc />
		protected override Size MeasureOverride(Size availableSize)
			=> MeasureAbsoluteShape(availableSize, GetPath());

		/// <inheritdoc />
		protected override Size ArrangeOverride(Size finalSize)
			=> ArrangeAbsoluteShape(finalSize, GetPath());

		private APath GetPath()
		{
			var coords = Points;

			if (coords == null || coords.Count <= 1)
			{
				return null;
			}

			var output = GetOrCreatePath();
			var startingPoint = coords[0];

			output.MoveTo((float)startingPoint.X, (float)startingPoint.Y);
			for (var i = 1; i < coords.Count; i++)
			{
				output.LineTo((float)coords[i].X, (float)coords[i].Y);
			}
			output.LineTo((float)startingPoint.X, (float)startingPoint.Y);

			return output;

		}
	}
}
