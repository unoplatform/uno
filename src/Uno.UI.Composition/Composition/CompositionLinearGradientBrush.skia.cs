#nullable enable

using SkiaSharp;

namespace Windows.UI.Composition
{
	public partial class CompositionLinearGradientBrush
	{
		private protected override void UpdatePaintCore(SKPaint paint, SKRect bounds)
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
			//

			// Create linear gradient shader.
			var shader = SKShader.CreateLinearGradient(
				startPoint, endPoint,
				Colors, ColorPositions,
				TileMode, transform);

			// Clean up old shader
			if (paint.Shader != null)
			{
				paint.Shader.Dispose();
				paint.Shader = null;
			}

			paint.Shader = shader;
			paint.Color = SKColors.Black;
		}
	}
}
