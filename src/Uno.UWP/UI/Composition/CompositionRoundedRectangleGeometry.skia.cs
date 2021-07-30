#nullable enable

using System.Numerics;
using SkiaSharp;
using Windows.Graphics;

namespace Windows.UI.Composition
{
	public partial class CompositionRoundedRectangleGeometry : CompositionGeometry
	{
		internal override IGeometrySource2D? BuildGeometry()
		{
			SKPath? path;

			Vector2 cornerRadius = CornerRadius;
			if (cornerRadius.X == 0 || cornerRadius.Y == 0)
			{
				// Simple rectangle
				path = BuildRectangleGeometry(Offset, Size);
			}
			else
			{
				// Complex rectangle
				path = BuildRoundedRectangleGeometry(Offset, Size, CornerRadius);
			}

			return new SkiaGeometrySource2D(path);
		}
	}
}
