#nullable enable

using SkiaSharp;
using Uno.UI.Composition.Drawing;
using Windows.Foundation;

namespace Microsoft.UI.Composition
{
	public partial class CompositionLinearGradientBrush
	{
		private protected override bool TryBuildShader(Rect bounds, out IShader? shader)
		{
			var start = StartPoint;
			var end = EndPoint;

			if (MappingMode == CompositionMappingMode.Relative)
			{
				start.X *= (float)bounds.Width;
				start.Y *= (float)bounds.Height;
				end.X *= (float)bounds.Width;
				end.Y *= (float)bounds.Height;
			}

			start.X += (float)bounds.Left;
			start.Y += (float)bounds.Top;
			end.X += (float)bounds.Left;
			end.Y += (float)bounds.Top;

			var localMatrix = CreateTransformMatrix(bounds.ToSKRect()).ToMatrix3x2();

			shader = DrawingBackend.Current.CreateLinearGradientShader(
				start, end, GetNeutralColors(), ColorPositions!, NeutralTileMode, localMatrix);
			return true;
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
