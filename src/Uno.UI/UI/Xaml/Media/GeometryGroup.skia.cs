using SkiaSharp;
using Uno.UI.UI.Xaml.Media;

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
