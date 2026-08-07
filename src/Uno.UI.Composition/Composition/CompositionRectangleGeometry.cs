#nullable enable

using System.Numerics;
using SkiaSharp;
using Windows.Graphics;

namespace Microsoft.UI.Composition
{
	public partial class CompositionRectangleGeometry : CompositionGeometry
	{
		private Vector2 _size;
		private Vector2 _offset;

		internal CompositionRectangleGeometry(Compositor compositor) : base(compositor)
		{

		}

		public Vector2 Size
		{
			get => _size;
			set => SetProperty(ref _size, value);
		}

		public Vector2 Offset
		{
			get => _offset;
			set => SetProperty(ref _offset, value);
		}

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
