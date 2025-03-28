using System.Numerics;
using Windows.UI.Composition;

namespace Windows.UI.Xaml.Media;

partial class ImageBrush
{
	internal override CompositionBrush GetOrCreateCompositionBrush(Compositor compositor)
	{
		if (_compositionBrush is null)
		{
			_compositionBrush = compositor.CreateSurfaceBrush();
			SynchronizeCompositionBrush();
		}

		return _compositionBrush;
	}

	internal override void SynchronizeCompositionBrush()
	{
		if (_compositionBrush is CompositionSurfaceBrush surfaceBrush && ImageDataCache is { } data)
		{
			surfaceBrush.Stretch = (CompositionStretch)Stretch;
			surfaceBrush.HorizontalAlignmentRatio = GetHorizontalAlignmentRatio(AlignmentX);
			surfaceBrush.VerticalAlignmentRatio = GetVerticalAlignmentRatio(AlignmentY);
			surfaceBrush.Surface = data.CompositionSurface;
			surfaceBrush.RelativeTransform = RelativeTransform?.MatrixCore ?? Matrix3x2.Identity;
		}
	}

	private static float GetHorizontalAlignmentRatio(AlignmentX alignmentX)
	{
		return alignmentX switch
		{
			AlignmentX.Left => 0.0f,
			AlignmentX.Center => 0.5f,
			AlignmentX.Right => 1.0f,
			_ => 0.5f, // this should never happen.
		};
	}

	private static float GetVerticalAlignmentRatio(AlignmentY alignmentY)
	{
		return alignmentY switch
		{
			AlignmentY.Top => 0.0f,
			AlignmentY.Center => 0.5f,
			AlignmentY.Bottom => 1.0f,
			_ => 0.5f, // this should never happen.
		};
	}
}
