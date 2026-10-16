#nullable enable

using Windows.Graphics;

namespace Microsoft.UI.Composition
{
	public partial class CompositionRectangleGeometry : CompositionGeometry
	{
		private global::Uno.UI.Composition.Drawing.IGeometry? _geometry;

		internal override IGeometrySource2D? BuildGeometry() => _geometry as IGeometrySource2D;

		private global::Uno.UI.Composition.Drawing.IGeometry InternalBuildGeometry()
			=> BuildRectangleGeometry(Offset, Size);

		private protected override void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
		{
			if (propertyName is nameof(Offset) or nameof(Size))
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
