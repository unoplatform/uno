#nullable enable

using System.Numerics;
using Windows.Graphics;

namespace Microsoft.UI.Composition
{
	public partial class CompositionLineGeometry : CompositionGeometry
	{
		private Vector2 _start;
		private Vector2 _end;

		internal CompositionLineGeometry(Compositor compositor) : base(compositor)
		{

		}

		public Vector2 Start
		{
			get => _start;
			set => SetProperty(ref _start, value);
		}

		public Vector2 End
		{
			get => _end;
			set => SetProperty(ref _end, value);
		}

		private SkiaGeometrySource2D? _geometrySource2D;

		internal override IGeometrySource2D? BuildGeometry() => _geometrySource2D;

		private SkiaGeometrySource2D? InternalBuildGeometry()
			=> new SkiaGeometrySource2D(BuildLineGeometry(Start, End));

		private protected override void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
		{
			if (propertyName is nameof(Start) or nameof(End))
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
