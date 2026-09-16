using Uno.UI.Composition;
using Uno.UI.Composition.Drawing;


namespace Microsoft.UI.Xaml.Media
{
	partial class GeometryGroup
	{
		internal override IGeometry GetGeometry()
		{
			var builder = GeometryFactory.Current.CreatePrimitiveGeometryBuilder();
			builder.FillRule = FillRule == FillRule.EvenOdd ? GeometryFillRule.EvenOdd : GeometryFillRule.NonZero;

			foreach (var geometry in Children)
			{
				// Use GetTransformedGeometry so each child's own Transform is applied
				if (geometry.GetTransformedGeometry() is { } childGeometry)
				{
					builder.AddGeometry(childGeometry);
				}
			}

			return builder.Build();
		}
	}
}
