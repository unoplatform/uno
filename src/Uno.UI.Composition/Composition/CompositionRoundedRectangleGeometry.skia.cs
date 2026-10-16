#nullable enable

using System.Numerics;
using Uno.UI.Composition.Drawing;
using Windows.Graphics;

namespace Microsoft.UI.Composition
{
	public partial class CompositionRoundedRectangleGeometry : CompositionGeometry
	{
		private IGeometry? _geometry;
		private IGeometry? _dashGeometry;

		internal override IGeometrySource2D? BuildGeometry() => _geometry as IGeometrySource2D;

		internal override IGeometry? BuildDashGeometry()
			=> CornerRadius.X == 0 || CornerRadius.Y == 0
				? null
				: _dashGeometry ??= BuildRoundedRectangleGeometry(Offset, Size, CornerRadius, startAtTopLeftArc: true);

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
				_dashGeometry?.Dispose();
				_dashGeometry = null;
			}

			base.OnPropertyChangedCore(propertyName, isSubPropertyChange);
		}

		private protected override void DisposeInternal()
		{
			_geometry?.Dispose();
			_dashGeometry?.Dispose();
			base.DisposeInternal();
		}
	}
}
