using SkiaSharp;
using Uno.UI.UI.Xaml.Media;

#pragma warning disable CS0618 // SkiaSharp 4: intentional use of deprecated mutable SKPath/SKCanvas API (SKPathBuilder/SKSamplingOptions migration deferred)

namespace Microsoft.UI.Xaml.Media
{
	partial class GeometryGroup
	{
		internal override SKPath GetSKPath()
		{
			var path = new SKPath();

			foreach (var geometry in Children)
			{
				// Use GetTransformedSKPath so each child's own Transform is applied
				var geometryPath = geometry.GetTransformedSKPath();
				path.AddPath(geometryPath);
			}

			path.FillType = FillRule.ToSkiaFillType();
			return path;
		}
	}
}
