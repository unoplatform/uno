using SkiaSharp;
using Uno.UI.UI.Xaml.Media;

namespace Windows.UI.Xaml.Media
{
	partial class GeometryGroup
	{
		internal override SKPath GetSKPath()
		{
			var path = new SKPath();

			foreach (var geometry in Children)
			{
				var geometryPath = geometry.GetSKPath();
				path.AddPath(geometryPath);
			}

			path.FillType = FillRule.ToSkiaFillType();
			return path;
		}
	}
}
