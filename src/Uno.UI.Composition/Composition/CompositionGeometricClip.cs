using System;
using SkiaSharp;
using Windows.ApplicationModel.Contacts;
using Windows.Foundation;
#nullable enable

namespace Microsoft.UI.Composition
{
	public partial class CompositionGeometricClip : CompositionClip
	{
		private CompositionViewBox? _viewBox;
		private CompositionGeometry? _geometry;

		public CompositionGeometricClip(Compositor compositor) : base(compositor)
		{

		}

		public CompositionViewBox? ViewBox
		{
			get => _viewBox;
			set => SetProperty(ref _viewBox, value);
		}

		public CompositionGeometry? Geometry
		{
			get => _geometry;
			set => SetProperty(ref _geometry, value);
		}

		private protected override Rect? GetBoundsCore(Visual visual)
		{
			if (Geometry is not null)
			{
				var geometry = Geometry.BuildGeometry();

				if (geometry is SkiaGeometrySource2D skiaGeometrySource)
				{
					return skiaGeometrySource.Geometry.TightBounds.ToRect();
				}
				else
				{
					throw new InvalidOperationException($"Clipping with source {geometry} is not supported");
				}
			}

			return null;
		}

		private static readonly SKPath _spareTransformedPath = new();

		internal override SKPath? GetClipPath(Visual visual)
		{
			if (Geometry is not null)
			{
				var geometry = Geometry.BuildGeometry();

				if (geometry is SkiaGeometrySource2D geometrySource)
				{
					var path = geometrySource.Geometry;
					if (!TransformMatrix.IsIdentity)
					{
						var transformedPath = _spareTransformedPath;
						transformedPath.Reset();
						path.Transform(TransformMatrix.ToSKMatrix(), transformedPath);
						path = transformedPath;
					}

					return path;
				}
				else
				{
					throw new InvalidOperationException($"Clipping with source {geometry} is not supported");
				}
			}

			return null;
		}
	}
}
