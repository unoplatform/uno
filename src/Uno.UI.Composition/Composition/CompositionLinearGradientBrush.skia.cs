#nullable enable

using SkiaSharp;

namespace Microsoft.UI.Composition
{
	public partial class CompositionLinearGradientBrush
	{
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
