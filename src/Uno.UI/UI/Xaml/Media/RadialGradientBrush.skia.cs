using System.Numerics;
using Microsoft.UI.Composition;

namespace Microsoft.UI.Xaml.Media;

partial class RadialGradientBrush
{
	internal override CompositionBrush GetOrCreateCompositionBrush(Compositor compositor)
	{
		if (_compositionBrush is null)
		{
			_compositionBrush = compositor.CreateRadialGradientBrush();
			SynchronizeCompositionBrush();
		}

		return _compositionBrush;
	}

	internal override void SynchronizeCompositionBrush()
	{
		// Intentionally not calling base.SynchronizeCompositionBrush().
		// The logic for XamlCompositionBrushBase is irrelevant for RadialGradientBrush.
		if (_compositionBrush is CompositionRadialGradientBrush compositionBrush)
		{
			compositionBrush.EllipseCenter = Center.ToVector2();
			compositionBrush.EllipseRadius = new Vector2((float)RadiusX, (float)RadiusY);
			ConvertGradientColorStops(compositionBrush.Compositor, compositionBrush, GradientStops, Opacity);
			compositionBrush.GradientOriginOffset = GradientOrigin.ToVector2();
			compositionBrush.InterpolationSpace = InterpolationSpace;
			compositionBrush.MappingMode = ConvertBrushMappingMode(MappingMode);
			compositionBrush.ExtendMode = ConvertGradientExtendMode(SpreadMethod);
			compositionBrush.RelativeTransformMatrix = RelativeTransform?.MatrixCore ?? Matrix3x2.Identity;
		}
	}
}
