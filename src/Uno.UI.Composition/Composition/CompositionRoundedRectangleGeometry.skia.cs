#nullable enable

using System.Numerics;
using Uno.UI.Composition.Drawing;
using Windows.Graphics;

namespace Microsoft.UI.Composition
{
	public partial class CompositionRoundedRectangleGeometry : CompositionGeometry
	{
		private IGeometry? _geometry;

		internal override IGeometrySource2D? BuildGeometry() => _geometry as IGeometrySource2D;

		private IGeometry InternalBuildGeometry()
		{
			Vector2 cornerRadius = CornerRadius;
			return cornerRadius.X == 0 || cornerRadius.Y == 0
				// Simple rectangle
				? BuildRectangleGeometry(Offset, Size)
				// Complex rectangle
				: BuildRoundedRectangleGeometry(Offset, Size, CornerRadius);
		}

		private protected override void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
		{
			if (propertyName is nameof(Offset) or nameof(Size) or nameof(CornerRadius))
			{
				_geometry?.Dispose();
				_geometry = InternalBuildGeometry();
			}

			base.OnPropertyChangedCore(propertyName, isSubPropertyChange);
		}

		private protected override void DisposeInternal()
		{
			_geometry?.Dispose();
			base.DisposeInternal();
		}
	}
}
