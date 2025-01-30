#nullable enable

using SkiaSharp;
using Windows.Graphics;

namespace Microsoft.UI.Composition
{
	public partial class CompositionRectangleGeometry : CompositionGeometry
	{
		private SkiaGeometrySource2D? _geometrySource2D;

		internal override IGeometrySource2D? BuildGeometry() => _geometrySource2D;

		private SkiaGeometrySource2D? InternalBuildGeometry()
			=> new SkiaGeometrySource2D(BuildRectangleGeometry(Offset, Size));

		private protected override void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
		{
			if (propertyName is nameof(Offset) or nameof(Size))
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
