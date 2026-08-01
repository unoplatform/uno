using SkiaSharp;
using Uno.UI.UI.Xaml.Media;


namespace Microsoft.UI.Xaml.Media
{
	partial class GeometryGroup
	{
		internal override SKPath GetSKPath()
		{
			var builder = new SKPathBuilder();

			foreach (var geometry in Children)
			{
				// Use GetTransformedSKPath so each child's own Transform is applied
				var geometryPath = geometry.GetTransformedSKPath();
				builder.AddPath(geometryPath, SKPathAddMode.Append);
			}

			builder.FillType = FillRule.ToSkiaFillType();
			return builder.Detach();
		}
	}
}
