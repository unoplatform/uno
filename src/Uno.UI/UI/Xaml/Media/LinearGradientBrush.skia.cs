using System.Numerics;
using Windows.UI.Composition;

namespace Windows.UI.Xaml.Media;

public partial class LinearGradientBrush : GradientBrush
{
	internal override CompositionBrush GetOrCreateCompositionBrush(Compositor compositor)
	{
		if (_compositionBrush is null)
		{
			_compositionBrush = compositor.CreateLinearGradientBrush();
			SynchronizeCompositionBrush();
		}

		return _compositionBrush;
	}

	internal override void SynchronizeCompositionBrush()
	{
		base.SynchronizeCompositionBrush();
		if (_compositionBrush is CompositionLinearGradientBrush compositionBrush)
		{
			compositionBrush.StartPoint = StartPoint.ToVector2();
			compositionBrush.EndPoint = EndPoint.ToVector2();

			compositionBrush.RelativeTransformMatrix = RelativeTransform?.MatrixCore ?? Matrix3x2.Identity;
			compositionBrush.ExtendMode = ConvertGradientExtendMode(SpreadMethod);
			compositionBrush.MappingMode = ConvertBrushMappingMode(MappingMode);
			ConvertGradientColorStops(compositionBrush.Compositor, compositionBrush, GradientStops, Opacity);
		}
	}
}
