#nullable enable

using Uno.UI.Composition.Drawing;
using Windows.Foundation;

namespace Microsoft.UI.Composition
{
	public partial class CompositionLinearGradientBrush
	{
		private protected override bool TryBuildShader(Rect bounds, float opacity, out IShader? shader)
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

			var localMatrix = CreateTransformMatrix(bounds);

			shader = DrawingFactory.Current.CreateLinearGradientShader(
				start, end, GetNeutralColors(opacity), ColorPositions!, NeutralTileMode, localMatrix);
			return true;
		}

	}
}
