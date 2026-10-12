#nullable enable

using System.Windows.Input;
using Windows.Graphics;

namespace Microsoft.UI.Composition
{
	public partial class CompositionEllipseGeometry : CompositionGeometry
	{
		private global::Uno.UI.Composition.Drawing.IGeometry? _geometry;

		internal override IGeometrySource2D? BuildGeometry() => _geometry as IGeometrySource2D;

		private global::Uno.UI.Composition.Drawing.IGeometry InternalBuildGeometry()
			=> BuildEllipseGeometry(Center, Radius);

		private protected override void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
		{
			if (propertyName is nameof(Center) or nameof(Radius))
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
