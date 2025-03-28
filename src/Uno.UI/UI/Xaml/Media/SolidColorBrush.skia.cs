using Windows.UI.Composition;

namespace Windows.UI.Xaml.Media;

public partial class SolidColorBrush : Brush
{
	internal override CompositionBrush GetOrCreateCompositionBrush(Compositor compositor)
	{
		if (_compositionBrush is null)
		{
			_compositionBrush = compositor.CreateColorBrush();
			SynchronizeCompositionBrush();
		}

		return _compositionBrush;
	}

	internal override void SynchronizeCompositionBrush()
	{
		base.SynchronizeCompositionBrush();
		if (_compositionBrush is CompositionColorBrush compositionBrush)
		{
			compositionBrush.Color = ColorWithOpacity;
		}
	}
}
