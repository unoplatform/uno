#nullable enable

using Uno.UI.Composition.Drawing;
using Windows.Foundation;

namespace Microsoft.UI.Composition
{
	public partial class CompositionRadialGradientBrush : CompositionGradientBrush
	{
		private protected override bool TryBuildShader(Rect bounds, float opacity, out IShader? shader)
		{
			var center = EllipseCenter;
			var gradientOrigin = GradientOriginOffset;
			var radius = EllipseRadius;
			var transform = CreateTransformMatrix(bounds);

			if (MappingMode == CompositionMappingMode.Relative)
			{
				center.X *= (float)bounds.Width;
				center.Y *= (float)bounds.Height;
				gradientOrigin.X *= (float)bounds.Width;
				gradientOrigin.Y *= (float)bounds.Height;
				radius.X *= (float)bounds.Width;
				radius.Y *= (float)bounds.Height;
			}

			center.X += (float)bounds.Left;
			center.Y += (float)bounds.Top;
			gradientOrigin.X += (float)bounds.Left;
			gradientOrigin.Y += (float)bounds.Top;

			shader = DrawingBackend.Current.CreateRadialGradientShader(
				center, gradientOrigin, radius.X, radius.Y, GetNeutralColors(opacity), ColorPositions!, NeutralTileMode, transform);
			return true;
		}
	}
}
