#nullable enable

using System.Numerics;
using SkiaSharp;
using Windows.Graphics;

namespace Microsoft.UI.Composition
{
	public partial class CompositionRoundedRectangleGeometry : CompositionGeometry
	{
		private Vector2 _size;
		private Vector2 _offset;
		private Vector2 _cornerRadius;

		internal CompositionRoundedRectangleGeometry(Compositor compositor) : base(compositor)
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

		public Vector2 CornerRadius
		{
			get => _cornerRadius;
			set => SetProperty(ref _cornerRadius, value);
		}

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
