#nullable enable

using SkiaSharp;
using System.Numerics;
using Windows.Graphics;

namespace Microsoft.UI.Composition
{
	public partial class CompositionRoundedRectangleGeometry : CompositionGeometry
	{
		private SkiaGeometrySource2D? _geometrySource2D;

		internal override IGeometrySource2D? BuildGeometry() => _geometrySource2D;

		private SkiaGeometrySource2D? InternalBuildGeometry()
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

		private protected override void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
		{
			if (propertyName is nameof(Offset) or nameof(Size) or nameof(CornerRadius))
			{
				_geometrySource2D?.Dispose();
				_geometrySource2D = InternalBuildGeometry();
			}

			base.OnPropertyChangedCore(propertyName, isSubPropertyChange);
		}

		private protected override void DisposeInternal()
		{
			_geometrySource2D?.Dispose();
			base.DisposeInternal();
		}
	}
}
