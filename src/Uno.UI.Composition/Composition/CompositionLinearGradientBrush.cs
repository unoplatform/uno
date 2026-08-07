#nullable enable

using System.Numerics;
using SkiaSharp;

namespace Microsoft.UI.Composition
{
	public partial class CompositionLinearGradientBrush : CompositionGradientBrush
	{
		private Vector2 _startPoint = Vector2.Zero;
		private Vector2 _endPoint = new Vector2(1, 0);

		internal CompositionLinearGradientBrush(Compositor compositor)
			: base(compositor)
		{

		}

		public Vector2 StartPoint
		{
			get => _startPoint;
			set => SetProperty(ref _startPoint, value);
		}

		public Vector2 EndPoint
		{
			get => _endPoint;
			set => SetProperty(ref _endPoint, value);
		}

		private protected override (SKShader? shader, SKColor color) GetPaintingParameters(SKRect bounds)
		{
			var startPoint = StartPoint.ToSKPoint();
			var endPoint = EndPoint.ToSKPoint();
			var transform = CreateTransformMatrix(bounds);

			// Transform the points into absolute coordinates.
			if (MappingMode == CompositionMappingMode.Relative)
			{
				// If mapping is relative to bounding box, multiply points by bounds.
				startPoint.X *= (float)bounds.Width;
				startPoint.Y *= (float)bounds.Height;

				endPoint.X *= (float)bounds.Width;
				endPoint.Y *= (float)bounds.Height;
			}

			// Translate gradient points by bounds offset.
			startPoint.X += bounds.Left;
			startPoint.Y += bounds.Top;

			endPoint.X += bounds.Left;
			endPoint.Y += bounds.Top;

			// Create linear gradient shader.
			var shader = SKShader.CreateLinearGradient(
				startPoint, endPoint,
				Colors, ColorPositions,
				TileMode, transform);

			return (shader, SKColors.Black);
		}
	}
}
