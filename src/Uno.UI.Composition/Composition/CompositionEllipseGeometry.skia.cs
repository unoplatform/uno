#nullable enable

using System.Windows.Input;
using Windows.Graphics;

namespace Microsoft.UI.Composition
{
	public partial class CompositionEllipseGeometry : CompositionGeometry
	{
		private global::Uno.UI.Composition.Drawing.IGeometry? _geometry;
		private global::Uno.UI.Composition.Drawing.IGeometry? _dashGeometry;

		internal override IGeometrySource2D? BuildGeometry() => _geometry as IGeometrySource2D;

		internal override global::Uno.UI.Composition.Drawing.IGeometry? BuildDashGeometry()
			=> _dashGeometry ??= BuildEllipseGeometry(Center, Radius, EllipseFigureStart.Left);

		private global::Uno.UI.Composition.Drawing.IGeometry InternalBuildGeometry()
			=> BuildEllipseGeometry(Center, Radius);

		private protected override void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
		{
			if (propertyName is nameof(Center) or nameof(Radius))
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
