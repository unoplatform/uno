#nullable enable

using System.Windows.Input;
using Windows.Graphics;

namespace Microsoft.UI.Composition
{
	public partial class CompositionEllipseGeometry : CompositionGeometry
	{
		private SkiaGeometrySource2D? _geometrySource2D;

		internal override IGeometrySource2D? BuildGeometry() => _geometrySource2D;

		private SkiaGeometrySource2D? InternalBuildGeometry()
			=> new SkiaGeometrySource2D(BuildEllipseGeometry(Center, Radius));

		private protected override void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
		{
			if (propertyName is nameof(Center) or nameof(Radius))
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
