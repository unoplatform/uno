using Windows.Foundation;
using Uno.Extensions;
using Microsoft.UI.Composition;
using System.Numerics;
using Uno.UI.Composition.Drawing;


namespace Microsoft.UI.Xaml.Shapes
{
	partial class Ellipse : Shape
	{
		public Ellipse()
		{
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			var (_, renderingArea) = ArrangeRelativeShape(finalSize);

			Render(renderingArea.Width > 0 && renderingArea.Height > 0
				? GetGeometry(renderingArea)
				: null);

			return finalSize;
		}

		private IGeometry GetGeometry(Rect renderingArea)
		{
			var builder = GeometryFactory.Current.CreatePrimitiveGeometryBuilder();
			var center = new Vector2((float)(renderingArea.X + renderingArea.Width / 2), (float)(renderingArea.Y + renderingArea.Height / 2));
			builder.AddEllipse(center, (float)(renderingArea.Width / 2), (float)(renderingArea.Height / 2));
			return builder.Build();
		}
	}
}
